using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Unicodex.Model;

namespace Unicodex.Controller
{
    public class FilterController
    {
        protected MainWindow window;
        protected ListView results;
        protected TextBox input;

        public Filter Filter { get; protected set; }

        protected FilterController(MainWindow window, ListView results, TextBox input)
        {
            this.window = window;
            this.results = results;
            this.input = input;
        }

        public void UpdateResults()
        {
            results.ItemsSource = Filter.Search(new Model.Query(input.Text));
            UpdateSelectedResult(0);
        }

        public void UpdateSelectedResult(int selected)
        {
            ObservableCollection<View.Character> items = (ObservableCollection<View.Character>)results.ItemsSource;
            if (items != null && items.Count > 0)
            {
                results.SelectedIndex = Math.Min(Math.Max(selected, 0), items.Count - 1);
                results.ScrollIntoView(results.SelectedItem);
            }
        }

        public void PreviewKeyDown(KeyEventArgs e)
        {
            ObservableCollection<View.Character> items = (ObservableCollection<View.Character>)results.ItemsSource;
            if (items != null && items.Count > 0)
            {
                if (e.IsDown)
                {
                    // Use up/down arrow keys to navigate search results
                    if (e.Key == Key.Down)
                    {
                        UpdateSelectedResult(results.SelectedIndex + 1);
                    }
                    else if (e.Key == Key.Up)
                    {
                        UpdateSelectedResult(results.SelectedIndex - 1);
                    }
                    // Use Enter to send the selected character
                    else if (e.Key == Key.Enter)
                    {
                        SendCharacter(items[results.SelectedIndex]);
                    }
                    else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
                    {
                        if (input.SelectedText == string.Empty)
                        {
                            CopyToClipboard(items[results.SelectedIndex]);
                        }
                    }
                }
            }
        }

        private void SendCharacter(View.Character c)
        {
            string value = c.Model.Value;

            // Build key data for SendInput
            // FIXME: astral plane characters break in Notepad++
            Win32.INPUT[] inputs = new Win32.INPUT[value.Length];
            int iChr = 0;
            foreach (char chr in value)
            {
                inputs[iChr] = new Win32.INPUT();
                inputs[iChr].type = Win32.InputType.KEYBOARD;
                inputs[iChr].U.ki = new Win32.KEYBDINPUT();
                inputs[iChr].U.ki.wVk = 0;
                inputs[iChr].U.ki.wScan = (short)chr;
                inputs[iChr].U.ki.dwFlags = Win32.KEYEVENTF.UNICODE;
                inputs[iChr].U.ki.time = 0;
                iChr++;
            }

            // Send keys as soon as Unicodex is done hiding itself
            EventHandler handler = null;
            handler = delegate (object sender, EventArgs e)
            {
                uint result = Win32.SendInput((uint)value.Length, inputs, Marshal.SizeOf(inputs[0]));
                if (result == 0)
                {
                    throw new Win32Exception();
                }
                window.Deactivated -= handler;
            };
            window.Deactivated += handler;
            window.Hide();
        }

        public void CopyToClipboard(View.Character character)
        {
            Clipboard.SetText(character.Model.Value);
            window.Hide();
        }

        public bool IsActive()
        {
            return input.IsVisible;
        }

        public void FocusInput()
        {
            input.Focus();
        }
    }

    public class FavoritesController : FilterController
    {
        public FavoritesController(MainWindow window) : base(window, window.FavoritesResults, window.FavoritesTextBox)
        {
            Properties.Settings.Default.Favorites.CollectionChanged += Favorites_CollectionChanged;
            Initialize();
        }

        private void Favorites_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Initialize();
        }

        private void Initialize()
        {
            Filter = new Filter();
            Filter.ReturnAllCharactersOnEmptyQuery = true;

            foreach (string codepointHex in Properties.Settings.Default.Favorites.FavoriteSet)
            {
                Filter.Add(((App)Application.Current).Characters.AllCharactersByCodepointHex[codepointHex]);
            }

            /* Because an empty query returns results for this filter, we need
             * to prepopulate the results for this tab, otherwise it'll appear
             * empty until the user types something. */
            UpdateResults();
        }
    }

    public class SearchController : FilterController
    {
        public SearchController(MainWindow window) : base(window, window.SearchResults, window.SearchTextBox)
        {
            Filter = new Filter();

            foreach (Character c in ((App) Application.Current).Characters.AllCharacters)
            {
                Filter.Add(c);
            }
        }
    }
}
using System;
using System.Collections.ObjectModel;
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

        public UnicodexFilter Filter { get; protected set; }

        protected FilterController(MainWindow window, ListView results, TextBox input)
        {
            this.window = window;
            this.results = results;
            this.input = input;
        }

        public void UpdateResults()
        {
            results.ItemsSource = Filter.Search(new Model.Query(input.Text));
            UpdateSelectedResult(results, 0);
        }

        public void UpdateSelectedResult(ListView listView, int selected)
        {
            ObservableCollection<View.Character> results = (ObservableCollection<View.Character>)listView.ItemsSource;
            if (results != null && results.Count > 0)
            {
                listView.SelectedIndex = Math.Min(Math.Max(selected, 0), results.Count - 1);
                listView.ScrollIntoView(listView.SelectedItem);
            }
        }

        public void PreviewKeyDown(object sender, KeyEventArgs e, ListView listView)
        {
            TextBox textBox = (TextBox)sender;
            ObservableCollection<View.Character> results = (ObservableCollection<View.Character>)listView.ItemsSource;
            if (results != null && results.Count > 0)
            {
                if (e.IsDown)
                {
                    // Use up/down arrow keys to navigate search results
                    if (e.Key == Key.Down)
                    {
                        UpdateSelectedResult(listView, listView.SelectedIndex + 1);
                    }
                    else if (e.Key == Key.Up)
                    {
                        UpdateSelectedResult(listView, listView.SelectedIndex - 1);
                    }
                    // Use Enter to send the selected character
                    else if (e.Key == Key.Enter)
                    {
                        SendCharacter(results[listView.SelectedIndex]);
                    }
                    else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
                    {
                        if (textBox.SelectedText == string.Empty)
                        {
                            CopyToClipboard(results[listView.SelectedIndex]);
                        }
                    }
                }
            }
        }

        private void SendCharacter(View.Character c)
        {
            // Build key data for SendInput
            // FIXME: astral plane characters break in Notepad++
            Win32.INPUT[] inputs = new Win32.INPUT[c.Value.Length];
            int iChr = 0;
            foreach (char chr in c.Value)
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
                uint result = Win32.SendInput((uint)c.Value.Length, inputs, Marshal.SizeOf(inputs[0]));
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
            Clipboard.SetText(character.Value);
            window.Hide();
        }
    }

    public class FavoritesController : FilterController
    {
        public FavoritesController(MainWindow window, UnicodexFilter searchFilter) : base(window, window.FavoritesResults, window.FavoritesTextBox)
        {
            Filter = new UnicodexFilter();
            Filter.ReturnAllCharactersOnEmptyQuery = true;

            foreach (string codepoint in Properties.Settings.Default.Favorites)
            {
                Filter.Add(searchFilter.GetByCodepoint(codepoint));
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
            Filter = new UnicodexFilter();

            using (StringReader unicodeDataLines = new StringReader(Properties.Resources.UnicodeData))
            {
                string unicodeDataLine = string.Empty;
                while (true)
                {
                    unicodeDataLine = unicodeDataLines.ReadLine();
                    if (unicodeDataLine == null) break;
                    UnicodeDataEntry entry = new UnicodeDataEntry(unicodeDataLine);
                    Character c = new Character(entry);
                    Filter.Add(c);
                }
            }
        }
    }
}
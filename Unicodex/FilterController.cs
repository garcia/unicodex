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
using Unicodex.View;

namespace Unicodex.Controller
{
    public abstract class FilterController<ModelT, ViewT>
        where ModelT : ModelObject<ViewT>
        where ViewT : ViewObject
    {
        protected MainWindow window;
        protected ListView results;
        protected TextBox input;

        public Filter<ModelT> Filter { get; protected set; }

        protected FilterController(MainWindow window, ListView results, TextBox input)
        {
            this.window = window;
            this.results = results;
            this.input = input;
        }

        public void UpdateResults()
        {
            ObservableCollection<ViewT> view = new ObservableCollection<ViewT>();
            foreach (ModelT model in Filter.Search(new Query(input.Text)))
            {
                view.Add(model.ToView());
            }
            results.ItemsSource = view;
            UpdateSelectedResult(0);
        }

        public void UpdateSelectedResult(int selected)
        {
            ObservableCollection<ViewT> items = (ObservableCollection<ViewT>)results.ItemsSource;
            if (items != null && items.Count > 0)
            {
                results.SelectedIndex = Math.Min(Math.Max(selected, 0), items.Count - 1);
                results.ScrollIntoView(results.SelectedItem);
            }
        }

        public void PreviewKeyDown(KeyEventArgs e)
        {
            ObservableCollection<ViewT> items = (ObservableCollection<ViewT>)results.ItemsSource;
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
                        HandleChooseEvent(items[results.SelectedIndex]);
                    }
                    else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
                    {
                        if (input.SelectedText == string.Empty)
                        {
                            HandleCopyEvent(items[results.SelectedIndex]);
                        }
                    }
                }
            }
        }

        public abstract void HandleCopyEvent(ViewT viewT);

        public abstract void HandleChooseEvent(ViewT viewT);

        public bool IsActive()
        {
            return input.IsVisible;
        }

        public void FocusInput()
        {
            input.Focus();
        }
    }

    public class SearchController : FilterController<Model.Character, View.Character>
    {
        public SearchController(MainWindow window) : base(window, window.SearchResults, window.SearchTextBox)
        {
            Filter = new Filter<Model.Character>(new Cache<Model.Character>[] {
                new FavoritesCache(((App)Application.Current).Favorites),
                new AllWordsCache<Model.Character>(),
                new TagsCache(((App)Application.Current).TagGroups),
                new FirstLetterOfAllWordsCache<Model.Character>(),
                new CodepointCache()
            });

            foreach (Model.Character c in ((App) Application.Current).Characters.AllCharacters)
            {
                Filter.Add(c);
            }
        }

        internal static void SendCharacter(Window window, View.Character c)
        {
            string value = c.ModelObject.Value;

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

        internal static void CopyToClipboard(Window window, View.Character character)
        {
            Clipboard.SetText(character.ModelObject.Value);
            window.Hide();
        }

        public override void HandleCopyEvent(View.Character c)
        {
            CopyToClipboard(window, c);
        }

        public override void HandleChooseEvent(View.Character c)
        {
            SendCharacter(window, c);
        }
    }

    public class FavoritesController : FilterController<Model.Character, View.Character>
    {
        public FavoritesController(MainWindow window) : base(window, window.FavoritesResults, window.FavoritesTextBox)
        {
            ((App)Application.Current).Favorites.CollectionChanged += Favorites_CollectionChanged;
            Initialize();
        }

        public override void HandleCopyEvent(View.Character c)
        {
            SearchController.CopyToClipboard(window, c);
        }

        public override void HandleChooseEvent(View.Character c)
        {
            SearchController.SendCharacter(window, c);
        }

        private void Favorites_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Initialize();
        }

        private void Initialize()
        {
            Filter = new Filter<Model.Character>(new Cache<Model.Character>[] {
                new TagsCache(((App)Application.Current).TagGroups),
                new AllWordsCache<Model.Character>(),
                new FirstLetterOfAllWordsCache<Model.Character>(),
                new CodepointCache()
            });
            Filter.ReturnAllCharactersOnEmptyQuery = true;

            foreach (string codepointHex in ((App)Application.Current).Favorites.FavoriteSet)
            {
                Filter.Add(((App)Application.Current).Characters.AllCharactersByCodepointHex[codepointHex]);
            }

            /* Because an empty query returns results for this filter, we need
             * to prepopulate the results for this tab, otherwise it'll appear
             * empty until the user types something. */
            UpdateResults();
        }
    }

    public class TagsController : FilterController<Model.Tag, View.Tag>
    {
        public TagsController(MainWindow window) : base(window, window.TagsResults, window.TagsTextBox)
        {
            Initialize();
        }

        private void Initialize()
        {
            Filter = new Filter<Model.Tag>(new Cache<Model.Tag>[] {
                new AllWordsCache<Model.Tag>(),
                new FirstLetterOfAllWordsCache<Model.Tag>(),
            });
            Filter.ReturnAllCharactersOnEmptyQuery = true;

            foreach (Model.Tag tag in ((App)Application.Current).TagGroups.GetAllTags())
            {
                Filter.Add(tag);
            }

            UpdateResults();
        }

        public override void HandleCopyEvent(View.Tag viewT)
        {
            // no-op?
        }

        public override void HandleChooseEvent(View.Tag viewT)
        {
            string searchText;
            string hashtag = "#" + viewT.Name;
            if (viewT.Name.Contains(" "))
            {
                searchText = "\"" + hashtag + "\" ";
            }
            else
            {
                searchText = hashtag + " ";
            }
            window.SearchTab.IsSelected = true;
            window.SearchTextBox.Text = searchText;
            window.SearchTextBox.CaretIndex = searchText.Length;
            window.SearchTextBox.Focus();
        }

    }
}
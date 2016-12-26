using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Unicodex.Controller;
using Unicodex.Properties;

namespace Unicodex
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FilterController<Model.Character, View.Character> search;
        private FilterController<Model.Character, View.Character> favorites;
        private FilterController<Model.Tag, View.Tag> tags;

        private System.Windows.Forms.NotifyIcon notifyIcon;

        public MainWindow()
        {
            InitializeComponent();
            InitializeNotifyIcon();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Create filter controllers
            search = new SearchController(this);
            favorites = new FavoritesController(this);
            tags = new TagsController(this);

            // Add WndProc handler
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);

            // Register global hotkey (has to be done after this window is created)
            ((App)Application.Current).UpdateHotkey();
        }

        private void InitializeNotifyIcon()
        {
            // Create tray icon
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Icon = Properties.Resources.main;
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick += delegate (object sender, EventArgs args)
            {
                Show();
                WindowState = WindowState.Normal;
            };

            // Create right-click context menu for tray icon
            System.Windows.Forms.MenuItem menuItemShow = new System.Windows.Forms.MenuItem();
            menuItemShow.Index = 0;
            menuItemShow.Text = "&Show";
            menuItemShow.Click += new System.EventHandler(NotifyIcon_MenuItem_Show_Click);

            System.Windows.Forms.MenuItem menuItemExit = new System.Windows.Forms.MenuItem();
            menuItemExit.Index = 1;
            menuItemExit.Text = "E&xit";
            menuItemExit.Click += new System.EventHandler(NotifyIcon_MenuItem_Exit_Click);

            System.Windows.Forms.ContextMenu contextMenu = new System.Windows.Forms.ContextMenu();
            contextMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] { menuItemShow, menuItemExit });
            notifyIcon.ContextMenu = contextMenu;
        }

        private void NotifyIcon_MenuItem_Show_Click(object sender, EventArgs e)
        {
            Show();
        }

        private void NotifyIcon_MenuItem_Exit_Click(object sender, EventArgs e)
        {
            Shutdown();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Win32.WM_HOTKEY)
            {
                Show();
            }
            return (IntPtr)0;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.IsDown)
            {
                if (e.Key == Key.Escape)
                {
                    Hide();
                }
                else if (e.Key != Key.Up && e.Key != Key.Down)
                {
                    // TODO: bleh. There should be a cleaner way to do this...
                    if (search.IsActive())
                    {
                        search.FocusInput();
                        search.PreviewKeyDown(e);
                    }
                    else if (favorites.IsActive())
                    {
                        favorites.FocusInput();
                        favorites.PreviewKeyDown(e);
                    }
                    else if (tags.IsActive())
                    {
                        tags.FocusInput();
                        tags.PreviewKeyDown(e);
                    }
                }
            }
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                // Switch back to the search tab and clear the filter inputs
                SearchTextBox.Text = string.Empty;
                FavoritesTextBox.Text = string.Empty;
                tabControl.SelectedIndex = 0;
            }
            else
            {
                /* Window is becoming visible - spawn it according to the
                 * user's preferences */
                if (!((App)Application.Current).Preferences.spawnNearTextCaret || !putNearTextCaret())
                {
                    var spawnPlacement = ((App)Application.Current).Preferences.spawnPlacement;
                    switch (spawnPlacement)
                    {
                        case SpawnPlacement.SPAWN_NEAR_CURSOR:
                            spawnNearCursor();
                            break;
                        case SpawnPlacement.SPAWN_NEAR_WINDOW:
                            spawnNearWindow();
                            break;
                        case SpawnPlacement.SPAWN_IN_MONITOR:
                            spawnInMonitor();
                            break;

                    }
                }


                // Focus textbox once it's re-marked as visible
                DependencyPropertyChangedEventHandler handler = null;
                handler = delegate
                {
                    SearchTextBox.Focus();
                    SearchTextBox.IsVisibleChanged -= handler;
                };
                SearchTextBox.IsVisibleChanged += handler;
            }
        }

        private void spawnNearCursor()
        {
            int left = System.Windows.Forms.Cursor.Position.X;
            int top = System.Windows.Forms.Cursor.Position.Y;
            WindowUtils.PutWindowNear(this, new Rect(left, top, 0, 0), PlacementSide.CENTER, PlacementInOut.INSIDE);
        }

        private void spawnNearWindow()
        {
            Win32.GUITHREADINFO gui = new Win32.GUITHREADINFO();
            gui.cbSize = Marshal.SizeOf(gui);
            bool success = Win32.GetGUIThreadInfo(0, ref gui);

            if (success)
            {
                if (gui.hwndActive != IntPtr.Zero)
                {
                    Win32.RECT windowRect = new Win32.RECT();
                    bool success2 = Win32.GetWindowRect(gui.hwndActive, ref windowRect);
                    if (success2)
                    {
                        var windowPlacement = ((App)Application.Current).Preferences.windowPlacement;
                        var insideOutsidePlacement = ((App)Application.Current).Preferences.insideOutsidePlacement;
                        WindowUtils.PutWindowNear(this, windowRect.asRect(), windowPlacement, insideOutsidePlacement);
                    }
                }
                else
                {
                    // No active window -- spawn somewhere in the active monitor instead
                    spawnInMonitor();
                }
            }
            else
            {
                // TODO: log these, don't crash
                throw new Win32Exception();
            }
        }



        private void spawnInMonitor()
        {
            Rect rect = Rect.Empty;

            // Use the active window to determine the active monitor
            Win32.GUITHREADINFO gui = new Win32.GUITHREADINFO();
            gui.cbSize = Marshal.SizeOf(gui);
            bool success = Win32.GetGUIThreadInfo(0, ref gui);
            if (success)
            {
                if (gui.hwndActive != IntPtr.Zero)
                {
                    Win32.RECT windowRect = new Win32.RECT();
                    bool success2 = Win32.GetWindowRect(gui.hwndActive, ref windowRect);
                    if (success2)
                    {
                        rect = windowRect.asRect();
                    }
                }
            }

            // If that fails, use the cursor to determine the active monitor
            if (rect.IsEmpty)
            {
                int left = System.Windows.Forms.Cursor.Position.X;
                int top = System.Windows.Forms.Cursor.Position.Y;
                rect = new Rect(left, top, 0, 0);
            }

            PlacementSide monitorPlacement = ((App)Application.Current).Preferences.monitorPlacement;
            WindowUtils.PutWindowNear(this, WindowUtils.MonitorWorkAreaFromRect(rect), monitorPlacement, PlacementInOut.INSIDE);
            
        }

        private bool putNearTextCaret()
        {
            Win32.GUITHREADINFO gui = new Win32.GUITHREADINFO();
            gui.cbSize = Marshal.SizeOf(gui);
            bool success = Win32.GetGUIThreadInfo(0, ref gui);
            if (success)
            {
                if (gui.hwndCaret == IntPtr.Zero)
                {
                    /* The focused application has no caret information, so
                     * gracefully degrade to some alternative method. */
                    return false;
                }
                else
                {
                    /* The GUI's caret position is relative to its control,
                     * so get the control's position and add the two. */
                    Win32.RECT windowRect = new Win32.RECT();
                    bool success2 = Win32.GetWindowRect(gui.hwndCaret, ref windowRect);
                    if (success2)
                    {
                        Rect caretRect = gui.rcCaret.asRect();
                        caretRect.X += windowRect.left;
                        caretRect.Y += windowRect.top;
                        WindowUtils.PutWindowNear(this, caretRect, PlacementSide.BOTTOM_LEFT, PlacementInOut.OUTSIDE);
                        return true;
                    }
                    else
                    {
                        /* TODO: before production release, handle these
                         * exceptions and always fall back to cursor pos */
                        throw new Win32Exception();
                    }
                }
            }
            else
            {
                // TODO: see above
                throw new Win32Exception();
            }
        }
        
        private void Window_Deactivated(object sender, EventArgs e)
        {
            Hide();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            search.UpdateResults();
        }

        private void FavoritesTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            favorites.UpdateResults();
        }

        private void TagsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            tags.UpdateResults();
        }

        private void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            search.PreviewKeyDown(e);
        }

        private void FavoritesTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            favorites.PreviewKeyDown(e);
        }


        private void TagsTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            tags.PreviewKeyDown(e);
        }

        private void SearchResult_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ContentControl content = (ContentControl)sender;
            View.Character character = (View.Character)content.DataContext;
            search.HandleChooseEvent(character);
        }

        private void TagsResult_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ContentControl content = (ContentControl)sender;
            View.Tag tag = (View.Tag)content.DataContext;
            tags.HandleChooseEvent(tag);
        }

        private void FavoritesResult_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ContentControl content = (ContentControl)sender;
            View.Character character = (View.Character)content.DataContext;
            favorites.HandleChooseEvent(character);
        }

        private void navButton_Click(object sender, RoutedEventArgs e)
        {
            navButton.ContextMenu.IsEnabled = true;
            navButton.ContextMenu.PlacementTarget = (sender as Button);
            navButton.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Left;
            navButton.ContextMenu.IsOpen = true;
            navButton.ContextMenu.HorizontalOffset = navButton.ActualWidth + 5;
            navButton.ContextMenu.VerticalOffset = navButton.ActualHeight;
        }

        private void Shutdown()
        {
            // Unregister gloabl hotkey (probably not necessary, but as a formality...)
            Win32.UnregisterHotKey((IntPtr)new WindowInteropHelper(Application.Current.MainWindow).Handle.ToInt32(), 0);
            
            // Remove tray icon ASAP (otherwise will only disappear when user hovers over it)
            notifyIcon.Icon = null;

            Application.Current.Shutdown();
        }

        private void Search_MenuItem_CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            View.Character character = (View.Character)menuItem.DataContext;
            search.HandleCopyEvent(character);
        }

        private void Search_MenuItem_MarkAsFavorite_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            View.Character character = (View.Character)menuItem.DataContext;
            character.IsFavorite = true;
        }

        private void Search_MenuItem_EditTags_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            View.Character character = (View.Character)menuItem.DataContext;
            EditTagsWindow editTagsWindow = new EditTagsWindow(character);
            editTagsWindow.Show();
        }

        private void NavButton_MenuItem_Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Show();
        }

        private void NavButton_MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            Shutdown();
        }
    }

}

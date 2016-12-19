using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Unicodex.Controller;

namespace Unicodex
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FilterController search;
        private FilterController favorites;

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
            favorites = new FavoritesController(this, search.Filter);

            // Add WndProc handler
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);

            // Register hotkey (TODO: make user-configurable)
            IntPtr hWnd = getMainWindowHwnd();
            Win32.RegisterHotKey(hWnd, 0, Win32.MOD_NOREPEAT | Win32.MOD_CONTROL | Win32.MOD_SHIFT, KeyInterop.VirtualKeyFromKey(Key.U));
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

        private IntPtr getMainWindowHwnd()
        {
            return (IntPtr)new WindowInteropHelper(Application.Current.MainWindow).Handle.ToInt32();
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
                    FilterController activeFilter = null;
                    if (search.IsActive())
                    {
                        activeFilter = search;
                    }
                    else if (favorites.IsActive())
                    {
                        activeFilter = favorites;
                    }
                    if (activeFilter != null)
                    {
                        activeFilter.FocusInput();
                        activeFilter.PreviewKeyDown(e);
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
                /* Window is becoming visible - we want to put it somewhere
                 * where the user will immediately see it, ideally right where
                 * they were typing. */
                Win32.GUITHREADINFO gui = new Win32.GUITHREADINFO();
                gui.cbSize = Marshal.SizeOf(gui);
                bool success = Win32.GetGUIThreadInfo(0, ref gui);
                if (success)
                {
                    if (gui.hwndCaret == IntPtr.Zero)
                    {
                        /* The focused application has no caret information, so
                         * gracefully degrade by using the cursor position. */
                        int left = System.Windows.Forms.Cursor.Position.X;
                        int top = System.Windows.Forms.Cursor.Position.Y;
                        PutWindowNear(left, top, top);
                    }
                    else
                    {
                        /* The GUI's caret position is relative to its control,
                         * so get the control's position and add the two. */
                        Win32.RECT windowRect = new Win32.RECT();
                        bool success2 = Win32.GetWindowRect(gui.hwndCaret, ref windowRect);
                        if (success2)
                        {
                            int left = windowRect.left + gui.rcCaret.left;
                            int top = windowRect.top + gui.rcCaret.top;
                            int bottom = windowRect.top + gui.rcCaret.bottom;
                            PutWindowNear(left, top, bottom);
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

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Hide();
        }

        private void PutWindowNear(int left, int top, int bottom)
        {
            IntPtr monitor = Win32.MonitorFromPoint(new Win32.POINT(left, (top + bottom) / 2), Win32.MonitorOptions.MONITOR_DEFAULTTONEAREST);
            Win32.MONITORINFO monitorInfo = new Win32.MONITORINFO();
            monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);
            Win32.GetMonitorInfo(monitor, ref monitorInfo);
            Win32.RECT workArea = monitorInfo.rcWork;

            /* Wherever the window spawns, put it just below and to the left
             * of the focal point, for aesthetic reasons. */
            int leftOffset = -5;
            int topOffset = -5;
            int bottomOffset = 5;

            int newRight = left + (int)ActualWidth + leftOffset;
            int newBottom = bottom + (int)ActualHeight + bottomOffset;
            if (newRight > workArea.right)
            {
                left -= (int)ActualWidth;
            }
            if (newBottom > workArea.bottom)
            {
                top -= (int)ActualHeight;
                top += topOffset;
            }
            else
            {
                top = bottom;
                top += bottomOffset;
            }

            Left = Math.Max(left + leftOffset, 0);
            Top = Math.Max(top, 0);
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

        private void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            search.PreviewKeyDown(e);
        }

        private void FavoritesTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            favorites.PreviewKeyDown(e);
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
            search.CopyToClipboard(character);
        }

        private void Search_MenuItem_MarkAsFavorite_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            View.Character character = (View.Character)menuItem.DataContext;

            // Update settings with new favorite
            Properties.Settings.Default.Favorites.Add(character.Model.CodepointHex);
            Properties.Settings.Default.Save();

            // Refresh UI
            favorites.Filter.Add(character.Model);
            favorites.UpdateResults();
        }

    }

}

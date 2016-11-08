using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Resources;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace Unicodex
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private UnicodexSearch unicodexSearch;

        private ObservableCollection<View.Character> results;

        private System.Windows.Forms.NotifyIcon notifyIcon;

        public MainWindow()
        {
            unicodexSearch = new UnicodexSearch();
            InitializeComponent();
            InitializeNotifyIcon();
        }

        private void InitializeNotifyIcon()
        {
            // Create tray icon
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Icon = Properties.Resources.main;
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick += delegate (object sender, EventArgs args)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            };

            // Create right-click context menu for tray icon
            System.Windows.Forms.MenuItem menuItemShow = new System.Windows.Forms.MenuItem();
            menuItemShow.Index = 0;
            menuItemShow.Text = "&Show";
            menuItemShow.Click += new System.EventHandler(this.menuItemShow_Click);

            System.Windows.Forms.MenuItem menuItemExit = new System.Windows.Forms.MenuItem();
            menuItemExit.Index = 1;
            menuItemExit.Text = "E&xit";
            menuItemExit.Click += new System.EventHandler(this.menuItemExit_Click);

            System.Windows.Forms.ContextMenu contextMenu = new System.Windows.Forms.ContextMenu();
            contextMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] { menuItemShow, menuItemExit });
            notifyIcon.ContextMenu = contextMenu;
        }

        private void menuItemShow_Click(object sender, EventArgs e)
        {
            this.Show();
        }

        private void menuItemExit_Click(object sender, EventArgs e)
        {
            Shutdown();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Add WndProc handler
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);

            // Register hotkey (TODO: make user-configurable)
            IntPtr hWnd = (IntPtr)new WindowInteropHelper(Application.Current.MainWindow).Handle.ToInt32();
            Win32.RegisterHotKey(hWnd, 0, Win32.MOD_NOREPEAT | Win32.MOD_CONTROL | Win32.MOD_SHIFT, KeyInterop.VirtualKeyFromKey(Key.U));
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Win32.WM_HOTKEY)
            {
                this.Show();
            }
            return (IntPtr)0;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.IsDown)
            {
                if (e.Key == Key.Escape)
                {
                    this.Hide();
                }
            }
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                // Switch back to the search tab and clear the search box
                textBox.Text = string.Empty;
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
                    textBox.Focus();
                    textBox.IsVisibleChanged -= handler;
                };
                textBox.IsVisibleChanged += handler;
            }
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

            int newRight = left + (int)this.ActualWidth + leftOffset;
            int newBottom = bottom + (int)this.ActualHeight + bottomOffset;
            if (newRight > workArea.right)
            {
                left -= (int)this.ActualWidth;
            }
            if (newBottom > workArea.bottom)
            {
                top -= (int)this.ActualHeight;
                top += topOffset;
            }
            else
            {
                top = bottom;
                top += bottomOffset;
            }

            this.Left = Math.Max(left + leftOffset, 0);
            this.Top = Math.Max(top, 0);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchResults.ItemsSource = results = unicodexSearch.Search(new Model.Query(textBox.Text));
            UpdateSelectedResult(0);

        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (results != null)
            {
                if (e.IsDown)
                {
                    // Use up/down arrow keys to navigate search results
                    if (e.Key == Key.Down)
                    {
                        UpdateSelectedResult(SearchResults.SelectedIndex + 1);
                    }
                    else if (e.Key == Key.Up)
                    {
                        UpdateSelectedResult(SearchResults.SelectedIndex - 1);
                    }
                }
            }
        }

        private void UpdateSelectedResult(int selected)
        {
            if (results != null && results.Count > 0)
            {
                SearchResults.SelectedIndex = Math.Min(Math.Max(selected, 0), results.Count - 1);
                SearchResults.ScrollIntoView(SearchResults.SelectedItem);
            }

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
    }
}

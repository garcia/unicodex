using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Resources;
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
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Icon = Properties.Resources.main;
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick += delegate (object sender, EventArgs args)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            };

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
            BeforeShutdown();
            Application.Current.Shutdown();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);

            IntPtr hWnd = (IntPtr)new WindowInteropHelper(Application.Current.MainWindow).Handle.ToInt32();
            Hotkey.RegisterHotKey(hWnd, 0, Hotkey.MOD_NOREPEAT | Hotkey.MOD_CONTROL | Hotkey.MOD_SHIFT, KeyInterop.VirtualKeyFromKey(Key.U));
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Hotkey.WM_HOTKEY)
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
                textBox.Text = string.Empty;
                tabControl.SelectedIndex = 0;
            }
            else
            {
                DependencyPropertyChangedEventHandler handler = null;
                handler = delegate
                {
                    textBox.Focus();
                    textBox.IsVisibleChanged -= handler;
                };
                textBox.IsVisibleChanged += handler;
            }
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

        private void BeforeShutdown()
        {
            Hotkey.UnregisterHotKey((IntPtr)new WindowInteropHelper(Application.Current.MainWindow).Handle.ToInt32(), 0);
            notifyIcon.Icon = null;
        }
    }
}

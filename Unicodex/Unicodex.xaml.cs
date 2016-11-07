using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        private UnicodexSearch unicodex;

        private ObservableCollection<View.Character> Results;

        public MainWindow()
        {
            unicodex = new UnicodexSearch();
            InitializeComponent();
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
            if (msg == 0x312)
            {
                MessageBox.Show("caught global hotkey");
            }
            return (IntPtr)0;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Hotkey.UnregisterHotKey((IntPtr)new WindowInteropHelper(Application.Current.MainWindow).Handle.ToInt32(), 0);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchResults.ItemsSource = Results = unicodex.Search(new Model.Query(textBox.Text));
            UpdateSelectedResult(0);

        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Results != null)
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
            if (Results != null && Results.Count > 0)
            {
                SearchResults.SelectedIndex = Math.Min(Math.Max(selected, 0), Results.Count - 1);
                SearchResults.ScrollIntoView(SearchResults.SelectedItem);
            }
        }
    }
}

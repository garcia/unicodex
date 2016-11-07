using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Unicodex
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Unicodex unicodex;

        private ObservableCollection<View.Character> Results;

        public MainWindow()
        {
            unicodex = new Unicodex();
            InitializeComponent();
            textBox.TextChanged += TextBox_TextChanged;
            textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchResults.ItemsSource = Results = unicodex.Search(new Model.Query(textBox.Text));
            UpdateSelectedResult(0);

        }

        private void TextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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

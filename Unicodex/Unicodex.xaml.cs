using System.Windows;
using System.Windows.Controls;

namespace Unicodex
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Unicodex unicodex;

        public MainWindow()
        {
            unicodex = new Unicodex();
            InitializeComponent();
            textBox.TextChanged += TextBox_TextChanged;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            icSearchResults.ItemsSource = unicodex.Search(textBox.Text);
        }
    }
}

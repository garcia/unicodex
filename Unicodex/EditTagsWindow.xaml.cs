using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Unicodex.Properties;
using Unicodex.View;

namespace Unicodex
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class EditTagsWindow : Window
    {
        public EditTagsWindow(Character c)
        {
            InitializeComponent();

            IEnumerable<string> currentTags = Settings.Default.UserTags.GetTags(c.Model.CodepointHex);
            string header = $"Editing tags for {c.Name}. Enter one tag per line.\nSome tags are built-in; these can be turned off in your Preferences.";
            string tagData = string.Join(Environment.NewLine, currentTags);
            this.DataContext = new EditTagsData(c, header, tagData);
        }

        private void saveAndClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                EditTagsData data = (EditTagsData)this.DataContext;
                Character c = data.Character;
                string tagData = data.TagData;

                /* Make a copy of the old tags since we will be iteratively
                 * removing them, thus changing the list it returns. */
                List<string> oldTags = new List<string>(Settings.Default.UserTags.GetTags(c.Model.CodepointHex));
                string[] newTags = tagData.Split(new[] { '\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (string oldTag in oldTags)
                {
                    Settings.Default.UserTags.RemoveTag(c.Model.CodepointHex, oldTag);
                }
                foreach (string newTag in newTags)
                {
                    string sanitizedTag = newTag
                        .Replace("#", "")
                        .Replace("\"", "")
                        .Replace("\r", "")
                        .Replace("\n", "");
                    Settings.Default.UserTags.AddTag(c.Model.CodepointHex, sanitizedTag);
                }

                Settings.Default.Save();
            }
        }
    }

    public class EditTagsData
    {
        public Character Character { get; set; }
        public string Header { get; set; }
        public string TagData { get; set; }

        public EditTagsData(Character character, string header, string tagData)
        {
            this.Character = character;
            this.Header = header;
            this.TagData = tagData;
        }
    }

    public sealed class MultiLineConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Join(Environment.NewLine, values);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            string stringValue = (string)value;
            return stringValue.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}

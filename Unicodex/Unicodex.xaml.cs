using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

using Unicodex.Model;

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



    public class Unicodex
    {
        private string UnicodeData = Properties.Resources.UnicodeData;
        private Dictionary<char, List<Character>> CharsByFirstLetterOfName = new Dictionary<char, List<Character>>();
        private Dictionary<char, List<Character>> CharsByFirstLettersOfWords = new Dictionary<char, List<Character>>();

        public Unicodex()
        {
            using (StringReader unicodeDataLines = new StringReader(UnicodeData))
            {
                string unicodeDataLine = string.Empty;
                while (true)
                {
                    unicodeDataLine = unicodeDataLines.ReadLine();
                    if (unicodeDataLine == null) break;

                    UnicodeDataEntry entry = new UnicodeDataEntry(unicodeDataLine);
                    Character c = new Character(entry);
                    Store(c);
                }
            }
        }

        private void Store(Character c)
        {
            char firstLetterOfName = c.Name[0];
            if (!CharsByFirstLetterOfName.ContainsKey(firstLetterOfName))
            {
                CharsByFirstLetterOfName[firstLetterOfName] = new List<Character>();
            }
            List<Character> charsByFirstLetter = CharsByFirstLetterOfName[firstLetterOfName];
            charsByFirstLetter.Add(c);

            foreach (string word in c.NameWords) {
                // Skip first word, as this is a dictionary for non-first-word matches
                if (word == c.NameWords[0]) continue;

                char firstLetterOfWord = word[0];
                if (!CharsByFirstLettersOfWords.ContainsKey(firstLetterOfWord))
                {
                    CharsByFirstLettersOfWords[firstLetterOfWord] = new List<Character>();
                }
                List<Character> charsByFirstLetters = CharsByFirstLettersOfWords[firstLetterOfWord];
                charsByFirstLetters.Add(c);
            }
        }

        private IEnumerable<Character> SearchByFirstLetterOfName(string[] queryWords)
        {
            foreach (string queryWord in queryWords)
            {
                char firstLetterOfQueryWord = queryWord[0];
                if (CharsByFirstLetterOfName.ContainsKey(firstLetterOfQueryWord))
                {
                    List<Character> charsByFirstLetter = CharsByFirstLetterOfName[firstLetterOfQueryWord];
                    foreach (Character c in charsByFirstLetter)
                    {
                        if (Matches(queryWords, c))
                        {
                            yield return c;
                        }
                    }
                }
            }
        }

        private IEnumerable<Character> SearchByFirstLettersOfWords(string[] queryWords)
        {
            foreach (string queryWord in queryWords)
            {
                char firstLetterOfQueryWord = queryWord[0];
                if (CharsByFirstLettersOfWords.ContainsKey(firstLetterOfQueryWord))
                {
                    List<Character> charsByFirstLetter = CharsByFirstLettersOfWords[firstLetterOfQueryWord];
                    foreach (Character c in charsByFirstLetter)
                    {
                        if (Matches(queryWords, c))
                        {
                            yield return c;
                        }
                    }
                }
            }
        }

        public List<View.Character> Search(String query)
        {
            HashSet<Character> seenCharacters = new HashSet<Character>();
            List<View.Character> results = new List<View.Character>();
            string[] queryWords = query.ToUpper().Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

            foreach (Character c in SearchByFirstLetterOfName(queryWords).Concat(SearchByFirstLettersOfWords(queryWords)))
            {
                if (!seenCharacters.Contains(c))
                {
                    results.Add(new View.Character(c));
                    seenCharacters.Add(c);
                    if (results.Count >= 50) break;
                }
            }

            return results;
        }

        private bool Matches(string[] queryWords, Character c)
        {
            foreach (string queryWord in queryWords) {
                bool matchesQueryWord = false;
                foreach (string nameWord in c.NameWords)
                {
                    if (nameWord.StartsWith(queryWord))
                    {
                        matchesQueryWord = true;
                        break;
                    }
                }
                if (!matchesQueryWord) return false;
            }
            return true;
        }
    }
}

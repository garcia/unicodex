using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Unicodex.Model;

namespace Unicodex
{
    public class Unicodex
    {
        private Dictionary<string, List<Character>> WordsCache = new Dictionary<string, List<Character>>();
        private Dictionary<char, List<Character>> FirstLetterOfNameCache = new Dictionary<char, List<Character>>();
        private Dictionary<char, List<Character>> FirstLettersOfWordsCache = new Dictionary<char, List<Character>>();

        public Unicodex()
        {
            using (StringReader unicodeDataLines = new StringReader(Properties.Resources.UnicodeData))
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
            // Add to first-letter-of-name cache
            char firstLetterOfName = c.Name[0];
            if (!FirstLetterOfNameCache.ContainsKey(firstLetterOfName))
            {
                FirstLetterOfNameCache[firstLetterOfName] = new List<Character>();
            }
            List<Character> charsByFirstLetter = FirstLetterOfNameCache[firstLetterOfName];
            charsByFirstLetter.Add(c);

            foreach (string word in c.NameWords)
            {
                // Add to words cache
                if (!WordsCache.ContainsKey(word))
                {
                    WordsCache[word] = new List<Character>();
                }
                List<Character> charsByWord = WordsCache[word];
                charsByWord.Add(c);

                // Add to first-letters-of-words cache - but not if this is the first word
                if (word == c.NameWords[0]) continue;

                char firstLetterOfWord = word[0];
                if (!FirstLettersOfWordsCache.ContainsKey(firstLetterOfWord))
                {
                    FirstLettersOfWordsCache[firstLetterOfWord] = new List<Character>();
                }
                List<Character> charsByFirstLetters = FirstLettersOfWordsCache[firstLetterOfWord];
                charsByFirstLetters.Add(c);
            }
        }

        private IEnumerable<Character> SearchByWords(string[] queryWords)
        {
            foreach (string queryWord in queryWords)
            {
                if (WordsCache.ContainsKey(queryWord))
                {
                    List<Character> charsByWord = WordsCache[queryWord];
                    foreach (Character c in charsByWord)
                    {
                        if (Matches(queryWords, c))
                        {
                            yield return c;
                        }
                    }
                }
            }
        }

        private IEnumerable<Character> SearchByFirstLetterOfName(string[] queryWords)
        {
            foreach (string queryWord in queryWords)
            {
                char firstLetterOfQueryWord = queryWord[0];
                if (FirstLetterOfNameCache.ContainsKey(firstLetterOfQueryWord))
                {
                    List<Character> charsByFirstLetter = FirstLetterOfNameCache[firstLetterOfQueryWord];
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
                if (FirstLettersOfWordsCache.ContainsKey(firstLetterOfQueryWord))
                {
                    List<Character> charsByFirstLetter = FirstLettersOfWordsCache[firstLetterOfQueryWord];
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

            IEnumerable<Character> aggregatedQuery = SearchByWords(queryWords);
            aggregatedQuery = aggregatedQuery.Concat(SearchByFirstLetterOfName(queryWords));
            aggregatedQuery = aggregatedQuery.Concat(SearchByFirstLettersOfWords(queryWords));

            foreach (Character c in aggregatedQuery)
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
            foreach (string queryWord in queryWords)
            {
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

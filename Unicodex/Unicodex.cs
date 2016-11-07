using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using Unicodex.Model;

namespace Unicodex
{
    public class Unicodex
    {
        private Cache[] Caches = new Cache[]
        {
            //new NameCache(),
            new FirstWordCache(),
            new AllWordsCache(),
            new FirstLetterOfFirstWordCache(),
            new FirstLetterOfAllWordsCache(),
        };

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
                    foreach (Cache cache in Caches)
                    {
                        cache.Add(c);
                    }
                }
            }
        }

        public ObservableCollection<View.Character> Search(Query query)
        {
            HashSet<Character> seenCharacters = new HashSet<Character>();
            ObservableCollection<View.Character> results = new ObservableCollection<View.Character>();

            if (query.QueryText.Length == 0)
            {
                return results;
            }

            IEnumerable<Character> aggregatedQuery = null;
            foreach (Cache cache in Caches)
            {
                if (aggregatedQuery == null)
                {
                    aggregatedQuery = cache.Search(query);
                }
                else
                {
                    aggregatedQuery = aggregatedQuery.Concat(cache.Search(query));
                }
            }

            foreach (Character cacheHit in aggregatedQuery)
            {
                if (query.Matches(cacheHit) && !seenCharacters.Contains(cacheHit))
                {
                    results.Add(new View.Character(cacheHit));
                    seenCharacters.Add(cacheHit);
                    if (results.Count >= 50) break;
                }
            }

            return results;
        }
    }
}

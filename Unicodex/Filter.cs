﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Unicodex.Model;
using Unicodex.Properties;

namespace Unicodex
{
    public class Filter
    {
        private List<Character> allCharacters = new List<Character>();

        private Cache[] Caches = new Cache[]
        {
            new FavoritesCache(Properties.Settings.Default.Favorites),
            new TagsCache(((App)Application.Current).TagGroups),
            //new NameCache(),
            //new FirstWordCache(),
            new AllWordsCache(),
            //new FirstLetterOfFirstWordCache(),
            new FirstLetterOfAllWordsCache(),
            new CodepointCache(),
        };

        public bool ReturnAllCharactersOnEmptyQuery { get; set; } = false;

        public Character GetByCodepoint(string codepoint)
        {
            foreach (Cache cache in Caches)
            {
                if (cache is CodepointCache)
                {
                    return cache.Items[codepoint][0];
                }
            }
            return null;
        }

        public void Add(Character c)
        {
            allCharacters.Add(c);

            foreach (Cache cache in Caches)
            {
                cache.Add(c);
            }
        }

        public ObservableCollection<View.Character> Search(Query query)
        {
            HashSet<Character> seenCharacters = new HashSet<Character>();
            ObservableCollection<View.Character> results = new ObservableCollection<View.Character>();

            // Handle empty query
            if (query.QueryText.Length == 0)
            {
                if (ReturnAllCharactersOnEmptyQuery)
                {
                    foreach (Character c in allCharacters)
                    {
                        results.Add(new View.Character(c));
                    }
                }
                return results;
            }

            // Create aggregated query of all of the caches
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
                if (!seenCharacters.Contains(cacheHit))
                {
                    results.Add(new View.Character(cacheHit));
                    seenCharacters.Add(cacheHit);
                    if (results.Count >= Settings.Default.Preferences.maxSearchResults) break;
                }
            }

            return results;
        }
    }
}

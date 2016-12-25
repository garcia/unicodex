using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Unicodex.Model;
using Unicodex.Properties;

namespace Unicodex
{
    public class Filter<ModelT> where ModelT : SplitString
    {
        private List<ModelT> allItems = new List<ModelT>();
        private Cache<ModelT>[] Caches;

        public Filter(Cache<ModelT>[] caches)
        {
            Caches = caches;
        }

        public bool ReturnAllCharactersOnEmptyQuery { get; set; } = false;

        public ModelT GetByCodepoint(string codepoint)
        {
            foreach (Cache<ModelT> cache in Caches)
            {
                if (cache is CodepointCache)
                {
                    return cache.Items[codepoint][0];
                }
            }
            return null;
        }

        public void Add(ModelT m)
        {
            allItems.Add(m);

            foreach (Cache<ModelT> cache in Caches)
            {
                cache.Add(m);
            }
        }

        public IEnumerable<ModelT> Search(Query query)
        {
            HashSet<ModelT> seenItems = new HashSet<ModelT>();
            List<ModelT> results = new List<ModelT>();

            // Handle empty query
            if (query.QueryText.Length == 0)
            {
                if (ReturnAllCharactersOnEmptyQuery)
                {
                    results.AddRange(allItems);
                }
                return results;
            }

            // Create aggregated query of all of the caches
            IEnumerable<ModelT> aggregatedQuery = null;
            foreach (Cache<ModelT> cache in Caches)
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

            foreach (ModelT cacheHit in aggregatedQuery)
            {
                if (!seenItems.Contains(cacheHit))
                {
                    results.Add(cacheHit);
                    seenItems.Add(cacheHit);
                    if (results.Count >= Settings.Default.Preferences.maxSearchResults) break;
                }
            }

            return results;
        }
    }
}

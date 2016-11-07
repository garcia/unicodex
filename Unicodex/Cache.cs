using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Unicodex.Model;

namespace Unicodex
{
    abstract class Cache
    {
        private Dictionary<string, List<Character>> cache { get; set; }

        public Cache()
        {
            cache = new Dictionary<string, List<Character>>();
        }

        public void Add(Character c)
        {
            foreach (string key in GetKeys(c))
            {
                if (!cache.ContainsKey(key))
                {
                    cache[key] = new List<Character>();
                }
                List<Character> cacheEntry = cache[key];
                cacheEntry.Add(c);
            }
        }

        public IEnumerable<Character> Search(SplitString input)
        {
            foreach (string queryKey in GetKeys(input))
            {
                if (cache.ContainsKey(queryKey))
                {
                    List<Character> cacheHits = cache[queryKey];
                    foreach (Character cacheHit in cacheHits)
                    {
                        yield return cacheHit;
                    }
                }
            }
        }

        public abstract IEnumerable<string> GetKeys(SplitString s);
    }

    class NameCache : Cache
    {
        public override IEnumerable<string> GetKeys(SplitString s)
        {
            yield return s.Unsplit;
        }
    }

    class FirstWordCache : Cache
    {
        public override IEnumerable<string> GetKeys(SplitString s)
        {
            yield return s.Split[0];
        }
    }

    class AllWordsCache : Cache
    {
        public override IEnumerable<string> GetKeys(SplitString s)
        {
            foreach (string word in s.Split)
            {
                yield return word;
            }
        }
    }

    class FirstLetterOfFirstWordCache : Cache
    {
        public override IEnumerable<string> GetKeys(SplitString s)
        {
            yield return s.Unsplit[0].ToString();
        }
    }

    class FirstLetterOfAllWordsCache : Cache
    {
        public override IEnumerable<string> GetKeys(SplitString s)
        {
            foreach (string word in s.Split)
            {
                yield return word[0].ToString();
            }
        }
    }
}

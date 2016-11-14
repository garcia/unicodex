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
        public Dictionary<string, List<Character>> Items { get; private set; }

        public Cache()
        {
            Items = new Dictionary<string, List<Character>>();
        }

        public void Add(Character c)
        {
            foreach (string key in GetKeys(c))
            {
                if (!Items.ContainsKey(key))
                {
                    Items[key] = new List<Character>();
                }
                List<Character> cacheEntry = Items[key];
                cacheEntry.Add(c);
            }
        }

        public IEnumerable<Character> Search(Query query)
        {
            foreach (string queryKey in GetQueryKeys(query))
            {
                if (Items.ContainsKey(queryKey))
                {
                    List<Character> cacheHits = Items[queryKey];
                    foreach (Character cacheHit in cacheHits)
                    {
                        if (Matches(query, cacheHit)) yield return cacheHit;
                    }
                }
            }
        }

        public abstract IEnumerable<string> GetKeys(SplitString s);

        public virtual IEnumerable<string> GetQueryKeys(SplitString s)
        {
            return GetKeys(s);
        }

        public virtual bool Matches(Query query, Character cacheHit)
        {
            return query.Matches(cacheHit);
        }
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

        public override bool Matches(Query query, Character cacheHit)
        {
            return cacheHit.NameWords[0].StartsWith(query.QueryWords[0]) && base.Matches(query, cacheHit);
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

    class CodepointCache : Cache
    {
        public override IEnumerable<string> GetKeys(SplitString s)
        {
            Character c = (Character)s;
            yield return c.CodepointHex;
        }

        public override IEnumerable<string> GetQueryKeys(SplitString s)
        {
            yield return s.Unsplit.PadLeft(4, '0');
        }

        public override bool Matches(Query query, Character cacheHit)
        {
            return true;
        }
    }
}

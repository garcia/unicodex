using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unicodex.Model;

namespace Unicodex
{
    public abstract class Cache<T> where T : SplitString
    {
        public Dictionary<string, List<T>> Items { get; private set; }

        public Cache()
        {
            Items = new Dictionary<string, List<T>>();
        }

        public virtual void Add(T t)
        {
            foreach (string key in GetKeys(t))
            {
                string upperKey = key.ToUpper();
                if (!Items.ContainsKey(upperKey))
                {
                    Items[upperKey] = new List<T>();
                }
                List<T> cacheEntry = Items[upperKey];
                cacheEntry.Add(t);
            }
        }

        public IEnumerable<T> Search(Query query)
        {
            foreach (string queryKey in GetQueryKeys(query))
            {
                if (Items.ContainsKey(queryKey))
                {
                    List<T> cacheHits = Items[queryKey];
                    foreach (T cacheHit in cacheHits)
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

        public virtual bool Matches(Query query, T cacheHit)
        {
            return query.Matches(cacheHit);
        }
    }

    class NameCache<T> : Cache<T> where T : SplitString
    {
        public override IEnumerable<string> GetKeys(SplitString s)
        {
            yield return s.Unsplit;
        }
    }

    class FirstWordCache<T> : Cache<T> where T : SplitString
    {
        public override IEnumerable<string> GetKeys(SplitString s)
        {
            yield return s.Split[0];
        }
    }

    class AllWordsCache<T> : Cache<T> where T : SplitString
    {
        public override IEnumerable<string> GetKeys(SplitString s)
        {
            foreach (string word in s.Split)
            {
                yield return word;
            }
        }
    }

    class FirstLetterOfFirstWordCache<T> : Cache<T> where T : SplitString
    {
        public override IEnumerable<string> GetKeys(SplitString s)
        {
            yield return s.Unsplit[0].ToString();
        }

        public override bool Matches(Query query, T cacheHit)
        {
            return cacheHit.Split[0].StartsWith(query.QueryFragments[0]) && base.Matches(query, cacheHit);
        }
    }

    class FirstLetterOfAllWordsCache<T> : Cache<T> where T : SplitString
    {
        public override IEnumerable<string> GetKeys(SplitString s)
        {
            foreach (string word in s.Split)
            {
                yield return word[0].ToString();
            }
        }
    }

    class CodepointCache : Cache<Character>
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

    class FavoritesCache : Cache<Character>
    {
        private Favorites favorites;

        public FavoritesCache(Favorites favorites) : base()
        {
            this.favorites = favorites;
        }

        public override void Add(Character c)
        {
            if (favorites.IsFavorite(c.CodepointHex))
            {
                base.Add(c);
            }
        }

        public override IEnumerable<string> GetKeys(SplitString s)
        {
            foreach (string word in s.Split)
            {
                yield return word[0].ToString();
            }
        }
    }

    class TagsCache : Cache<Character>
    {
        private TagGroups tagGroups;

        public TagsCache(TagGroups tagGroups) : base()
        {
            this.tagGroups = tagGroups;
        }

        public override IEnumerable<string> GetKeys(SplitString s)
        {
            Character c = (Character)s;
            List<string> tags = tagGroups.GetTags(c.CodepointHex);
            foreach (string tag in tags)
            {
                yield return tag.ToUpper();
            }
        }

        public override IEnumerable<string> GetQueryKeys(SplitString s)
        {
            foreach (string word in s.Split)
            {
                yield return word.TrimStart('#');
            }
        }
    }
}

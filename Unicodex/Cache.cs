using System;
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

        /* This Cache.Matches implementation doesn't defer to Query.Matches
         * because the penalty for checking extraneous cache hits can be severe.
         * For example, if the user types a query consisting of each letter as a
         * word ("a b c d e..."), performance without this override degrades as
         * nearly every character yields a cache hit.
         * 
         * The concern that a character might match a query for multiple reasons
         * (perhaps one query fragment matches the character's name and another
         * matches a tag) is valid - but only one cache needs to return a match
         * in such cases. In this situation, FirstLetterOfAllWordsCache defers
         * the responsibility to other caches to return the match. */
        public override bool Matches(Query query, T cacheHit)
        {
            foreach (string fragment in query.Split)
            {
                foreach (string nameWord in cacheHit.Split)
                {
                    if (!nameWord.StartsWith(fragment, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }
            return true;
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
            List<Tag> tags = tagGroups.GetTags(c.CodepointHex);
            foreach (Tag tag in tags)
            {
                yield return tag.TagName.ToUpper();
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

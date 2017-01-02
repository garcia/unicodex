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

    /* Find character by exact match of its whole name.  Most useful for finding
     * characters with short names, e.g. U+15F25 'FIRE'. */
    class NameCache<T> : Cache<T> where T : SplitString
    {
        public override IEnumerable<string> GetKeys(SplitString s)
        {
            yield return s.Unsplit;
        }
    }

    /* Find character by exact match of the first word in its name. */
    class FirstWordCache<T> : Cache<T> where T : SplitString
    {
        public override IEnumerable<string> GetKeys(SplitString s)
        {
            yield return s.Split[0];
        }
    }

    /* Find character by exact match of any word in its name. */
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

    /* Find character by partial match of the first word in its name. */
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

    /* Find a character by partial match of any word in its name. This is the
     * primary search mechanism and should be carefully optimized to deal with
     * large numbers of cache hits. */
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
                bool foundMatch = false;
                foreach (string nameWord in cacheHit.Split)
                {
                    if (nameWord.StartsWith(fragment, StringComparison.OrdinalIgnoreCase))
                    {
                        foundMatch = true;
                        break;
                    }
                }
                if (!foundMatch) return false;
            }
            return true;
        }
    }

    /* Find character by its hexadecimal codepoint. */
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

    /* Find favorited character by partial match of any word in its name.
     * Very similar to FirstLetterOfAllWordsCache, but restricted to favorites
     * for prioritization. */
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

    /* Find character by its tags.
     * TODO: support partial matches. */
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

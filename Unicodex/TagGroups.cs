using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Unicodex.Model;
using Unicodex.Properties;

namespace Unicodex
{
    [Serializable]
    public abstract class TagGroup
    {
        [XmlIgnore]
        public Dictionary<string, List<string>> TagToCodepoints { get; private set; }

        [XmlIgnore]
        public Dictionary<string, List<string>> CodepointToTags { get; private set; }

        [XmlIgnore]
        public abstract string Source { get; }

        public TagGroup()
        {
            TagToCodepoints = new Dictionary<string, List<string>>();
            CodepointToTags = new Dictionary<string, List<string>>();
        }

        public IEnumerable<Tag> GetTags(string codepoint)
        {
            if (CodepointToTags.ContainsKey(codepoint))
            {
                List<Tag> tags = new List<Tag>();
                foreach (string tag in CodepointToTags[codepoint])
                {
                    tags.Add(new Tag(tag, this));
                }
                return tags;
            }
            else
            {
                return Enumerable.Empty<Tag>();
            }

        }

        public IEnumerable<string> GetCodepoints(string tag)
        {
            if (TagToCodepoints.ContainsKey(tag))
            {
                return TagToCodepoints[tag];
            }
            else
            {
                return Enumerable.Empty<string>();
            }
        }

        public void AddTag(string codepoint, string tag)
        {
            /* Make sure this tag doesn't already exist. Why not use a set?
             * Because the user might care about the order of their tags. */
            if (CodepointToTags.ContainsKey(codepoint))
            {
                string upperTag = tag.ToUpper();
                foreach (string existingTag in CodepointToTags[codepoint])
                {
                    if (upperTag == existingTag.ToUpper()) return;
                }
            }

            if (!TagToCodepoints.ContainsKey(tag))
            {
                TagToCodepoints[tag] = new List<string>();
            }
            TagToCodepoints[tag].Add(codepoint);

            if (!CodepointToTags.ContainsKey(codepoint))
            {
                CodepointToTags[codepoint] = new List<string>();
            }
            CodepointToTags[codepoint].Add(tag);
        }

        public void RemoveTag(string codepoint, string tag)
        {
            if (TagToCodepoints.ContainsKey(tag))
            {
                TagToCodepoints[tag].Remove(codepoint);
            }

            if (CodepointToTags.ContainsKey(codepoint))
            {
                CodepointToTags[codepoint].Remove(tag);
            }
        }

        public virtual bool IsEnabled()
        {
            return true;
        }
    }

    public class TagGroups
    {
        public TagGroup UserTags { get; private set; }
        public TagGroup BlockTags { get; private set; }
        public TagGroup CategoryTags { get; private set; }
        public TagGroup EmojiTags { get; private set; }
        public TagGroup AliasTags { get; private set; }

        private TagGroup[] AllTags;

        public TagGroups(Characters characters)
        {
            if (Settings.Default.UserTags == null)
            {
                Settings.Default.UserTags = new UserTags();
            }
            UserTags = Settings.Default.UserTags;

            BlockTags = new BlockTags(characters);
            CategoryTags = new CategoryTags(characters);
            EmojiTags = new EmojiTags();
            AliasTags = new AliasTags();

            AllTags = new TagGroup[] { BlockTags, CategoryTags, EmojiTags, AliasTags, UserTags };
        }

        public List<string> GetCodepoints(string tag)
        {
            List<string> results = new List<string>();

            foreach (TagGroup tagGroup in AllTags)
            {
                results.AddRange(tagGroup.GetCodepoints(tag));
            }

            return results;
        }

        public List<Tag> GetTags(string codepoint)
        {
            List<Tag> results = new List<Tag>();

            foreach (TagGroup tagGroup in AllTags)
            {
                if (tagGroup.IsEnabled())
                {
                    results.AddRange(tagGroup.GetTags(codepoint));
                }
            }

            return results;
        }

        internal IEnumerable<Tag> GetAllTags()
        {
            List<Tag> results = new List<Tag>();

            // Reverse tag groups as lazy fix to show user tags first on the Tags tab
            foreach (TagGroup tagGroup in AllTags.Reverse())
            {
                if (tagGroup.IsEnabled())
                {
                    foreach (string tagName in tagGroup.TagToCodepoints.Keys)
                    {
                        results.Add(new Tag(tagName, tagGroup));
                    }
                }
            }

            return results;
        }
    }

    public class BlockTags : TagGroup
    {
        public override string Source { get { return "Block"; } }

        public BlockTags(Characters characters) : base()
        {
            using (StringReader blockDataLines = new StringReader(Properties.Resources.Blocks))
            {
                string blockDataLine = string.Empty;
                while (true)
                {
                    blockDataLine = blockDataLines.ReadLine();
                    if (blockDataLine == null) break;
                    if (blockDataLine.Length == 0 || blockDataLine.StartsWith("#")) continue;

                    string[] lineSegments = blockDataLine.Split(';');
                    string[] endpoints = lineSegments[0].Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                    string blockName = lineSegments[1].Trim();
                    int start = Convert.ToInt32(endpoints[0], 16);
                    int end = Convert.ToInt32(endpoints[1], 16);

                    foreach (int codepoint in Enumerable.Range(start, end - start + 1))
                    {
                        string codepointHex = codepoint.ToString("X4");
                        if (characters.AllCharactersByCodepointHex.ContainsKey(codepointHex))
                        {
                            AddTag(codepointHex, blockName);
                        }
                    }
                }
            }
        }

        public override bool IsEnabled()
        {
            return Settings.Default.Preferences.builtInTagsBlock;
        }
    }

    public class CategoryTags : TagGroup
    {
        public override string Source { get { return "Category"; } }

        public CategoryTags(Characters characters) : base()
        {
            foreach (Character c in characters.AllCharacters)
            {
                AddTag(c.CodepointHex, c.Category);
            }
    }

        public override bool IsEnabled()
        {
            return Settings.Default.Preferences.builtInTagsCategory;
        }
    }

    public class EmojiTags : TagGroup
    {
        public override string Source { get { return "Emoji"; } }

        public override bool IsEnabled()
        {
            return Settings.Default.Preferences.builtInTagsEmoji;
        }
    }

    public class AliasTags : TagGroup
    {
        public override string Source { get { return "Built-in alias"; } }

        public override bool IsEnabled()
        {
            return Settings.Default.Preferences.builtInTagsAlias;
        }
    }
}
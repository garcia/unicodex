using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unicodex.Model;
using Unicodex.Properties;

namespace Unicodex
{
    public class TagGroups
    {
        public Tags UserTags { get; private set; }
        public Tags MiscellaneousTags { get; private set; }
        public Tags BlockTags { get; private set; }
        public Tags CategoryTags { get; private set; }

        private Tags[] AllTags;

        public TagGroups(Characters characters)
        {
            if (Settings.Default.UserTags == null)
            {
                Settings.Default.UserTags = new Tags();
            }
            UserTags = Settings.Default.UserTags;

            MiscellaneousTags = new Tags();
            BlockTags = new Tags();
            CategoryTags = new Tags();

            populateCategoryTags(characters);
            populateBlockTags();

            AllTags = new Tags[] { BlockTags, CategoryTags, MiscellaneousTags, UserTags };
        }

        private void populateCategoryTags(Characters characters)
        {
            foreach (Character c in characters.AllCharacters)
            {
                CategoryTags.AddTag(c.CodepointHex, c.Category);
            }
        }

        private void populateBlockTags()
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
                        BlockTags.AddTag(codepoint.ToString("X4"), blockName);
                    }
                }
            }
        }

        public List<string> GetCodepoints(string tag)
        {
            List<string> results = new List<string>();

            foreach (Tags tagGroup in AllTags)
            {
                results.AddRange(tagGroup.GetCodepoints(tag));
            }

            return results;
        }

        public List<string> GetTags(string codepoint)
        {
            List<string> results = new List<string>();

            foreach (Tags tagGroup in AllTags)
            {
                results.AddRange(tagGroup.GetTags(codepoint));
            }

            return results;
        }

    }
}
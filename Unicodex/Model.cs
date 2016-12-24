using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace Unicodex.Model
{
    public abstract class SplitString
    {
        public abstract string Unsplit { get; }
        public abstract string[] Split { get; }
    }

    public class UnicodeDataEntry
    {
        public string Codepoint { get; private set; }
        public string Name { get; private set; }
        public string GeneralCategory { get; private set; }
        public string CanonicalCombiningClass { get; private set; }
        public string BidiClass { get; private set; }
        public string Decomposition { get; private set; }
        public string NumericDecimalDigit { get; private set; }
        public string NumericDigit { get; private set; }
        public string Numeric { get; private set; }
        public string BidiMirrored { get; private set; }
        public string Unicode1Name { get; private set; }
        public string ISOComment { get; private set; }
        public string SimpleUppercaseMapping { get; private set; }
        public string SimpleLowercaseMapping { get; private set; }
        public string SimpleTitlecaseMapping { get; private set; }

        public UnicodeDataEntry(string unicodeDataLine)
        {
            string[] components = unicodeDataLine.Split(new char[] { ';' });
            Codepoint = components[0];
            Name = components[1];
            GeneralCategory = components[2];
            CanonicalCombiningClass = components[3];
            BidiClass = components[4];
            Decomposition = components[5];
            NumericDecimalDigit = components[6];
            NumericDigit = components[7];
            Numeric = components[8];
            BidiMirrored = components[9];
            Unicode1Name = components[10];
            ISOComment = components[11];
            SimpleUppercaseMapping = components[12];
            SimpleLowercaseMapping = components[13];
            SimpleTitlecaseMapping = components[14];
        }
    }

    public class Character : SplitString
    {
        public string Value { get; private set; }
        public int Codepoint { get; private set; }
        public string Name { get; private set; }
        public string[] NameWords { get; private set; }
        public string Category { get; private set; }

        public string CodepointHex { get { return Codepoint.ToString("X4"); } }
        public override string Unsplit { get { return Name; } }
        public override string[] Split { get { return NameWords; } }

        public Character(UnicodeDataEntry entry)
        {
            Codepoint = Convert.ToInt32(entry.Codepoint, 16);

            // Skip isolated surrogate codepoints
            if (Codepoint >= 0xd800 && Codepoint <= 0xdfff) Value = "";
            else Value = char.ConvertFromUtf32(Codepoint);


            Name = entry.Name;
            if (Name.Equals("<control>") && entry.Unicode1Name.Length > 0)
            {
                Name = entry.Unicode1Name;
            }

            NameWords = Name.Split(new char[] { ' ' });

            Category = entry.GeneralCategory;
        }
    }

    public class Query : SplitString
    {
        public string QueryText { get; private set; }
        public string[] QueryFragments { get; private set; }

        public override string Unsplit { get { return QueryText; } }
        public override string[] Split { get { return QueryFragments; } }

        public Query(string text)
        {
            QueryFragments = Fragmentize(text);
            QueryText = string.Join(" ", QueryFragments);
        }

        private string[] Fragmentize(string text)
        {
            List<string> result = new List<string>();
            string[] words = text.ToUpper().Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

            List<string> partialQuotedString = null;
            foreach (string word in words)
            {
                // Parse quoted substrings as single fragments
                if (partialQuotedString == null && word.StartsWith("\""))
                {
                    partialQuotedString = new List<string>();
                }

                // Parsing a quoted string - wait until we find the closing "
                if (partialQuotedString != null)
                {
                    partialQuotedString.Add(word);
                    if (word.EndsWith("\""))
                    {
                        AddQuotedString(result, partialQuotedString);
                        partialQuotedString = null;
                    }
                }
                // Not parsing a quoted string - the word itself is a fragment
                else
                {
                    result.Add(word);
                }
            }

            /* If there's an unmatched quotation mark (e.g. if the user is
             * still typing a quoted string), add the partial string anyway. */
            if (partialQuotedString != null)
            {
                AddQuotedString(result, partialQuotedString);
            }

            return result.ToArray();
        }

        private void AddQuotedString(List<string> result, List<string> partialQuotedString)
        {
            string quotedString = string.Join(" ", partialQuotedString).Trim('"', ' ');
            
            // Prevent empty quoted strings from being added as fragments
            if (quotedString.Length > 0)
            {
                result.Add(quotedString);
            }
        }

        public bool Matches(Character c)
        {
            foreach (string queryFragment in QueryFragments)
            {
                bool matchesQueryFragment = false;
                foreach (string nameWord in c.NameWords)
                {
                    if (nameWord.StartsWith(queryFragment))
                    {
                        matchesQueryFragment = true;
                        break;
                    }
                }
                if (!matchesQueryFragment)
                {
                    // This query fragment doesn't match the character name.
                    // Check the codepoint:
                    if (c.CodepointHex == queryFragment) continue;

                    // Check the tags:
                    List<string> tags = ((App)Application.Current).TagGroups.GetTags(c.CodepointHex);
                    bool foundMatchingTag = false;
                    foreach (string tag in tags)
                    {
                        if (tag.ToUpper() == queryFragment.TrimStart('#'))
                        {
                            foundMatchingTag = true;
                            break;
                        }
                    }
                    if (foundMatchingTag) continue;

                    // No matches found - cache miss.
                    return false;
                }
            }
            return true;
        }
    }
}

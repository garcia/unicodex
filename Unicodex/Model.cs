using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public string[] QueryWords { get; private set; }

        public override string Unsplit { get { return QueryText; } }
        public override string[] Split { get { return QueryWords; } }

        public Query(string text)
        {
            QueryWords = text.ToUpper().Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            QueryText = string.Join(" ", QueryWords);
        }

        public bool Matches(Character c)
        {
            foreach (string queryWord in QueryWords)
            {
                bool matchesQueryWord = false;
                foreach (string nameWord in c.NameWords)
                {
                    if (nameWord.StartsWith(queryWord))
                    {
                        matchesQueryWord = true;
                        break;
                    }
                }
                if (!matchesQueryWord)
                {
                    if (c.CodepointHex != queryWord) return false;
                }
            }
            return true;
        }
    }
}

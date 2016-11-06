using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unicodex.Model
{
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

    public class Character
    {
        public string Value { get; private set; }
        public int Codepoint { get; private set; }
        public string Name { get; private set; }
        public string[] NameWords { get; private set; }

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
        }
    }
}

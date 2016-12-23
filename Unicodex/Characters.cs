using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unicodex.Model;

namespace Unicodex
{
    public class Characters
    {
        public List<Character> AllCharacters { get; private set; }
        public Dictionary<string, Character> AllCharactersByCodepointHex { get; private set; }

        public Characters()
        {
            AllCharacters = new List<Character>();
            AllCharactersByCodepointHex = new Dictionary<string, Character>();

            using (StringReader unicodeDataLines = new StringReader(Properties.Resources.UnicodeData))
            {
                string unicodeDataLine = string.Empty;
                while (true)
                {
                    unicodeDataLine = unicodeDataLines.ReadLine();
                    if (unicodeDataLine == null) break;
                    UnicodeDataEntry entry = new UnicodeDataEntry(unicodeDataLine);
                    Character c = new Character(entry);
                    AllCharacters.Add(c);
                    AllCharactersByCodepointHex[c.CodepointHex] = c;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Markup;
using System.Xml.Serialization;

namespace Unicodex
{
    [Serializable]
    public class Preferences
    {
        public Boolean runOnStartup { get; set; }
        public int maxSearchResults { get; set; }
        public Boolean globalHotkeyCtrl { get; set; }
        public Boolean globalHotkeyAlt { get; set; }
        public Boolean globalHotkeyShift { get; set; }
        public Boolean globalHotkeyWin { get; set; }
        public Key globalHotkeyNonModifier { get; set; }
        public Boolean spawnNearTextCaret { get; set; }
        public SpawnPlacement spawnPlacement { get; set; }
        public PlacementSide windowPlacement { get; set; }
        public PlacementInOut insideOutsidePlacement { get; set; }
        public PlacementSide monitorPlacement { get; set; }

        public Preferences()
        {
            runOnStartup = true;
            maxSearchResults = 50;
            globalHotkeyCtrl = true;
            globalHotkeyAlt = false;
            globalHotkeyShift = true;
            globalHotkeyWin = false;
            globalHotkeyNonModifier = Key.U;
            spawnNearTextCaret = true;
            spawnPlacement = SpawnPlacement.SPAWN_NEAR_CURSOR;
            windowPlacement = PlacementSide.CENTER;
            insideOutsidePlacement = PlacementInOut.INSIDE;
            monitorPlacement = PlacementSide.CENTER;
        }
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum SpawnPlacement
    {
        [Description("Spawn near cursor")]
        SPAWN_NEAR_CURSOR,
        [Description("Spawn relative to active window")]
        SPAWN_NEAR_WINDOW,
        [Description("Spawn relative to active monitor")]
        SPAWN_IN_MONITOR
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum PlacementSide
    {
        [Description("Top-left")]
        TOP_LEFT,
        [Description("Top-center")]
        TOP_CENTER,
        [Description("Top-right")]
        TOP_RIGHT,
        [Description("Center-left")]
        CENTER_LEFT,
        [Description("Center")]
        CENTER,
        [Description("Center-right")]
        CENTER_RIGHT,
        [Description("Bottom-left")]
        BOTTOM_LEFT,
        [Description("Bottom-center")]
        BOTTOM_CENTER,
        [Description("Bottom-right")]
        BOTTOM_RIGHT
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum PlacementInOut
    {
        [Description("Inside")]
        INSIDE,
        [Description("Outside")]
        OUTSIDE
    }

    [Serializable]
    public class Favorites : INotifyCollectionChanged
    {
        public HashSet<string> FavoriteSet { get; private set; }
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public Favorites()
        {
            FavoriteSet = new HashSet<string>();
            
        }

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            CollectionChanged?.Invoke(this, notifyCollectionChangedEventArgs);
        }

        public bool IsFavorite(string hexCodepoint)
        {
            return FavoriteSet.Contains(hexCodepoint);
        }

        public bool AddFavorite(string hexCodepoint)
        {
            bool added= FavoriteSet.Add(hexCodepoint);
            if (added)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, hexCodepoint));
            }
            return added;
        }

        public bool RemoveFavorite(string hexCodepoint)
        {
            bool removed = FavoriteSet.Remove(hexCodepoint);
            if (removed)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, hexCodepoint));
            }
            return removed;
        }
    }

    [Serializable]
    public class Tags
    {
        [XmlIgnore]
        public Dictionary<string, List<string>> TagToCodepoints { get; private set; }

        [XmlIgnore]
        public Dictionary<string, List<string>> CodepointToTags { get; private set; }

        public TagPair[] TagPairs
        {
            get
            {
                List<TagPair> pairs = new List<TagPair>();
                foreach (string codepoint in CodepointToTags.Keys)
                {
                    foreach (string tag in CodepointToTags[codepoint])
                    {
                        pairs.Add(new TagPair(codepoint, tag));
                    }
                }
                return pairs.ToArray();
            }
            set
            {
                foreach (TagPair pair in value)
                {
                    AddTag(pair.Codepoint, pair.Tag);
                }
            }
        }

        public Tags()
        {
            TagToCodepoints = new Dictionary<string, List<string>>();
            CodepointToTags = new Dictionary<string, List<string>>();
        }

        public IEnumerable<string> GetTags(string codepoint)
        {
            if (CodepointToTags.ContainsKey(codepoint))
            {
                return CodepointToTags[codepoint];
            }
            else
            {
                return Enumerable.Empty<string>();
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
    }

    [Serializable]
    public class TagPair
    {
        public string Codepoint { get; set; }
        public string Tag { get; set; }

        public TagPair() { }

        public TagPair(string codepoint, string tag)
        {
            Codepoint = codepoint;
            Tag = tag;
        }
    }
}

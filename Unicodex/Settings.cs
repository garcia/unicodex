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
        public bool runOnStartup { get; set; }
        public int maxSearchResults { get; set; }
        public bool globalHotkeyCtrl { get; set; }
        public bool globalHotkeyAlt { get; set; }
        public bool globalHotkeyShift { get; set; }
        public bool globalHotkeyWin { get; set; }
        public Key globalHotkeyNonModifier { get; set; }
        public bool spawnNearTextCaret { get; set; }
        public SpawnPlacement spawnPlacement { get; set; }
        public PlacementSide windowPlacement { get; set; }
        public PlacementInOut insideOutsidePlacement { get; set; }
        public PlacementSide monitorPlacement { get; set; }
        public bool builtInTagsBlock { get; set; }
        public bool builtInTagsCategory { get; set; }
        public bool builtInTagsEmoji { get; set; }

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
            builtInTagsBlock = true;
            builtInTagsCategory = false;
            builtInTagsEmoji = true;
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
    public class UserTags : TagGroup
    {
        public override string Source { get { return "User-defined"; } }

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

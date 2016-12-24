using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Markup;

namespace Unicodex
{
    [Serializable]
    public class Preferences
    {
        public Boolean runOnStartup { get; set; }
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
        public Dictionary<string, List<string>> tagToCodepoints { get; private set; }
        public Dictionary<string, List<string>> codepointToTags { get; private set; }

        public Tags()
        {
            tagToCodepoints = new Dictionary<string, List<string>>();
            codepointToTags = new Dictionary<string, List<string>>();
        }

        public IEnumerable<string> GetTags(string codepoint)
        {
            if (codepointToTags.ContainsKey(codepoint))
            {
                return codepointToTags[codepoint];
            }
            else
            {
                return Enumerable.Empty<string>();
            }
            
        }

        public IEnumerable<string> GetCodepoints(string tag)
        {
            if (tagToCodepoints.ContainsKey(tag))
            {
                return tagToCodepoints[tag];
            }
            else
            {
                return Enumerable.Empty<string>();
            }
        }

        public void AddTag(string codepoint, string tag)
        {
            if (!tagToCodepoints.ContainsKey(tag))
            {
                tagToCodepoints[tag] = new List<string>();
            }
            tagToCodepoints[tag].Add(codepoint);

            if (!codepointToTags.ContainsKey(codepoint))
            {
                codepointToTags[codepoint] = new List<string>();
            }
            codepointToTags[codepoint].Add(tag);
        }

        public void RemoveTag(string codepoint, string tag)
        {
            if (tagToCodepoints.ContainsKey(tag))
            {
                tagToCodepoints[tag].Remove(codepoint);
            }

            if (codepointToTags.ContainsKey(codepoint))
            {
                codepointToTags[codepoint].Remove(tag);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Unicodex
{
    [Serializable]
    public class Settings
    {
        public Boolean runOnStartup { get; set; }
        public Boolean globalHotkeyCtrl { get; set; }
        public Boolean globalHotkeyAlt { get; set; }
        public Boolean globalHotkeyShift { get; set; }
        public Boolean globalHotkeyWin { get; set; }
        public String globalHotkeyNonModifier { get; set; }
        public Boolean spawnNearTextCaret { get; set; }
        public SpawnPlacement spawnPlacement { get; set; }
        public PlacementSide windowPlacement { get; set; }
        public PlacementInOut insideOutsidePlacement { get; set; }
        public PlacementSide monitorPlacement { get; set; }

        public Settings()
        {
            runOnStartup = true;
            globalHotkeyCtrl = true;
            globalHotkeyAlt = false;
            globalHotkeyShift = true;
            globalHotkeyWin = false;
            globalHotkeyNonModifier = "U";
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
}

using System.Windows;

namespace DockManagerCore
{
    public static class ResourceKeys
    {
        public static readonly ComponentResourceKey CaptionBarUnselectedBrushKey =
            new ComponentResourceKey(typeof(ResourceKeys), "CaptionBarUnselectedBrush");
        public static readonly ComponentResourceKey CaptionBarActiveBrushKey =
         new ComponentResourceKey(typeof(ResourceKeys), "CaptionBarActiveBrush");
        public static readonly ComponentResourceKey CaptionBarGroupedBrushKey =
         new ComponentResourceKey(typeof(ResourceKeys), "CaptionBarGroupedBrush");

        public static readonly ComponentResourceKey ScreenDockignGridBorderBrushKey =
         new ComponentResourceKey(typeof(ResourceKeys), "ScreenDockingGridBorderBrush");
         
        public static readonly ComponentResourceKey ScreenDockingGridBackgroundBrushKey =
         new ComponentResourceKey(typeof(ResourceKeys), "ScreenDockingGridBackgroundBrush");

        public static readonly ComponentResourceKey DockignGridBorderBrushKey =
 new ComponentResourceKey(typeof(ResourceKeys), "DockingGridBorderBrush");

        public static readonly ComponentResourceKey DockingGridBackgroundBrushKey =
         new ComponentResourceKey(typeof(ResourceKeys), "DockingGridBackgroundBrush");

        public static readonly CornerRadius FloatingWindowBorderCornerRadius = new CornerRadius(5);


        public static readonly double PaneHeaderHeight = 20;
        public static readonly double CaptionBarHeight = PaneHeaderHeight + 5;

    }
}

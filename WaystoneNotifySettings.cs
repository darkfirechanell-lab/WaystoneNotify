using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using System.Collections.Generic;
using Vector4 = System.Numerics.Vector4;

namespace WaystoneNotify
{
    public class WaystoneNotifySettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new(true);

        // Cache intervals (ms)
        public RangeNode<int> InventoryCacheInterval { get; set; } = new(50, 1, 2000);
        public RangeNode<int> StashCacheInterval { get; set; } = new(500, 1, 2000);

        // Display
        public ToggleNode ShowModCount          { get; set; } = new(true);
        public ToggleNode ShowQuantityPercent   { get; set; } = new(true);
        public ToggleNode ShowRarityPercent     { get; set; } = new(true);
        public ToggleNode ShowPackSizePercent   { get; set; } = new(true);
        public ToggleNode ColorQuantityPercent  { get; set; } = new(true);
        public RangeNode<int> ColorQuantity     { get; set; } = new(50, 0, 300);
        public ToggleNode AlwaysShowTooltip     { get; set; } = new(true);
        public ToggleNode ShowModWarnings       { get; set; } = new(true);
        public ToggleNode HorizontalLines       { get; set; } = new(true);
        public RangeNode<int> TooltipOffsetX { get; set; } = new(25, 0, 300);
        public RangeNode<int> TooltipOffsetY { get; set; } = new(0, -200, 200);

        // Border
        public ToggleNode MapBorderStyle        { get; set; } = new(false); // false = Frame, true = Box
        public RangeNode<int> BorderDeflation   { get; set; } = new(4, 0, 50);
        public ToggleNode BoxForMapWarnings     { get; set; } = new(true);
        public ToggleNode BoxForMapBadWarnings  { get; set; } = new(true);
        public ToggleNode BoxForMapGoodMods     { get; set; } = new(true);
        public Vector4 Bricked                  { get; set; } = new(1f, 0f, 0f, 1f);
        public Vector4 MapBorderWarnings        { get; set; } = new(0f, 0.6f, 1f, 1f);
        public Vector4 GoodModBorder            { get; set; } = new(0f, 1f, 0f, 1f);
        public RangeNode<int> BorderThicknessMap{ get; set; } = new(2, 1, 6);

        // Windows
        public ToggleNode ShowBorderInPurchase { get; set; } = new(false);

        // Debug
        public ToggleNode DebugLogMods { get; set; } = new(false);

        // Profile system
        public TextNode ActiveProfile { get; set; } = new("Default");

        // Mod selection — persisted as flat dictionaries keyed by mod type (RawName substring)
        public Dictionary<string, bool> EnabledMods { get; set; } = new();
        public Dictionary<string, bool> BrickedMods { get; set; } = new();
        public Dictionary<string, bool> GoodMods { get; set; } = new();
        public Dictionary<string, string> CustomModNames { get; set; } = new();
    }
}

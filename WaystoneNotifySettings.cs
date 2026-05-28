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
        public Vector4 MapBorderWarnings        { get; set; } = new(0f, 0.6f, 1f, 1f);

        // Brick severity tiers (1 = lowest, 4 = highest)
        public Vector4 Brick1 { get; set; } = new(1.00f, 0.95f, 0.20f, 1f); // amarelo
        public Vector4 Brick2 { get; set; } = new(0.95f, 0.70f, 0.10f, 1f); // amarelo escuro
        public Vector4 Brick3 { get; set; } = new(1.00f, 0.50f, 0.05f, 1f); // laranja
        public Vector4 Brick4 { get; set; } = new(1.00f, 0.10f, 0.10f, 1f); // vermelho

        // Good tiers (1 = lowest, 4 = highest). Hues vary so tiers are easy to tell apart;
        // pure green is reserved for the full-map highlight below.
        public Vector4 Good1 { get; set; } = new(0.800f, 1.000f, 1.000f, 1f); // #ccffff
        public Vector4 Good2 { get; set; } = new(0.686f, 1.000f, 1.000f, 1f); // #afffff
        public Vector4 Good3 { get; set; } = new(0.431f, 0.812f, 0.812f, 1f); // #6ecfcf
        public Vector4 Good4 { get; set; } = new(0.157f, 0.569f, 0.573f, 1f); // #289192

        // Full-map highlight: a waystone with >= FullMapModCount mods is drawn in FullMapColor,
        // regardless of its mods (highest priority).
        public ToggleNode HighlightFullMap { get; set; } = new(true);
        public RangeNode<int> FullMapModCount { get; set; } = new(8, 1, 12);
        public Vector4 FullMapColor { get; set; } = new(0f, 1f, 0f, 1f); // verde puro

        public RangeNode<int> BorderThicknessMap{ get; set; } = new(2, 1, 6);

        public Vector4 BrickColor(int lvl) => lvl switch { 1 => Brick1, 2 => Brick2, 3 => Brick3, _ => Brick4 };
        public Vector4 GoodColor(int lvl)  => lvl switch { 1 => Good1,  2 => Good2,  3 => Good3,  _ => Good4  };

        // Windows
        public ToggleNode ShowBorderInPurchase { get; set; } = new(false);

        // Debug
        public ToggleNode DebugLogMods { get; set; } = new(false);

        // Profile system
        public TextNode ActiveProfile { get; set; } = new("Default");

        // Mod selection — persisted as flat dictionaries keyed by mod type (stat-id substring)
        public Dictionary<string, bool> EnabledMods { get; set; } = new();
        public Dictionary<string, int> BrickLevels { get; set; } = new(); // token -> 1..4
        public Dictionary<string, int> GoodLevels { get; set; } = new();  // token -> 1..4
        public Dictionary<string, string> CustomModNames { get; set; } = new();
    }
}

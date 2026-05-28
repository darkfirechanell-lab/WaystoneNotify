using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.Elements.InventoryElements;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared.Cache;
using ExileCore2.Shared.Enums;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using RectangleF = ExileCore2.Shared.RectangleF;
using nuVector2 = System.Numerics.Vector2;
using nuVector4 = System.Numerics.Vector4;

namespace WaystoneNotify
{
    public partial class WaystoneNotify : BaseSettingsPlugin<WaystoneNotifySettings>
    {
        private RectangleF windowArea;
        private static GameController gameController;
        private static IngameState ingameState;

        private CachedValue<List<NormalInventoryItem>> _inventoryItems;
        private CachedValue<(int stashIndex, List<NormalInventoryItem>)> _stashItems;
        private CachedValue<List<NormalInventoryItem>> _purchaseItems;

        // Profile system
        public static ProfileManager _profileManager;
        public static WaystoneNotifySettings LiveSettings;
        public static List<MapModEntry> _modEntries = new();

        public override bool Initialise()
        {
            Name = "Waystone Mod Notifications";
            gameController = GameController;
            ingameState = gameController.IngameState;
            windowArea = GameController.Window.GetWindowRectangle();

            _inventoryItems = new TimeCache<List<NormalInventoryItem>>(GetInventoryItems, Settings.InventoryCacheInterval);
            _stashItems = new TimeCache<(int, List<NormalInventoryItem>)>(GetStashItems, Settings.StashCacheInterval);
            _purchaseItems = new TimeCache<List<NormalInventoryItem>>(GetPurchaseItems, 500);

            LiveSettings = Settings;
            _modEntries = ModDataLoader.Load(DirectoryFullName);
            _profileManager = new ProfileManager(ConfigDirectory);
            var activeProf = Settings.ActiveProfile.Value;
            if (!_profileManager.Profiles.ContainsKey(activeProf)) activeProf = "Default";
            _profileManager.LoadProfile(activeProf, Settings);
            Settings.ActiveProfile.Value = activeProf;

            return true;
        }

        // ── Item gathering ───────────────────────────────────────────────────
        private static bool IsWaystone(Entity entity)
        {
            if (entity == null || entity.Address == 0 || !entity.IsValid) return false;
            if (!entity.HasComponent<Map>()) return false;
            var baseType = gameController.Files.BaseItemTypes.Translate(entity.Path);
            return baseType?.ClassName == "Map";
        }

        private List<NormalInventoryItem> GetInventoryItems()
        {
            var result = new List<NormalInventoryItem>();
            if (ingameState.IngameUi.InventoryPanel.IsVisible)
            {
                var items = ingameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems;
                if (items != null)
                    result.AddRange(items.Where(i => i?.Item != null && IsWaystone(i.Item)));
            }
            return result;
        }

        private (int stashIndex, List<NormalInventoryItem>) GetStashItems()
        {
            var result = new List<NormalInventoryItem>();
            var stashIndex = -1;
            if (ingameState.IngameUi.StashElement.IsVisible && ingameState.IngameUi.StashElement.VisibleStash != null)
            {
                stashIndex = ingameState.IngameUi.StashElement.IndexVisibleStash;
                var items = ingameState.IngameUi.StashElement.VisibleStash.VisibleInventoryItems;
                if (items != null)
                    result.AddRange(items.Where(i => i?.Item != null && IsWaystone(i.Item)));
            }
            return (stashIndex, result);
        }

        private List<NormalInventoryItem> GetPurchaseItems()
        {
            var result = new List<NormalInventoryItem>();
            try
            {
                var pw = ingameState.IngameUi.PurchaseWindow;
                if (pw?.IsVisible == true)
                {
                    var items = pw.TabContainer?.VisibleStash?.VisibleInventoryItems;
                    if (items != null)
                        result.AddRange(items.Where(i => i?.Item != null && IsWaystone(i.Item)));
                }

                var pwh = ingameState.IngameUi.PurchaseWindowHideout;
                if (pwh?.IsVisible == true)
                {
                    var items = pwh.TabContainer?.VisibleStash?.VisibleInventoryItems;
                    if (items != null)
                        result.AddRange(items.Where(i => i?.Item != null && IsWaystone(i.Item)));
                }
            }
            catch { }
            return result;
        }

        // ── Tooltip ─────────────────────────────────────────────────────────
        private void RenderItem(NormalInventoryItem item)
        {
            var entity = item?.Item;
            if (!IsWaystone(entity)) return;

            var rarity = entity.GetComponent<Mods>()?.ItemRarity;
            if (rarity == null || rarity == ItemRarity.Normal) return; // normal waystones have no mods

            var details = new ItemDetails(entity);

            if (!Settings.AlwaysShowTooltip && details.ActiveWarnings.Count == 0) return;

            var cursor = MouseLite.GetCursorPositionVector();
            var boxOrigin = new nuVector2(cursor.X + Settings.TooltipOffsetX, cursor.Y + Settings.TooltipOffsetY);

            var opened = true;
            ImGui.PushStyleColor(ImGuiCol.WindowBg, 0xFF3F3F3F);
            if (ImGui.Begin($"##wn_{entity.Address}", ref opened,
                    ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoSavedSettings |
                    ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoNavInputs))
            {
                ImGui.BeginGroup();
                ImGui.TextColored(details.ItemColor, details.MapName);

                // Quantity / Rarity / Pack Size
                var qCol = new nuVector4(1f, 1f, 1f, 1f);
                if (Settings.ColorQuantityPercent)
                    qCol = details.Quantity < Settings.ColorQuantity
                        ? new nuVector4(1f, 0.4f, 0.4f, 1f)
                        : new nuVector4(0.4f, 1f, 0.4f, 1f);

                if (Settings.ShowQuantityPercent && details.Quantity != 0)
                {
                    ImGui.TextColored(qCol, $"{details.Quantity}%% Quant");
                    if (Settings.ShowRarityPercent && details.Rarity != 0) { ImGui.SameLine(); ImGui.TextColored(new nuVector4(1f, 1f, 1f, 1f), $"{details.Rarity}%% Rarity"); }
                    if (Settings.ShowPackSizePercent && details.PackSize != 0) { ImGui.SameLine(); ImGui.TextColored(new nuVector4(1f, 1f, 1f, 1f), $"{details.PackSize}%% Pack"); }
                }
                else
                {
                    if (Settings.ShowRarityPercent && details.Rarity != 0) ImGui.TextColored(new nuVector4(1f, 1f, 1f, 1f), $"{details.Rarity}%% Rarity");
                    if (Settings.ShowPackSizePercent && details.PackSize != 0) ImGui.TextColored(new nuVector4(1f, 1f, 1f, 1f), $"{details.PackSize}%% Pack Size");
                }

                if (Settings.HorizontalLines && details.ActiveWarnings.Count > 0 && (Settings.ShowModCount || Settings.ShowModWarnings))
                    ImGui.Separator();

                if (Settings.ShowModCount && details.ModCount != 0)
                    ImGui.TextColored(new nuVector4(1f, 1f, 1f, 1f), $"{details.ModCount} Mods");

                if (Settings.ShowModWarnings)
                    foreach (var w in details.ActiveWarnings.OrderBy(x => x.Color.ToString()).ToList())
                        ImGui.TextColored(w.Color, w.Text);

                ImGui.EndGroup();

                var size = ImGui.GetWindowSize();
                if (boxOrigin.X + size.X > windowArea.Width)
                    ImGui.SetWindowPos(new nuVector2(boxOrigin.X - (boxOrigin.X + size.X - windowArea.Width) - 4, boxOrigin.Y + 24), ImGuiCond.Always);
                else
                    ImGui.SetWindowPos(boxOrigin, ImGuiCond.Always);
            }
            ImGui.End();
            ImGui.PopStyleColor();
        }

        // ── Borders ───────────────────────────────────────────────────────────
        private List<string> _cachedWarnTypes = new();                 // enabled, no tier
        private List<(string token, int lvl)> _cachedBrickLevels = new();
        private List<(string token, int lvl)> _cachedGoodLevels = new();
        private int _lastEnabledCount = -1, _lastBrickSig = -1, _lastGoodSig = -1;

        private void RefreshBorderCache()
        {
            var en = LiveSettings?.EnabledMods;
            var br = LiveSettings?.BrickLevels;
            var gd = LiveSettings?.GoodLevels;
            int ec = en?.Count ?? 0;
            int brickSig = (br?.Count ?? 0) * 31 + (br?.Sum(kv => kv.Value) ?? 0);
            int goodSig  = (gd?.Count ?? 0) * 31 + (gd?.Sum(kv => kv.Value) ?? 0);
            if (ec == _lastEnabledCount && brickSig == _lastBrickSig && goodSig == _lastGoodSig) return;

            var enabled = en?.Where(kv => kv.Value).Select(kv => kv.Key).ToHashSet() ?? new();
            _cachedBrickLevels = br?.Where(kv => kv.Value > 0 && enabled.Contains(kv.Key))
                                    .Select(kv => (kv.Key, kv.Value)).ToList() ?? new();
            _cachedGoodLevels  = gd?.Where(kv => kv.Value > 0 && enabled.Contains(kv.Key))
                                    .Select(kv => (kv.Key, kv.Value)).ToList() ?? new();
            var tiered = _cachedBrickLevels.Select(x => x.token)
                         .Concat(_cachedGoodLevels.Select(x => x.token)).ToHashSet();
            _cachedWarnTypes = enabled.Where(t => !tiered.Contains(t)).ToList();
            _lastEnabledCount = ec; _lastBrickSig = brickSig; _lastGoodSig = goodSig;
        }

        // TEMP diagnostic: prints a hovered waystone's mods + stat ids to the ExileCore Debug log (once per item).
        private long _lastDbgAddr;
        private void LogWaystoneDebug(Entity entity)
        {
            try
            {
                if (!IsWaystone(entity)) return;
                if (entity.Address == _lastDbgAddr) return;
                _lastDbgAddr = entity.Address;

                var mods = entity.GetComponent<Mods>();
                if (mods?.ItemMods == null) return;
                DebugWindow.LogMsg("[Waystone] ===== hovered waystone mods =====");
                foreach (var mod in mods.ItemMods)
                {
                    var stats = "";
                    try
                    {
                        var names = mod.ModRecord?.StatNames;
                        if (names != null)
                            stats = string.Join(", ", names.Where(s => s?.Key != null).Select(s => s.Key));
                    }
                    catch { }
                    var values = mod.Values != null ? string.Join(",", mod.Values) : "";
                    DebugWindow.LogMsg($"[Waystone] {mod.RawName} | stats: {stats} | val: {values}");
                }
            }
            catch (Exception ex) { DebugWindow.LogError($"[Waystone dbg] {ex.Message}"); }
        }

        // Matches a token against the mod's underlying stat names (e.g. "map_player_maximum_resists_-%").
        public static bool ModHasStat(ItemMod mod, string statToken)
        {
            try
            {
                var names = mod?.ModRecord?.StatNames;
                if (names == null) return false;
                foreach (var s in names)
                {
                    var key = s?.Key;
                    if (key != null && key.Contains(statToken, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch { }
            return false;
        }

        // Returns the rolled value of the first stat matching the token, or 0.
        public static int ModStatValue(ItemMod mod, string statToken)
        {
            try
            {
                var names = mod?.ModRecord?.StatNames;
                if (names == null || mod.Values == null) return 0;
                int i = 0;
                foreach (var s in names)
                {
                    if (s?.Key != null && s.Key.Contains(statToken, StringComparison.OrdinalIgnoreCase))
                        return i < mod.Values.Count ? mod.Values[i] : 0;
                    i++;
                }
            }
            catch { }
            return 0;
        }

        private void DrawWaystoneBorders(NormalInventoryItem item)
        {
            if (!IsWaystone(item?.Item)) return;
            RefreshBorderCache();

            var rect = item.GetClientRect();
            var dw = (int)(rect.Width * (Settings.BorderDeflation / 100.0));
            var dh = (int)(rect.Height * (Settings.BorderDeflation / 100.0));
            var r = new RectangleF(rect.X + dw, rect.Y + dh, rect.Width - 2 * dw, rect.Height - 2 * dh);

            var mods = item.Item.GetComponent<Mods>()?.ItemMods;
            if (mods == null) return;

            // Highest tier present wins (brick4 > brick1, etc.); brick takes priority over good.
            int maxBrick = 0, maxGood = 0;
            bool warn = false;
            foreach (var m in mods)
            {
                foreach (var (token, lvl) in _cachedBrickLevels)
                    if (lvl > maxBrick && ModHasStat(m, token)) maxBrick = lvl;
                foreach (var (token, lvl) in _cachedGoodLevels)
                    if (lvl > maxGood && ModHasStat(m, token)) maxGood = lvl;
                if (!warn)
                    foreach (var t in _cachedWarnTypes)
                        if (ModHasStat(m, t)) { warn = true; break; }
            }

            nuVector4? modColor = null;
            if (maxBrick > 0 && Settings.BoxForMapBadWarnings)      modColor = Settings.BrickColor(maxBrick);
            else if (maxGood > 0 && Settings.BoxForMapGoodMods)     modColor = Settings.GoodColor(maxGood);
            else if (warn && Settings.BoxForMapWarnings)            modColor = Settings.MapBorderWarnings;

            if (!modColor.HasValue) return;

            var thickness = Settings.BorderThicknessMap;
            if (Settings.MapBorderStyle) Graphics.DrawBox(r, modColor.Value.ToSdColor());
            else Graphics.DrawFrame(r, modColor.Value.ToSdColor(), thickness);
        }

        // ── Render ─────────────────────────────────────────────────────────────
        public override void Render()
        {
            var uiHover = ingameState.UIHover;
            if (uiHover != null && uiHover.Address != 0)
            {
                var hoverItem = uiHover.AsObject<NormalInventoryItem>();
                var hovEntity = hoverItem?.Item;
                if (hovEntity != null && hovEntity.Address != 0 && hovEntity.IsValid && hovEntity.Path != null)
                {
                    if (Settings.DebugLogMods) LogWaystoneDebug(hovEntity);
                    RenderItem(hoverItem);
                }
            }

            if (ingameState.IngameUi.InventoryPanel.IsVisible)
                foreach (var item in _inventoryItems.Value)
                    DrawWaystoneBorders(item);

            if (ingameState.IngameUi.StashElement.IsVisible
                && ingameState.IngameUi.StashElement.VisibleStash != null
                && ingameState.IngameUi.StashElement.IndexVisibleStash == _stashItems.Value.stashIndex)
                foreach (var item in _stashItems.Value.Item2)
                    DrawWaystoneBorders(item);

            if (Settings.ShowBorderInPurchase)
                foreach (var item in _purchaseItems.Value)
                    DrawWaystoneBorders(item);
        }
    }
}

using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.Elements.InventoryElements;
using ExileCore2.Shared.Nodes;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using nuVector2 = System.Numerics.Vector2;
using nuVector4 = System.Numerics.Vector4;

namespace WaystoneNotify
{
    public partial class WaystoneNotify : BaseSettingsPlugin<WaystoneNotifySettings>
    {
        private string _modSearch = "";
        private string _tabletSearch = "";
        private bool _confirmDelete = false;
        private string _deleteTarget = "";
        private string _newProfName = "";
        private readonly Dictionary<string, bool> _groupOpen = new();        // waystone tab
        private readonly Dictionary<string, bool> _tabletGroupOpen = new();  // tablet tab
        public static List<string> hoverMods = new();

        private static void HelpMarker(string desc)
        {
            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            if (!ImGui.IsItemHovered()) return;
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
            ImGui.TextUnformatted(desc);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }

        private static void Toggle(string label, ToggleNode node)
        {
            var v = node.Value;
            ImGui.Checkbox(label, ref v);
            node.Value = v;
        }

        private static int IntSlider(string label, RangeNode<int> node)
        {
            var v = node.Value;
            ImGui.SliderInt(label, ref v, node.Min, node.Max);
            return v;
        }

        private static nuVector4 ColorEdit(string label, nuVector4 value)
        {
            ImGui.ColorEdit4(label, ref value, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.AlphaBar);
            return value;
        }

        private static void DebugHover()
        {
            var uiHover = ingameState.UIHover;
            if (uiHover == null || !uiHover.IsVisible) return;
            var icon = uiHover.AsObject<NormalInventoryItem>();
            var entity = icon?.Item;
            if (entity == null || entity.Address == 0 || !entity.IsValid) return;
            var mods = entity.GetComponent<Mods>();
            if (mods?.ItemMods == null || mods.ItemMods.Count == 0) { hoverMods.Clear(); return; }
            hoverMods.Clear();
            foreach (var mod in mods.ItemMods)
            {
                var values = mod.Values != null ? string.Join(", ", mod.Values) : "";
                var stats = "";
                try
                {
                    var names = mod.ModRecord?.StatNames;
                    if (names != null)
                        stats = string.Join(", ", names.Where(s => s?.Key != null).Select(s => s.Key));
                }
                catch { }
                hoverMods.Add($"{mod.RawName}  [stats: {stats}]  ({values})");
            }
        }

        // Renders 4 tiny tier buttons (1-4); returns the new level (0 = none). Clicking the active tier clears it.
        private static int LevelButtons(string id, int current, System.Func<int, nuVector4> colorOf)
        {
            int result = current;
            for (int n = 1; n <= 4; n++)
            {
                var col = colorOf(n);
                bool active = current == n;
                var bg = active ? col : new nuVector4(col.X * 0.30f, col.Y * 0.30f, col.Z * 0.30f, 1f);
                ImGui.PushStyleColor(ImGuiCol.Button,        bg);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, col);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive,  col);
                ImGui.PushStyleColor(ImGuiCol.Text,          new nuVector4(0f, 0f, 0f, 1f));
                if (ImGui.SmallButton($"{n}##{id}{n}")) result = active ? 0 : n;
                ImGui.PopStyleColor(4);
                if (n < 4) ImGui.SameLine();
            }
            return result;
        }

        public override void DrawSettings()
        {
            int warnCount  = Settings.EnabledMods.Count(kv => kv.Value);
            int brickCount = Settings.BrickLevels.Count(kv => kv.Value > 0);
            int goodCount  = Settings.GoodLevels.Count(kv => kv.Value > 0);
            int tWarn = Settings.TabletEnabledMods.Count(kv => kv.Value);
            ImGui.TextDisabled($"Waystone: {warnCount} on  {brickCount} brick  {goodCount} good   |   Tablet: {tWarn} on   |   {Settings.ActiveProfile.Value}");
            ImGui.Separator();

            if (!ImGui.BeginTabBar("##waystone_tabs")) return;
            if (ImGui.BeginTabItem("Waystone Mods")) { TabMods(); ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("Tablet Mods"))   { TabTabletMods(); ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("Profiles")) { TabProfiles(); ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("Border"))   { TabBorder();   ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("Display"))  { TabDisplay();  ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("Debug"))    { TabDebug();    ImGui.EndTabItem(); }
            ImGui.EndTabBar();
        }

        private void TabMods() =>
            DrawModList("ws", _modEntries, "waystone_mods_data.json",
                ref _modSearch, _groupOpen,
                Settings.EnabledMods, Settings.BrickLevels, Settings.GoodLevels);

        private void TabTabletMods() =>
            DrawModList("tab", _tabletEntries, "tablet_mods_data.json",
                ref _tabletSearch, _tabletGroupOpen,
                Settings.TabletEnabledMods, Settings.TabletBrickLevels, Settings.TabletGoodLevels);

        // Shared mod-picker UI; the only differences between waystone/tablet tabs are which
        // dataset + dictionaries (+ ImGui id namespace) get passed in.
        private void DrawModList(string idns, List<MapModEntry> entries, string fileName,
            ref string search, Dictionary<string, bool> groupOpen,
            Dictionary<string, bool> enabledMods,
            Dictionary<string, int> brickLevels, Dictionary<string, int> goodLevels)
        {
            if (entries == null || entries.Count == 0)
            {
                ImGui.TextColored(new nuVector4(1f, 0.4f, 0.1f, 1f), $"{fileName} not loaded (empty or missing).");
                ImGui.TextDisabled("Use the Debug tab to dump hovered item mods, then add their tokens to the data file.");
                return;
            }

            ImGui.Spacing();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 60f);
            ImGui.InputTextWithHint($"##ms_{idns}", "Search mods...", ref search, 128);
            ImGui.SameLine();
            if (ImGui.Button($"Clear##{idns}")) search = "";
            ImGui.Spacing();

            var q = search.Trim().ToLowerInvariant();
            var grouped = entries
                .Where(e => string.IsNullOrEmpty(q)
                    || e.Name.ToLowerInvariant().Contains(q)
                    || e.Effect.ToLowerInvariant().Contains(q)
                    || e.ModType.ToLowerInvariant().Contains(q))
                .GroupBy(e => ModDataLoader.GetGroup(e))
                .OrderBy(g => g.Key == "Other" ? "zzz" : g.Key);

            foreach (var group in grouped)
            {
                int activeInGroup = group.Count(e => enabledMods.TryGetValue(e.ModType, out var v) && v);
                string headerLabel = activeInGroup > 0
                    ? $"{group.Key}  ({activeInGroup} active)##grp_{idns}_{group.Key}"
                    : $"{group.Key}##grp_{idns}_{group.Key}";

                if (!groupOpen.ContainsKey(group.Key)) groupOpen[group.Key] = false;
                bool open = groupOpen[group.Key];

                ImGui.PushStyleColor(ImGuiCol.Button,        new nuVector4(0.15f, 0.15f, 0.18f, 1f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new nuVector4(0.20f, 0.20f, 0.25f, 1f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new nuVector4(0.12f, 0.12f, 0.15f, 1f));
                ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new nuVector2(0f, 0.5f));
                if (ImGui.Button((open ? "v " : "> ") + headerLabel, new nuVector2(ImGui.GetContentRegionAvail().X, 0)))
                    groupOpen[group.Key] = !open;
                ImGui.PopStyleVar();
                ImGui.PopStyleColor(3);
                if (!open) continue;

                foreach (var entry in group)
                {
                    bool isEnabled = enabledMods.TryGetValue(entry.ModType, out var en) && en;
                    int brickLvl = isEnabled && brickLevels.TryGetValue(entry.ModType, out var bl) ? bl : 0;
                    int goodLvl  = isEnabled && goodLevels.TryGetValue(entry.ModType, out var gl) ? gl : 0;
                    bool isBricked = brickLvl > 0;
                    bool isGood    = goodLvl > 0;

                    if (isBricked)      { var c = Settings.BrickColor(brickLvl); ImGui.PushStyleColor(ImGuiCol.Header, new nuVector4(c.X * 0.5f, c.Y * 0.5f, c.Z * 0.5f, 0.5f)); }
                    else if (isGood)    { var c = Settings.GoodColor(goodLvl);   ImGui.PushStyleColor(ImGuiCol.Header, new nuVector4(c.X * 0.5f, c.Y * 0.5f, c.Z * 0.5f, 0.5f)); }
                    else if (isEnabled) ImGui.PushStyleColor(ImGuiCol.Header, new nuVector4(0.45f, 0.18f, 0.06f, 0.35f));

                    float lineH = ImGui.GetTextLineHeight();
                    float rowH  = lineH * 2f + 8f;
                    float availW = ImGui.GetContentRegionAvail().X;
                    float selectW = isEnabled ? Math.Max(60f, availW - 250f) : availW;

                    ImGui.SetNextItemAllowOverlap();
                    bool rowClicked = ImGui.Selectable($"##sel_{idns}_{entry.ModType}", isEnabled,
                        ImGuiSelectableFlags.DontClosePopups, new nuVector2(selectW, rowH));

                    if (isEnabled) ImGui.PopStyleColor();

                    if (rowClicked)
                    {
                        if (isEnabled)
                        {
                            enabledMods[entry.ModType] = false;
                            brickLevels.Remove(entry.ModType);
                            goodLevels.Remove(entry.ModType);
                        }
                        else enabledMods[entry.ModType] = true;
                    }

                    var selMin = ImGui.GetItemRectMin();
                    var dl = ImGui.GetWindowDrawList();
                    var nameCol = isBricked ? Settings.BrickColor(brickLvl)
                        : isGood ? Settings.GoodColor(goodLvl)
                        : isEnabled ? new nuVector4(0.90f, 0.90f, 0.90f, 1f)
                        : new nuVector4(0.65f, 0.65f, 0.68f, 1f);
                    dl.AddText(selMin + new nuVector2(4f, 3f), ImGui.GetColorU32(nameCol), entry.Name);
                    dl.AddText(selMin + new nuVector2(4f, 3f + lineH),
                        ImGui.GetColorU32(new nuVector4(0.32f, 0.32f, 0.36f, 1f)),
                        entry.Effect.Length > 100 ? entry.Effect[..100] + "..." : entry.Effect);

                    if (isEnabled)
                    {
                        ImGui.SameLine();
                        ImGui.AlignTextToFramePadding();
                        ImGui.TextColored(new nuVector4(0.8f, 0.5f, 0.5f, 1f), "B"); ImGui.SameLine();
                        int nb = LevelButtons($"brk_{idns}_" + entry.ModType, brickLvl, Settings.BrickColor);
                        if (nb != brickLvl)
                        {
                            if (nb > 0) { brickLevels[entry.ModType] = nb; goodLevels.Remove(entry.ModType); }
                            else brickLevels.Remove(entry.ModType);
                        }
                        ImGui.SameLine();
                        ImGui.TextColored(new nuVector4(0.5f, 0.8f, 0.5f, 1f), "G"); ImGui.SameLine();
                        int ng = LevelButtons($"good_{idns}_" + entry.ModType, goodLvl, Settings.GoodColor);
                        if (ng != goodLvl)
                        {
                            if (ng > 0) { goodLevels[entry.ModType] = ng; brickLevels.Remove(entry.ModType); }
                            else goodLevels.Remove(entry.ModType);
                        }
                    }
                }
                ImGui.Spacing();
            }
        }

        private void TabProfiles()
        {
            var pm = _profileManager;
            if (pm == null) { ImGui.TextDisabled("Profile manager not initialised."); return; }

            ImGui.Spacing();
            ImGui.TextDisabled("Active: " + Settings.ActiveProfile.Value);
            ImGui.Separator();
            ImGui.Spacing();

            foreach (var name in pm.ProfileNames)
            {
                bool isActive = name == Settings.ActiveProfile.Value;
                if (isActive)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new nuVector4(1f, 0.65f, 0.2f, 1f));
                    ImGui.TextUnformatted(">> " + name);
                    ImGui.PopStyleColor();
                }
                else ImGui.TextUnformatted("   " + name);

                ImGui.SameLine(200f);
                if (!isActive)
                {
                    if (ImGui.SmallButton($"Load##{name}"))
                    {
                        pm.LoadProfile(name, Settings);
                        Settings.ActiveProfile.Value = name;
                    }
                    ImGui.SameLine();
                }
                if (ImGui.SmallButton($"Save##{name}")) pm.SaveProfile(name, Settings);
                HelpMarker("Overwrites this profile with current mod selections");

                if (name != "Default")
                {
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Text, new nuVector4(0.75f, 0.2f, 0.2f, 1f));
                    if (ImGui.SmallButton($"Del##{name}")) { _confirmDelete = true; _deleteTarget = name; }
                    ImGui.PopStyleColor();
                }
                ImGui.Separator();
            }

            if (_confirmDelete)
            {
                ImGui.Spacing();
                ImGui.TextColored(new nuVector4(1f, 0.3f, 0.3f, 1f), $"Delete \"{_deleteTarget}\"?");
                ImGui.SameLine();
                if (ImGui.SmallButton("Yes"))
                {
                    if (Settings.ActiveProfile.Value == _deleteTarget)
                    { pm.LoadProfile("Default", Settings); Settings.ActiveProfile.Value = "Default"; }
                    pm.DeleteProfile(_deleteTarget);
                    _confirmDelete = false; _deleteTarget = "";
                }
                ImGui.SameLine();
                if (ImGui.SmallButton("Cancel##cxd")) { _confirmDelete = false; _deleteTarget = ""; }
                ImGui.Spacing();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.TextDisabled("New profile");
            ImGui.SetNextItemWidth(180f);
            ImGui.InputTextWithHint("##np", "Profile name...", ref _newProfName, 64);
            ImGui.SameLine();
            bool canCreate = !string.IsNullOrWhiteSpace(_newProfName) && !pm.Profiles.ContainsKey(_newProfName);
            if (!canCreate) ImGui.BeginDisabled();
            if (ImGui.SmallButton("Create"))
            {
                pm.SaveProfile(_newProfName, Settings);
                Settings.ActiveProfile.Value = _newProfName;
                _newProfName = "";
            }
            if (!canCreate) ImGui.EndDisabled();
        }

        private void TabBorder()
        {
            ImGui.Spacing();
            ImGui.TextDisabled("Enable Borders");
            ImGui.Separator();
            Toggle("Warning mods border", Settings.BoxForMapWarnings);
            Toggle("Bricked mods border", Settings.BoxForMapBadWarnings);
            Toggle("Good mods border",    Settings.BoxForMapGoodMods);
            Toggle("Use Box style (off = Frame)", Settings.MapBorderStyle);

            ImGui.Spacing();
            ImGui.TextDisabled("Warning color (enabled, no tier)");
            ImGui.Separator();
            Settings.MapBorderWarnings = ColorEdit("Warning color##wc", Settings.MapBorderWarnings);

            ImGui.Spacing();
            ImGui.TextDisabled("Brick tiers (1 = lowest .. 4 = highest)");
            ImGui.Separator();
            Settings.Brick1 = ColorEdit("Brick 1##b1", Settings.Brick1);
            Settings.Brick2 = ColorEdit("Brick 2##b2", Settings.Brick2);
            Settings.Brick3 = ColorEdit("Brick 3##b3", Settings.Brick3);
            Settings.Brick4 = ColorEdit("Brick 4##b4", Settings.Brick4);

            ImGui.Spacing();
            ImGui.TextDisabled("Good tiers (1 = lowest .. 4 = highest)");
            ImGui.Separator();
            Settings.Good1 = ColorEdit("Good 1##g1", Settings.Good1);
            Settings.Good2 = ColorEdit("Good 2##g2", Settings.Good2);
            Settings.Good3 = ColorEdit("Good 3##g3", Settings.Good3);
            Settings.Good4 = ColorEdit("Good 4##g4", Settings.Good4);

            ImGui.Spacing();
            ImGui.TextDisabled("Full map highlight (overrides everything)");
            ImGui.Separator();
            Toggle("Highlight full maps", Settings.HighlightFullMap);
            HelpMarker("A waystone with at least this many mods is drawn in the color below, regardless of its mods.");
            Settings.FullMapModCount.Value = IntSlider("Min mods##fmc", Settings.FullMapModCount);
            Settings.FullMapColor = ColorEdit("Full map color##fmcol", Settings.FullMapColor);

            ImGui.Spacing();
            ImGui.TextDisabled("Size");
            ImGui.Separator();
            Settings.BorderDeflation.Value    = IntSlider("Deflation##bd", Settings.BorderDeflation);
            Settings.BorderThicknessMap.Value = IntSlider("Thickness##bt", Settings.BorderThicknessMap);
        }

        private void TabDisplay()
        {
            ImGui.Spacing();
            ImGui.TextDisabled("Tooltip");
            ImGui.Separator();
            Toggle("Always show tooltip", Settings.AlwaysShowTooltip);
            HelpMarker("Shows tooltip even when there are no warnings");
            Toggle("Show mod warnings", Settings.ShowModWarnings);
            Toggle("Show mod count",    Settings.ShowModCount);
            Toggle("Show quantity %",   Settings.ShowQuantityPercent);
            Toggle("Show rarity %",     Settings.ShowRarityPercent);
            Toggle("Show pack size %",  Settings.ShowPackSizePercent);
            Toggle("Horizontal separator lines", Settings.HorizontalLines);

            ImGui.Spacing();
            ImGui.TextDisabled("Windows");
            ImGui.Separator();
            Toggle("Show border in purchase window", Settings.ShowBorderInPurchase);

            ImGui.Spacing();
            ImGui.TextDisabled("Quantity Color");
            ImGui.Separator();
            ImGui.TextDisabled("Turns red below threshold, green above.");
            Toggle("Enable quantity color", Settings.ColorQuantityPercent);
            Settings.ColorQuantity.Value = IntSlider("Threshold##cq", Settings.ColorQuantity);

            ImGui.Spacing();
            ImGui.TextDisabled("Cache Intervals (reload plugin after changing)");
            ImGui.Separator();
            Settings.InventoryCacheInterval.Value = IntSlider("Inventory (ms)##ic", Settings.InventoryCacheInterval);
            Settings.StashCacheInterval.Value     = IntSlider("Stash (ms)##sc",     Settings.StashCacheInterval);

            ImGui.Spacing();
            ImGui.TextDisabled("Tooltip Position");
            ImGui.Separator();
            Settings.TooltipOffsetX.Value = IntSlider("Horizontal offset##tx", Settings.TooltipOffsetX);
            Settings.TooltipOffsetY.Value = IntSlider("Vertical offset##ty",   Settings.TooltipOffsetY);
        }

        private void TabDebug()
        {
            ImGui.Spacing();
            ImGui.TextDisabled("Hover a Waystone to see its raw mod names.");
            ImGui.TextDisabled("Use these to fix the \"Mod type\" tokens in waystone_mods_data.json.");
            ImGui.Separator();
            ImGui.Spacing();
            Toggle("Print mod names to ExileCore Debug log", Settings.DebugLogMods);
            HelpMarker("Off by default. Turn on to dump hovered waystone mods (RawName + stat ids) to the Debug log.");
            ImGui.Spacing();
            DebugHover();
            if (hoverMods.Count > 0)
            {
                if (ImGui.Button("Copy All"))
                    ImGui.SetClipboardText(string.Join(Environment.NewLine, hoverMods));
                ImGui.Spacing();
                foreach (var mod in hoverMods)
                {
                    ImGui.TextColored(new nuVector4(1f, 0.45f, 0.1f, 1f), mod);
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Text, new nuVector4(0.4f, 0.4f, 0.45f, 1f));
                    if (ImGui.SmallButton($"Copy##{mod}"))
                        ImGui.SetClipboardText(mod.Split(' ')[0]);
                    ImGui.PopStyleColor();
                }
            }
            else ImGui.TextDisabled("(no waystone hovered)");
        }
    }
}

using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using nuVector4 = System.Numerics.Vector4;

namespace WaystoneNotify
{
    public partial class WaystoneNotify : BaseSettingsPlugin<WaystoneNotifySettings>
    {
        public static nuVector4 GetRarityColor(ItemRarity rarity) => rarity switch
        {
            ItemRarity.Rare   => new nuVector4(0.99f, 0.99f, 0.46f, 1f),
            ItemRarity.Magic  => new nuVector4(0.68f, 0.68f, 1f, 1f),
            ItemRarity.Unique => new nuVector4(1f, 0.50f, 0.10f, 1f),
            _                 => new nuVector4(1f, 1f, 1f, 1f),
        };

        public class StyledText
        {
            public string Text { get; set; }
            public nuVector4 Color { get; set; }
            public bool Bricking { get; set; }
        }

        public class ItemDetails
        {
            public Entity Entity { get; }
            public ItemKind Kind { get; }
            public List<StyledText> ActiveWarnings { get; } = new();
            public nuVector4 ItemColor { get; set; }
            public string MapName { get; set; }
            public int Tier { get; set; }
            public int ModCount { get; set; }
            public int Quantity { get; set; }
            public int Rarity { get; set; }
            public int PackSize { get; set; }
            public bool Bricked { get; set; }

            public ItemDetails(Entity entity, ItemKind kind)
            {
                Entity = entity;
                Kind = kind;
                Update();
            }

            public void Update()
            {
                ActiveWarnings.Clear();

                var baseItem = gameController.Files.BaseItemTypes.Translate(Entity.Path);
                var itemName = baseItem?.BaseName ?? (Kind == ItemKind.Tablet ? "Tablet" : "Waystone");

                // Only waystones carry a Map/Tier; tablets have no tier.
                Tier = Kind == ItemKind.Waystone ? (Entity.GetComponent<Map>()?.Tier ?? -1) : -1;

                var mods = Entity.GetComponent<Mods>();
                if (mods == null) { MapName = itemName; return; }

                ItemColor = GetRarityColor(mods.ItemRarity);

                var rareName = mods.UniqueName;
                MapName = !string.IsNullOrEmpty(rareName)
                    ? (Tier > 0 ? $"[T{Tier}] {rareName}" : rareName)
                    : (Tier > 0 ? $"[T{Tier}] {itemName}" : itemName);

                var itemMods = mods.ItemMods;
                if (itemMods == null) return;
                ModCount = itemMods.Count;

                // Pick the dictionaries + dataset that match this item kind.
                var (enabledMods, brickLevels, goodLevels, customNames, entries) = ConfigFor(Kind);

                foreach (var mod in itemMods)
                {
                    // Reward numbers read by exact stat id (shared tokens between waystones and tablets)
                    if (Quantity == 0) Quantity = ModStatValue(mod, "map_item_drop_quantity_+%");
                    if (Rarity == 0)   Rarity   = ModStatValue(mod, "map_item_drop_rarity_+%");
                    if (PackSize == 0) PackSize  = ModStatValue(mod, "map_pack_size_+%");

                    if (enabledMods == null || entries == null) continue;

                    foreach (var entry in entries)
                    {
                        if (!enabledMods.TryGetValue(entry.ModType, out var enabled) || !enabled)
                            continue;
                        if (!ModHasStat(mod, entry.ModType))
                            continue;

                        var display = customNames != null
                                      && customNames.TryGetValue(entry.ModType, out var custom)
                                      && !string.IsNullOrWhiteSpace(custom)
                            ? custom : entry.Name;

                        if (ActiveWarnings.Any(w => w.Text == display)) continue;

                        int brickLvl = brickLevels != null && brickLevels.TryGetValue(entry.ModType, out var br) ? br : 0;
                        int goodLvl  = brickLvl == 0 && goodLevels != null
                                       && goodLevels.TryGetValue(entry.ModType, out var gd) ? gd : 0;
                        bool isBricked = brickLvl > 0;
                        if (isBricked) Bricked = true;

                        var s = LiveSettings;
                        var color = isBricked ? s.BrickColor(brickLvl)
                                  : goodLvl > 0 ? s.GoodColor(goodLvl)
                                  : s.MapBorderWarnings;

                        ActiveWarnings.Add(new StyledText { Text = display, Color = color, Bricking = isBricked });
                    }
                }
            }

        }
    }
}

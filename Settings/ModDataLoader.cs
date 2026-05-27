using ExileCore2;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace WaystoneNotify
{
    // Mirrors the structure of waystone_mods_data.json
    public class MapModEntry
    {
        [JsonProperty("Mod type")] public string ModType { get; set; }
        [JsonProperty("Name")]     public string Name    { get; set; }
        [JsonProperty("Effect")]   public string Effect  { get; set; }
        [JsonProperty("Group")]    public string Group   { get; set; }
    }

    public static class ModDataLoader
    {
        // Groups by mod-type prefix. First match wins.
        // ModType values are the PoE1 internal mod ids (reused by PoE2); matched as substrings of mod RawName.
        private static readonly (string prefix, string group)[] Groups =
        {
            // Order matters — first prefix match wins.
            ("map_monsters_reflect",                    "Reflect & Regen"),
            ("map_ground_effect",                       "Ground Effects"),
            ("map_monsters_gain_percentage_physical_as","Monster Damage Types"),
            ("map_player_",                             "Player Debuffs"),

            // Rewards
            ("map_item_drop",  "Reward"),
            ("map_gold",       "Reward"),
            ("map_experience", "Reward"),
            ("map_pack_size",  "Reward"),
            ("map_additional", "Reward"),
            ("map_magic",      "Reward"),
            ("map_rare",       "Reward"),
            ("map_contains",   "Reward"),

            // Catch-all for remaining map_monster(s)_* tokens
            ("map_monster",    "Monster Buffs"),
        };

        // Prefer the explicit "Group" field from the data file; fall back to prefix matching.
        public static string GetGroup(MapModEntry entry)
        {
            if (entry != null && !string.IsNullOrWhiteSpace(entry.Group))
                return entry.Group;
            return GetGroup(entry?.ModType ?? "");
        }

        public static string GetGroup(string modType)
        {
            foreach (var (prefix, group) in Groups)
                if (modType.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    return group;
            return "Other";
        }

        public static List<MapModEntry> Load(string directoryFullName)
        {
            try
            {
                var path = Path.Combine(directoryFullName, "data", "waystone_mods_data.json");
                if (!File.Exists(path))
                {
                    DebugWindow.LogError($"[Waystone] waystone_mods_data.json not found at {path}");
                    return new List<MapModEntry>();
                }
                var result = JsonConvert.DeserializeObject<List<MapModEntry>>(File.ReadAllText(path))
                             ?? new List<MapModEntry>();
                DebugWindow.LogMsg($"[Waystone] Loaded {result.Count} mods from {path}");
                return result;
            }
            catch (System.Exception ex)
            {
                DebugWindow.LogError($"[Waystone] Failed to load waystone_mods_data.json: {ex.Message}");
                return new List<MapModEntry>();
            }
        }
    }
}

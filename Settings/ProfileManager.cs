using ExileCore2;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WaystoneNotify
{
    public class WaystoneProfile
    {
        public string Name                       { get; set; } = "Default";
        public DateTime LastSaved                { get; set; } = DateTime.UtcNow;
        public Dictionary<string, bool> Mods     { get; set; } = new();
        public Dictionary<string, bool> Bricked  { get; set; } = new();
        public Dictionary<string, bool> Good     { get; set; } = new();
        public Dictionary<string, string> CustomNames { get; set; } = new();
    }

    public class ProfileManager
    {
        private readonly string _path;
        public Dictionary<string, WaystoneProfile> Profiles { get; private set; } = new();
        public List<string> ProfileNames => Profiles.Keys.OrderBy(k => k).ToList();

        public ProfileManager(string configDir)
        {
            _path = Path.Combine(configDir, "profiles.json");
            Load();
            if (!Profiles.ContainsKey("Default"))
            {
                Profiles["Default"] = new WaystoneProfile { Name = "Default" };
                Save();
            }
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(_path)) return;
                Profiles = JsonConvert.DeserializeObject<Dictionary<string, WaystoneProfile>>(
                    File.ReadAllText(_path)) ?? new Dictionary<string, WaystoneProfile>();
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"[Waystone] Profile load failed: {ex.Message}");
                Profiles = new Dictionary<string, WaystoneProfile>();
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                File.WriteAllText(_path, JsonConvert.SerializeObject(Profiles, Formatting.Indented));
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"[Waystone] Profile save failed: {ex.Message}");
            }
        }

        public void SaveProfile(string name, WaystoneNotifySettings s)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var p = Profiles.GetValueOrDefault(name) ?? new WaystoneProfile();
            p.Name        = name;
            p.LastSaved   = DateTime.UtcNow;
            p.Mods        = new Dictionary<string, bool>(s.EnabledMods);
            p.Bricked     = new Dictionary<string, bool>(s.BrickedMods);
            p.Good        = new Dictionary<string, bool>(s.GoodMods);
            p.CustomNames = new Dictionary<string, string>(s.CustomModNames);
            Profiles[name] = p;
            Save();
        }

        public bool LoadProfile(string name, WaystoneNotifySettings s)
        {
            if (!Profiles.TryGetValue(name, out var p)) return false;
            s.EnabledMods    = new Dictionary<string, bool>(p.Mods);
            s.BrickedMods    = new Dictionary<string, bool>(p.Bricked ?? new());
            s.GoodMods       = new Dictionary<string, bool>(p.Good    ?? new());
            s.CustomModNames = new Dictionary<string, string>(p.CustomNames ?? new());
            return true;
        }

        public void DeleteProfile(string name)
        {
            if (name == "Default") return;
            Profiles.Remove(name);
            Save();
        }
    }
}

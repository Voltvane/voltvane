using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Voltvane.Services
{
    /// <summary>
    /// Persists which tweaks the user has turned ON in Voltvane to a JSON file in
    /// %AppData%\Voltvane\voltvane_state.json.
    ///
    /// Key behavior:
    /// - On first ever launch the file doesn't exist, so EVERY tweak reads as Off.
    /// - The app trusts THIS file as the source of truth for toggle state - not a
    ///   live system scan. So the UI always reflects what YOU set inside Voltvane.
    /// </summary>
    public class StateStore
    {
        private readonly string _dir;
        private readonly string _file;

        // tweakId -> isEnabled
        private Dictionary<string, bool> _state = new();

        public StateStore()
        {
            _dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Voltvane");
            _file = Path.Combine(_dir, "voltvane_state.json");
            Load();
        }

        private void Load()
        {
            try
            {
                if (File.Exists(_file))
                {
                    string json = File.ReadAllText(_file);
                    _state = JsonSerializer.Deserialize<Dictionary<string, bool>>(json)
                             ?? new Dictionary<string, bool>();
                }
                else
                {
                    // First launch - empty state means everything is Off.
                    _state = new Dictionary<string, bool>();
                }
            }
            catch
            {
                _state = new Dictionary<string, bool>();
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(_dir);
                string json = JsonSerializer.Serialize(_state,
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_file, json);
            }
            catch
            {
                // Non-fatal: if we can't write, state just won't persist this session.
            }
        }

        /// <summary>Returns the saved on/off state for a tweak. Defaults to Off.</summary>
        public bool IsEnabled(string tweakId)
        {
            return _state.TryGetValue(tweakId, out bool v) && v;
        }

        /// <summary>Records a tweak's new state and writes to disk immediately.</summary>
        public void Set(string tweakId, bool enabled)
        {
            _state[tweakId] = enabled;
            Save();
        }

        public string FilePath => _file;
    }
}

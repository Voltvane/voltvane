using System;
using System.Linq;
using System.Management;

namespace Voltvane.Services
{
    public class HardwareInfo
    {
        public string Cpu { get; set; } = "Unknown CPU";
        public string Gpu { get; set; } = "Unknown GPU";
        public string RamText { get; set; } = "Unknown RAM";
        public bool IsLikelyLaptop { get; set; }
        public string OsName { get; set; } = "Windows";
    }

    /// <summary>
    /// Reads basic hardware via WMI. Used to (a) show the user their specs and
    /// (b) auto-guess PC vs Laptop for the startup selector (the user can override).
    /// </summary>
    public static class HardwareDetector
    {
        public static HardwareInfo Detect()
        {
            var info = new HardwareInfo();

            info.Cpu = QuerySingle("SELECT Name FROM Win32_Processor", "Name") ?? info.Cpu;
            info.Gpu = QueryGpu() ?? info.Gpu;
            info.RamText = QueryRam();
            info.IsLikelyLaptop = DetectLaptop();
            info.OsName = QuerySingle("SELECT Caption FROM Win32_OperatingSystem", "Caption") ?? info.OsName;

            return info;
        }

        private static string? QuerySingle(string query, string property)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(query);
                foreach (var obj in searcher.Get())
                {
                    var val = obj[property]?.ToString();
                    if (!string.IsNullOrWhiteSpace(val))
                        return val.Trim();
                }
            }
            catch { }
            return null;
        }

        private static string? QueryGpu()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name, AdapterRAM FROM Win32_VideoController");
                // Prefer a discrete NVIDIA/AMD GPU name if present
                string? best = null;
                foreach (var obj in searcher.Get())
                {
                    var name = obj["Name"]?.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("GeForce", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Radeon", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("RTX", StringComparison.OrdinalIgnoreCase))
                        return name;
                    best ??= name;
                }
                return best;
            }
            catch { return null; }
        }

        private static string QueryRam()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                foreach (var obj in searcher.Get())
                {
                    if (ulong.TryParse(obj["TotalPhysicalMemory"]?.ToString(), out var bytes))
                    {
                        double gb = bytes / 1024.0 / 1024.0 / 1024.0;
                        return $"{Math.Round(gb)} GB RAM";
                    }
                }
            }
            catch { }
            return "Unknown RAM";
        }

        /// <summary>
        /// Heuristic laptop detection. Checks for a battery and chassis type.
        /// Not 100% but good enough to pre-select the right button.
        /// </summary>
        private static bool DetectLaptop()
        {
            // Method 1: does a battery exist?
            try
            {
                using var batt = new ManagementObjectSearcher("SELECT * FROM Win32_Battery");
                if (batt.Get().Count > 0) return true;
            }
            catch { }

            // Method 2: chassis type (8=Portable, 9=Laptop, 10=Notebook, 14=SubNotebook)
            try
            {
                using var chassis = new ManagementObjectSearcher(
                    "SELECT ChassisTypes FROM Win32_SystemEnclosure");
                foreach (var obj in chassis.Get())
                {
                    if (obj["ChassisTypes"] is ushort[] types)
                    {
                        if (types.Any(t => t is 8 or 9 or 10 or 11 or 12 or 14 or 18 or 21))
                            return true;
                    }
                }
            }
            catch { }

            return false;
        }
    }
}

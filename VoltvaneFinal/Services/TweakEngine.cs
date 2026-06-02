using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Win32;
using Voltvane.Models;

namespace Voltvane.Services
{
    /// <summary>
    /// Executes tweaks. Three rules baked in:
    /// 1. A System Restore point is offered/created before the first change in a session.
    /// 2. Every change is reversible.
    /// 3. Registry and command actions are logged so the user can see exactly what ran.
    /// </summary>
    public class TweakEngine
    {
        public event Action<string>? Log;

        private bool _restorePointMadeThisSession = false;

        // ------------------------------------------------------------------
        // SYSTEM RESTORE POINT
        // ------------------------------------------------------------------

        /// <summary>
        /// Creates a System Restore point. You learned the hard way why this matters.
        /// Uses PowerShell's Checkpoint-Computer. Returns true on success.
        /// </summary>
        public async Task<bool> CreateRestorePointAsync(string description = "Voltvane - before tweaks")
        {
            if (_restorePointMadeThisSession)
            {
                Log?.Invoke("Restore point already created this session - skipping.");
                return true;
            }

            Log?.Invoke("Creating a System Restore point (this can take a minute)...");

            try
            {
                // Enable system restore on C: in case it's off, then checkpoint.
                string psCommand =
                    "Enable-ComputerRestore -Drive 'C:\\'; " +
                    "Checkpoint-Computer -Description '" + description.Replace("'", "") +
                    "' -RestorePointType 'MODIFY_SETTINGS'";

                bool ok = await RunPowerShellAsync(psCommand);

                if (ok)
                {
                    _restorePointMadeThisSession = true;
                    Log?.Invoke("Restore point created successfully.");
                }
                else
                {
                    Log?.Invoke("Could not create a restore point automatically. " +
                                "Windows limits how often these can be made (once per ~24h by default). " +
                                "You can proceed, but consider making one manually first.");
                }
                return ok;
            }
            catch (Exception ex)
            {
                Log?.Invoke("Restore point error: " + ex.Message);
                return false;
            }
        }

        // ------------------------------------------------------------------
        // APPLY / REVERT
        // ------------------------------------------------------------------

        public async Task<bool> ApplyAsync(Tweak t)
        {
            Log?.Invoke($"Applying: {t.Title}");
            try
            {
                switch (t.Kind)
                {
                    case TweakKind.Registry:
                        ApplyRegistry(t, revert: false);
                        break;
                    case TweakKind.PowerCfg:
                    case TweakKind.Command:
                        foreach (var cmd in t.ApplyCommands)
                            await RunCmdAsync(cmd);
                        break;
                    case TweakKind.Guide:
                        Log?.Invoke($"'{t.Title}' is a guided tweak - see the steps panel. Nothing auto-applied.");
                        return true;
                }
                t.IsCurrentlyApplied = true;
                Log?.Invoke($"Applied: {t.Title}");
                return true;
            }
            catch (Exception ex)
            {
                Log?.Invoke($"FAILED to apply {t.Title}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RevertAsync(Tweak t)
        {
            Log?.Invoke($"Reverting: {t.Title}");
            try
            {
                switch (t.Kind)
                {
                    case TweakKind.Registry:
                        ApplyRegistry(t, revert: true);
                        break;
                    case TweakKind.PowerCfg:
                    case TweakKind.Command:
                        foreach (var cmd in t.RevertCommands)
                            await RunCmdAsync(cmd);
                        break;
                    case TweakKind.Guide:
                        Log?.Invoke($"'{t.Title}' is guided - revert manually in the external tool.");
                        return true;
                }
                t.IsCurrentlyApplied = false;
                Log?.Invoke($"Reverted: {t.Title}");
                return true;
            }
            catch (Exception ex)
            {
                Log?.Invoke($"FAILED to revert {t.Title}: {ex.Message}");
                return false;
            }
        }

        // ------------------------------------------------------------------
        // REGISTRY
        // ------------------------------------------------------------------

        private void ApplyRegistry(Tweak t, bool revert)
        {
            if (t.RegHive == null || t.RegPath == null || t.RegName == null)
                throw new InvalidOperationException("Registry tweak missing hive/path/name.");

            RegistryKey root = t.RegHive.ToUpper() switch
            {
                "HKLM" => Registry.LocalMachine,
                "HKCU" => Registry.CurrentUser,
                _ => throw new InvalidOperationException($"Unsupported hive {t.RegHive}")
            };

            using var key = root.CreateSubKey(t.RegPath, writable: true)
                            ?? throw new InvalidOperationException($"Could not open {t.RegPath}");

            string? targetValue = revert ? t.RegValueRevert : t.RegValueApply;

            if (revert && targetValue == null)
            {
                // Revert means "delete the value" (it didn't exist by default)
                if (key.GetValue(t.RegName) != null)
                    key.DeleteValue(t.RegName, throwOnMissingValue: false);
                Log?.Invoke($"  registry: deleted {t.RegHive}\\{t.RegPath}\\{t.RegName}");
                return;
            }

            if (t.RegType?.ToUpper() == "STRING")
            {
                key.SetValue(t.RegName, targetValue ?? "", RegistryValueKind.String);
            }
            else // DWORD
            {
                int val = int.Parse(targetValue ?? "0");
                key.SetValue(t.RegName, val, RegistryValueKind.DWord);
            }

            Log?.Invoke($"  registry: set {t.RegHive}\\{t.RegPath}\\{t.RegName} = {targetValue}");
        }

        /// <summary>Reads current registry state to decide if a tweak is already applied.</summary>
        public bool IsRegistryTweakApplied(Tweak t)
        {
            if (t.Kind != TweakKind.Registry || t.RegHive == null || t.RegPath == null || t.RegName == null)
                return false;

            try
            {
                RegistryKey root = t.RegHive.ToUpper() == "HKLM"
                    ? Registry.LocalMachine : Registry.CurrentUser;

                using var key = root.OpenSubKey(t.RegPath);
                var current = key?.GetValue(t.RegName);
                if (current == null) return false;
                return current.ToString() == t.RegValueApply;
            }
            catch { return false; }
        }

        // ------------------------------------------------------------------
        // PROCESS HELPERS
        // ------------------------------------------------------------------

        private Task<bool> RunCmdAsync(string command)
        {
            Log?.Invoke($"  cmd: {command}");
            return RunProcessAsync("cmd.exe", $"/c {command}");
        }

        private Task<bool> RunPowerShellAsync(string psCommand)
        {
            return RunProcessAsync("powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -Command \"{psCommand}\"");
        }

        private async Task<bool> RunProcessAsync(string exe, string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var p = Process.Start(psi);
                if (p == null) return false;

                string stdout = await p.StandardOutput.ReadToEndAsync();
                string stderr = await p.StandardError.ReadToEndAsync();
                await p.WaitForExitAsync();

                if (!string.IsNullOrWhiteSpace(stderr))
                    Log?.Invoke($"    ! {stderr.Trim()}");

                return p.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Log?.Invoke($"    process error: {ex.Message}");
                return false;
            }
        }

        /// <summary>Launches an external tool (ThrottleStop, Afterburner) if we can find it, else opens its download page.</summary>
        public void LaunchOrDownload(string? exeGuessPath, string downloadUrl)
        {
            try
            {
                if (!string.IsNullOrEmpty(exeGuessPath) && System.IO.File.Exists(exeGuessPath))
                {
                    Process.Start(new ProcessStartInfo(exeGuessPath) { UseShellExecute = true });
                    Log?.Invoke($"Launched {exeGuessPath}");
                }
                else
                {
                    Process.Start(new ProcessStartInfo(downloadUrl) { UseShellExecute = true });
                    Log?.Invoke($"Opened download page: {downloadUrl}");
                }
            }
            catch (Exception ex)
            {
                Log?.Invoke($"Could not launch/open: {ex.Message}");
            }
        }
    }
}

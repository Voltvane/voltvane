using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Voltvane.Services
{
    public class UpdateService
    {
        private const string CURRENT_VERSION = "1.0.3";
        private const string VERSION_URL = "https://raw.githubusercontent.com/Voltvane/voltvane-releases/main/version.json";
        private static readonly HttpClient _http = new();

        public record UpdateInfo(string Version, string DownloadUrl, string ReleaseNotes);

        public static async Task<UpdateInfo?> CheckForUpdateAsync()
        {
            try
            {
                _http.DefaultRequestHeaders.UserAgent.ParseAdd("Voltvane/" + CURRENT_VERSION);
                var json = await _http.GetStringAsync(VERSION_URL);
                var doc = JsonDocument.Parse(json).RootElement;
                var latest = doc.GetProperty("version").GetString()!;
                var downloadUrl = doc.GetProperty("download_url").GetString()!;
                var notes = doc.TryGetProperty("release_notes", out var n) ? n.GetString() ?? "" : "";
                if (IsNewerVersion(latest, CURRENT_VERSION))
                    return new UpdateInfo(latest, downloadUrl, notes);
                return null;
            }
            catch { return null; }
        }

        public static async Task PromptAndUpdateAsync(UpdateInfo update)
        {
            var notes = string.IsNullOrEmpty(update.ReleaseNotes) ? "" : $"\n\nWhat's new:\n{update.ReleaseNotes}";
            var result = MessageBox.Show(
                $"Voltvane {update.Version} is available (you have {CURRENT_VERSION}).{notes}\n\nUpdate now?",
                "Update available", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (result != DialogResult.Yes) return;

            try
            {
                var zipPath = Path.Combine(Path.GetTempPath(), "Voltvane_update.zip");
                using var response = await _http.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                using var stream = await response.Content.ReadAsStreamAsync();
                using var fileStream = File.Create(zipPath);
                var buffer = new byte[81920];
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                fileStream.Close();

                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName!;
                var installDir = Path.GetDirectoryName(exePath)!;
                var batchPath = Path.Combine(Path.GetTempPath(), "voltvane_update.bat");
                File.WriteAllText(batchPath,
                    $"@echo off\r\ntimeout /t 2 /nobreak >nul\r\n" +
                    $"powershell -NoProfile -ExecutionPolicy Bypass -Command \"Expand-Archive -Path '{zipPath}' -DestinationPath '{installDir}' -Force\"\r\n" +
                    $"del \"{zipPath}\"\r\nstart \"\" \"{exePath}\"\r\ndel \"%~f0\"\r\n");

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe", $"/c \"{batchPath}\"")
                { CreateNoWindow = true, UseShellExecute = false });
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update failed: {ex.Message}\n\nDownload manually from voltvane.com",
                    "Update failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static bool IsNewerVersion(string latest, string current)
        {
            try { return new Version(latest) > new Version(current); }
            catch { return false; }
        }
    }
}

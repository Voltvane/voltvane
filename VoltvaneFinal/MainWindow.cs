using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Voltvane.Services;
using Microsoft.Win32;

namespace Voltvane
{
    public class MainWindow : Form
    {
        private WebView2 _webView = new();
        private System.Windows.Forms.NotifyIcon _trayIcon = new();
        private bool _startMinimized = false;
        private static readonly System.Net.Http.HttpClient _http = new();
        private const string SUPABASE_URL = "https://hoergivmasjnpkqoblbw.supabase.co";
        private const string SUPABASE_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImhvZXJnaXZtYXNqbnBrcW9ibGJ3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODAyMTIxMzIsImV4cCI6MjA5NTc4ODEzMn0.Imiej6T8Y1Erw18aJ7DzU-fyrPfHwvBgyYwb4nXDgz0";
        private string _accessToken = "";
        private string _userId = "";
        private bool _isPro = false;
        private readonly StateStore _store = new();
        private readonly TweakEngine _engine = new();

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public MainWindow()
        {
            Text = "Voltvane";
            Width = 1100;
            Height = 700;
            MinimumSize = new System.Drawing.Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = System.Drawing.Color.FromArgb(10, 12, 16);

            var dark = 1;
            DwmSetWindowAttribute(Handle, 20, ref dark, sizeof(int));
            var borderColor = unchecked((int)0xFF2FD9AE);
            DwmSetWindowAttribute(Handle, 34, ref borderColor, sizeof(int));
            var corner = 2;
            DwmSetWindowAttribute(Handle, 33, ref corner, sizeof(int));

            _webView.Dock = DockStyle.Fill;
            Controls.Add(_webView);
            Load += OnLoad;

            // Set window icon
            try
            {
                var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "voltvane.ico");
                if (File.Exists(iconPath))
                {
                    var ico = new System.Drawing.Icon(iconPath);
                    Icon = ico;

                    // Tray icon
                    _trayIcon.Icon = ico;
                    _trayIcon.Text = "Voltvane";
                    _trayIcon.Visible = true;
                    _trayIcon.DoubleClick += (s, e) => ShowWindow();

                    // Tray context menu
                    var menu = new ContextMenuStrip();
                    menu.Items.Add("Open Voltvane", null, (s, e) => ShowWindow());
                    menu.Items.Add("-");
                    menu.Items.Add("Exit", null, (s, e) => { _trayIcon.Visible = false; Application.Exit(); });
                    _trayIcon.ContextMenuStrip = menu;
                }
            }
            catch { }

            // Check if launched on startup (minimized)
            var args = Environment.GetCommandLineArgs();
            _startMinimized = args.Contains("--startup");

            // Fix WebView2 black screen on focus/resize in borderless window
            Activated += (s, e) => _webView.Refresh();
            ResizeEnd += (s, e) => _webView.Refresh();
            Move += (s, e) => _webView.Refresh();

            // Register voltvane:// URL scheme for OAuth deep linking
            RegisterUrlScheme();
            // Enable startup on first launch if not already set
            if (!System.IO.File.Exists(System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Voltvane", "startup_configured.txt")))
            {
                SetStartup(true);
                var flag = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Voltvane", "startup_configured.txt");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(flag)!);
                System.IO.File.WriteAllText(flag, "1");
            }
        }

        private static void RegisterUrlScheme()
        {
            try
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName!;
                using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Classes\voltvane");
                key.SetValue("", "URL:Voltvane Protocol");
                key.SetValue("URL Protocol", "");
                using var iconKey = key.CreateSubKey("DefaultIcon");
                iconKey.SetValue("", $"{exePath},0");
                using var cmdKey = key.CreateSubKey(@"shell\open\command");
                cmdKey.SetValue("", $"\"{exePath}\" \"%1\"");
            }
            catch { }
        }

        private const int WM_NCHITTEST = 0x84;
        private const int HTCLIENT = 1;
        private const int HTCAPTION = 2;

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_NCHITTEST && (int)m.Result == HTCLIENT)
            {
                var cursor = PointToClient(Cursor.Position);
                // Only treat as caption if in top 42px AND not in right 200px (where buttons are)
                if (cursor.Y >= 0 && cursor.Y <= 42 && cursor.X < Width - 200)
                    m.Result = (IntPtr)HTCAPTION;
            }
        }

        private void ShowWindow()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            BringToFront();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Minimize to tray instead of closing
            e.Cancel = true;
            Hide();
            _trayIcon.ShowBalloonTip(2000, "Voltvane", "Running in the background. Double-click to reopen.", ToolTipIcon.Info);
        }

        private async void OnLoad(object? sender, EventArgs e)
        {
            // If launched on startup, hide to tray immediately
            if (_startMinimized)
            {
                Hide();
                WindowState = FormWindowState.Minimized;
            }

            var dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Voltvane", "WebData");
            var options = new CoreWebView2EnvironmentOptions("--disable-features=msSmartScreen --enable-features=OverlayScrollbar");
            var env = await CoreWebView2Environment.CreateAsync(null, dataFolder, options);
            await _webView.EnsureCoreWebView2Async(env);

            _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            _webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
            _webView.DefaultBackgroundColor = System.Drawing.Color.FromArgb(10, 12, 16);

            _webView.CoreWebView2.WebMessageReceived += OnMessage;
            _webView.CoreWebView2.NavigateToString(GetHtml());

            // Wait for page to load then check session
            _webView.CoreWebView2.DOMContentLoaded += async (s, e) =>
            {
                try
                {
                    if (LoadSession())
                        await LoadApp();
                    else
                        Send("hide_loading", new { });
                }
                catch
                {
                    Send("hide_loading", new { });
                }
            };

            // Check update after delay
            _ = Task.Delay(5000).ContinueWith(async _ =>
            {
                var update = await UpdateService.CheckForUpdateAsync();
                if (update != null)
                    Send("update_available", new { version = update.Version, notes = update.ReleaseNotes });
            });
        }

        private async void OnMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var msg = e.TryGetWebMessageAsString();
                var doc = JsonDocument.Parse(msg).RootElement;
                var action = doc.GetProperty("action").GetString();

                switch (action)
                {
                    case "window_close": Application.Exit(); break;
                    case "window_minimize": Invoke(() => WindowState = FormWindowState.Minimized); break;
                    case "window_maximize": Invoke(() => WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized); break;

                    case "login": await HandleLogin(doc); break;
                    case "signup": await HandleSignup(doc); break;
                    case "logout": HandleLogout(); break;
                    case "forgot_password": await HandleForgotPassword(doc); break;
                    case "oauth": HandleOAuth(doc); break;
                    case "guest": HandleGuest(); break;
                    case "load_app": await LoadApp(); break;

                    case "get_tweaks": SendTweaks(); break;
                    case "apply_tweak": await HandleTweak(doc, true); break;
                    case "revert_tweak": await HandleTweak(doc, false); break;
                    case "apply_all_recommended":
                    var ff = doc.TryGetProperty("formFactor", out var ffProp) ? ffProp.GetString() ?? "pc" : "pc";
                    await ApplyAllRecommended(ff); break;

                    case "open_throttlestop": OpenTool("ThrottleStop", "ThrottleStop.exe"); break;
                    case "open_afterburner": OpenTool("MSI Afterburner", "MSIAfterburner.exe"); break;
                    case "open_url": OpenUrl(doc); break;
                case "set_startup": SetStartup(doc.GetProperty("enabled").GetBoolean()); break;
                case "get_startup": Send("startup_state", new { enabled = GetStartupEnabled() }); break;

                    case "do_update": await DoUpdate(doc); break;
                }
            }
            catch (Exception ex)
            {
                Send("error", new { message = ex.Message });
            }
        }

        // ── Auth ──

        private async Task HandleLogin(JsonElement doc)
        {
            var email = doc.GetProperty("email").GetString()!;
            var password = doc.GetProperty("password").GetString()!;

            var body = JsonSerializer.Serialize(new { email, password });
            var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, $"{SUPABASE_URL}/auth/v1/token?grant_type=password");
            req.Headers.Add("apikey", SUPABASE_KEY);
            req.Content = new System.Net.Http.StringContent(body, System.Text.Encoding.UTF8, "application/json");

            var res = await _http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(json).RootElement;

            if (!res.IsSuccessStatusCode)
            {
                var err = data.TryGetProperty("error_description", out var ed) ? ed.GetString() : "Login failed";
                Send("auth_error", new { message = err });
                return;
            }

            _accessToken = data.GetProperty("access_token").GetString()!;
            _userId = data.GetProperty("user").GetProperty("id").GetString()!;
            SaveSession();
            await LoadProStatus();
            await LoadApp();
        }

        private async Task HandleSignup(JsonElement doc)
        {
            var email = doc.GetProperty("email").GetString()!;
            var password = doc.GetProperty("password").GetString()!;

            var body = JsonSerializer.Serialize(new { email, password });
            var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, $"{SUPABASE_URL}/auth/v1/signup");
            req.Headers.Add("apikey", SUPABASE_KEY);
            req.Content = new System.Net.Http.StringContent(body, System.Text.Encoding.UTF8, "application/json");

            var res = await _http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(json).RootElement;

            if (!res.IsSuccessStatusCode)
            {
                var err = data.TryGetProperty("error_description", out var ed) ? ed.GetString() : "Signup failed";
                Send("auth_error", new { message = err });
                return;
            }

            Send("auth_success", new { message = "✓ Check your email to confirm your account." });
        }

        private async Task HandleForgotPassword(JsonElement doc)
        {
            var email = doc.GetProperty("email").GetString()!;
            var body = JsonSerializer.Serialize(new { email, redirectTo = "https://voltvane.com/account.html" });
            var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, $"{SUPABASE_URL}/auth/v1/recover");
            req.Headers.Add("apikey", SUPABASE_KEY);
            req.Content = new System.Net.Http.StringContent(body, System.Text.Encoding.UTF8, "application/json");
            await _http.SendAsync(req);
            Send("auth_success", new { message = "✓ Password reset email sent." });
        }

        private void HandleOAuth(JsonElement doc)
        {
            var provider = doc.GetProperty("provider").GetString()!;
            // Use deep link redirect so app auto-logs in
            var redirectTo = Uri.EscapeDataString("https://voltvane.com/go-to-app.html");
            var url = $"{SUPABASE_URL}/auth/v1/authorize?provider={provider}&redirect_to={redirectTo}";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            Send("auth_status", new { message = "Browser opened — log in to continue.", type = "info" });

            // Poll for OAuth callback file
            _ = PollForOAuthCallback();
        }

        private async Task PollForOAuthCallback()
        {
            var tokenFile = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Voltvane", "oauth_callback.txt");

            // Poll for up to 5 minutes
            for (int i = 0; i < 300; i++)
            {
                await Task.Delay(1000);
                if (System.IO.File.Exists(tokenFile))
                {
                    try
                    {
                        var raw = System.IO.File.ReadAllText(tokenFile);
                        System.IO.File.Delete(tokenFile);

                        // Parse voltvane://auth#access_token=...&refresh_token=...
                        var uri = new Uri(raw);
                        var fragment = uri.Fragment.TrimStart('#');
                        var query = uri.Query.TrimStart('?');
                        var combined = string.IsNullOrEmpty(fragment) ? query : fragment;
                        var pairs = combined.Split('&');
                        string token = "", refresh = "", userId = "";

                        foreach (var pair in pairs)
                        {
                            var kv = pair.Split('=', 2);
                            if (kv.Length != 2) continue;
                            if (kv[0] == "access_token") token = Uri.UnescapeDataString(kv[1]);
                            if (kv[0] == "refresh_token") refresh = Uri.UnescapeDataString(kv[1]);
                        }

                        if (!string.IsNullOrEmpty(token))
                        {
                            // Decode user ID from JWT
                            try
                            {
                                var parts = token.Split('.');
                                if (parts.Length >= 2)
                                {
                                    var payload = parts[1];
                                    while (payload.Length % 4 != 0) payload += "=";
                                    var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                                    var jdoc = JsonDocument.Parse(json).RootElement;
                                    userId = jdoc.TryGetProperty("sub", out var sub) ? sub.GetString() ?? "" : "";
                                }
                            }
                            catch { }

                            _accessToken = token;
                            _userId = userId;
                            SaveSession();
                            await LoadApp();
                        }
                    }
                    catch (Exception ex)
                    {
                        Send("auth_error", new { message = $"OAuth failed: {ex.Message}" });
                    }
                    return;
                }
            }
        }

        private void HandleLogout()
        {
            _accessToken = "";
            _userId = "";
            _isPro = false;
            DeleteSession();
            Send("show_login", new { });
        }

        private void HandleGuest()
        {
            _isPro = false;
            Send("app_loaded", new
            {
                isPro = false,
                email = "Guest",
                isGuest = true,
                tweaks = GetTweakData(),
                hardware = HardwareDetector.Detect()
            });
        }

        // ── Session ──

        private string SessionPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Voltvane", "session.json");

        private void SaveSession()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SessionPath)!);
            File.WriteAllText(SessionPath, JsonSerializer.Serialize(new { token = _accessToken, userId = _userId }));
        }

        private void DeleteSession()
        {
            if (File.Exists(SessionPath)) File.Delete(SessionPath);
        }

        private bool LoadSession()
        {
            try
            {
                if (!File.Exists(SessionPath)) return false;
                var data = JsonDocument.Parse(File.ReadAllText(SessionPath)).RootElement;
                _accessToken = data.GetProperty("token").GetString()!;
                _userId = data.GetProperty("userId").GetString()!;
                return !string.IsNullOrEmpty(_accessToken);
            }
            catch { return false; }
        }

        private async Task LoadProStatus()
        {
            try
            {
                var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get,
                    $"{SUPABASE_URL}/rest/v1/profiles?id=eq.{_userId}&select=is_pro");
                req.Headers.Add("apikey", SUPABASE_KEY);
                req.Headers.Add("Authorization", $"Bearer {_accessToken}");
                var res = await _http.SendAsync(req);
                var json = await res.Content.ReadAsStringAsync();
                var arr = JsonDocument.Parse(json).RootElement;
                if (arr.GetArrayLength() > 0)
                    _isPro = arr[0].GetProperty("is_pro").GetBoolean();
            }
            catch { }
        }

        private async Task LoadApp()
        {
            Send("loading", new { message = "Loading your profile..." });
            await LoadProStatus();

            var email = "";
            try
            {
                var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, $"{SUPABASE_URL}/auth/v1/user");
                req.Headers.Add("apikey", SUPABASE_KEY);
                req.Headers.Add("Authorization", $"Bearer {_accessToken}");
                var res = await _http.SendAsync(req);
                var data = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
                email = data.TryGetProperty("email", out var em) ? (em.GetString() ?? "") : "";
            }
            catch { }

            Send("app_loaded", new
            {
                isPro = _isPro,
                email,
                isGuest = false,
                tweaks = GetTweakData(),
                hardware = HardwareDetector.Detect()
            });
        }

        // ── Tweaks ──

        private void SendTweaks() => Send("tweaks_data", new { tweaks = GetTweakData() });

        private object[] GetTweakData()
        {
            var tweaks = TweakCatalog.GetAll();
            return tweaks.Select(t => new
            {
                t.Id, t.Title, t.ShortDescription, t.Explanation,
                t.LaptopWarning, t.Category, t.IsPro,
                t.RecommendedForPC, t.RecommendedForLaptop,
                risk = t.Risk.ToString(),
                target = t.Target.ToString(),
                kind = t.Kind.ToString(),
                isEnabled = _store.IsEnabled(t.Id),
                t.GuideToolName, t.GuideSteps, t.GuideDownloadUrl
            }).ToArray();
        }

        private async Task HandleTweak(JsonElement doc, bool apply)
        {
            var id = doc.GetProperty("id").GetString()!;
            var tweak = TweakCatalog.GetAll().FirstOrDefault(t => t.Id == id);
            if (tweak == null) { Send("tweak_error", new { id, message = "Tweak not found" }); return; }
            if (tweak.IsPro && !_isPro) { Send("tweak_locked", new { id }); return; }

            try
            {
                if (apply) await _engine.ApplyAsync(tweak);
                else await _engine.RevertAsync(tweak);
                _store.Set(id, apply);
                Send("tweak_done", new { id, enabled = apply });
            }
            catch (Exception ex)
            {
                Send("tweak_error", new { id, message = ex.Message });
            }
        }

        private async Task ApplyAllRecommended(string formFactor = "pc")
        {
            var isLaptop = formFactor == "laptop";
            var tweaks = TweakCatalog.GetAll().Where(t =>
                !t.IsPro &&
                t.Kind != Models.TweakKind.Guide &&
                (isLaptop ? t.RecommendedForLaptop : t.RecommendedForPC) &&
                (t.Target == Models.FormFactor.Both || (isLaptop ? t.Target == Models.FormFactor.Laptop : t.Target == Models.FormFactor.PC)));

            foreach (var t in tweaks)
            {
                try { await _engine.ApplyAsync(t); _store.Set(t.Id, true); } catch { }
                Send("tweak_done", new { id = t.Id, enabled = true });
            }
            Send("all_recommended_done", new { });
        }

        // ── Tools ──

        private void OpenTool(string folder, string exe)
        {
            // Try multiple possible install locations
            var installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Voltvane");
            var searchDir = Path.Combine(installDir, folder);

            string? path = null;

            // Search recursively in the tool's folder
            if (Directory.Exists(searchDir))
                path = Directory.GetFiles(searchDir, exe, SearchOption.AllDirectories).FirstOrDefault();

            // Fallback: search anywhere in Voltvane install dir
            if (path == null && Directory.Exists(installDir))
                path = Directory.GetFiles(installDir, exe, SearchOption.AllDirectories).FirstOrDefault();

            if (path != null && File.Exists(path))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
            else
                Send("error", new { message = $"{exe} not found. Make sure Voltvane is installed via the installer." });
        }

        private void OpenUrl(JsonElement doc)
        {
            var url = doc.GetProperty("url").GetString()!;
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }

        // ── Update ──

        private async Task DoUpdate(JsonElement doc)
        {
            var url = doc.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "";
            var version = doc.TryGetProperty("version", out var v) ? v.GetString() ?? "" : "";
            var update = new UpdateService.UpdateInfo(version, url, "");
            await UpdateService.PromptAndUpdateAsync(update);
        }

        // ── Helpers ──

        private static void SetStartup(bool enable)
        {
            try
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName!;
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true)!;
                if (enable)
                    key.SetValue("Voltvane", $"\"{exePath}\" --startup");
                else
                    key.DeleteValue("Voltvane", false);
            }
            catch { }
        }

        private static bool GetStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run")!;
                return key.GetValue("Voltvane") != null;
            }
            catch { return false; }
        }

        private void Send(string type, object data)
        {
            var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(new { type, data }, opts);
            Invoke(() => _webView.CoreWebView2.PostWebMessageAsString(json));
        }

        private static string GetHtml()
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream("Voltvane.UI.app.html")!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

    }
}

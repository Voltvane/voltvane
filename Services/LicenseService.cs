using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Voltvane.Services
{
    public enum Tier { Guest, Pro }

    public class LicenseService
    {
        // ---------------------------------------------------------------
        // CHANGE THESE before shipping:
        // ---------------------------------------------------------------
        public const string SupabaseUrl = "https://hoergivmasjnpkqoblbw.supabase.co";
        public const string SupabaseAnonKey =  "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImhvZXJnaXZtYXNqbnBrcW9ibGJ3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODAyMTIxMzIsImV4cCI6MjA5NTc4ODEzMn0.Imiej6T8Y1Erw18aJ7DzU-fyrPfHwvBgyYwb4nXDgz0";
        // ---------------------------------------------------------------

        private static readonly HttpClient Http = new();
        private readonly string _dir;
        private readonly string _sessionFile;

        public Tier CurrentTier { get; private set; } = Tier.Guest;
        public string? UserEmail { get; private set; }
        public string? AccessToken { get; private set; }

        // Fired when a lapsed subscription is detected on launch
        public event Action? SubscriptionLapsed;

        public LicenseService()
        {
            _dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Voltvane");
            _sessionFile = Path.Combine(_dir, "session.json");
            Http.DefaultRequestHeaders.Add("apikey", SupabaseAnonKey);
        }

        // ------------------------------------------------------------------
        // LOAD SAVED SESSION ON APP LAUNCH + CHECK SUBSCRIPTION STATUS
        // ------------------------------------------------------------------

        /// <summary>
        /// Called on startup. Restores a saved session and verifies Pro status
        /// against the server. If subscription lapsed, fires SubscriptionLapsed.
        /// Returns true if a valid Pro session was restored.
        /// </summary>
        public async Task<bool> TryRestoreSessionAsync()
        {
            try
            {
                if (!File.Exists(_sessionFile)) return false;
                var data = JsonSerializer.Deserialize<SessionData>(File.ReadAllText(_sessionFile));
                if (data == null || string.IsNullOrEmpty(data.AccessToken)) return false;

                // Try to refresh the token for a fresh one
                string? refreshed = await RefreshTokenAsync(data.RefreshToken);
                string token = refreshed ?? data.AccessToken;

                // Check current Pro status from server
                bool isPro = await FetchIsProAsync(token);

                // Lapse detection: was Pro last session, not Pro now = subscription lapsed
                if (data.WasPro && !isPro)
                    SubscriptionLapsed?.Invoke();

                CurrentTier = isPro ? Tier.Pro : Tier.Guest;
                UserEmail = data.Email;
                AccessToken = token;

                // Save updated session with refreshed token and current Pro status
                SaveSession(new SessionData
                {
                    AccessToken = refreshed ?? data.AccessToken,
                    RefreshToken = data.RefreshToken,
                    Email = data.Email,
                    WasPro = isPro
                });

                return true; // session restored — caller checks CurrentTier to decide what to show
            }
            catch { return false; }
        }

        // ------------------------------------------------------------------
        // LOGIN
        // ------------------------------------------------------------------

        public class LoginResult
        {
            public bool Success { get; set; }
            public string? Error { get; set; }
            public bool IsPro { get; set; }
        }

        public async Task<LoginResult> LoginAsync(string email, string password)
        {
            try
            {
                var body = JsonSerializer.Serialize(new { email, password });
                var req = new HttpRequestMessage(HttpMethod.Post,
                    $"{SupabaseUrl}/auth/v1/token?grant_type=password");
                req.Headers.Add("apikey", SupabaseAnonKey);
                req.Content = new StringContent(body, Encoding.UTF8, "application/json");
                var resp = await Http.SendAsync(req);
                var json = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    var err = JsonDocument.Parse(json).RootElement;
                    return new LoginResult
                    {
                        Success = false,
                        Error = err.TryGetProperty("error_description", out var ed)
                            ? ed.GetString() : "Login failed. Check your email and password."
                    };
                }

                var doc = JsonDocument.Parse(json).RootElement;
                string accessToken = doc.GetProperty("access_token").GetString()!;
                string refreshToken = doc.GetProperty("refresh_token").GetString()!;

                bool isPro = await FetchIsProAsync(accessToken);

                CurrentTier = isPro ? Tier.Pro : Tier.Guest;
                UserEmail = email;
                AccessToken = accessToken;

                SaveSession(new SessionData
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    Email = email,
                    WasPro = isPro
                });

                return new LoginResult { Success = true, IsPro = isPro };
            }
            catch (Exception ex)
            {
                return new LoginResult { Success = false, Error = "Could not reach the server. Check your connection.\n" + ex.Message };
            }
        }

        // ------------------------------------------------------------------
        // SIGNUP
        // ------------------------------------------------------------------

        public async Task<LoginResult> SignUpAsync(string email, string password)
        {
            try
            {
                var body = JsonSerializer.Serialize(new { email, password });
                var req = new HttpRequestMessage(HttpMethod.Post, $"{SupabaseUrl}/auth/v1/signup");
                req.Headers.Add("apikey", SupabaseAnonKey);
                req.Content = new StringContent(body, Encoding.UTF8, "application/json");
                var resp = await Http.SendAsync(req);
                var json = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    var err = JsonDocument.Parse(json).RootElement;
                    return new LoginResult
                    {
                        Success = false,
                        Error = err.TryGetProperty("error_description", out var ed)
                            ? ed.GetString() : "Sign up failed."
                    };
                }

                // After signup, log them in to get a session
                return await LoginAsync(email, password);
            }
            catch (Exception ex)
            {
                return new LoginResult { Success = false, Error = "Could not reach the server.\n" + ex.Message };
            }
        }

        // ------------------------------------------------------------------
        // SIGN OUT
        // ------------------------------------------------------------------

        public void SignOut()
        {
            CurrentTier = Tier.Guest;
            UserEmail = null;
            AccessToken = null;
            try { if (File.Exists(_sessionFile)) File.Delete(_sessionFile); } catch { }
        }

        public void ContinueAsGuest()
        {
            CurrentTier = Tier.Guest;
            UserEmail = null;
            AccessToken = null;
        }

        // ------------------------------------------------------------------
        // INTERNALS
        // ------------------------------------------------------------------

        /// <summary>
        /// Fetches is_pro from the profiles table for the currently-authed user.
        /// Returns false if any error occurs (fail safe = Guest).
        /// </summary>
        private async Task<bool> FetchIsProAsync(string accessToken)
        {
            try
            {
                // First get the user's ID
                var userReq = new HttpRequestMessage(HttpMethod.Get, $"{SupabaseUrl}/auth/v1/user");
                userReq.Headers.Add("apikey", SupabaseAnonKey);
                userReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var userResp = await Http.SendAsync(userReq);
                if (!userResp.IsSuccessStatusCode) return false;
                var userDoc = JsonDocument.Parse(await userResp.Content.ReadAsStringAsync()).RootElement;
                string userId = userDoc.GetProperty("id").GetString()!;

                // Now fetch that specific user's profile row
                var req = new HttpRequestMessage(HttpMethod.Get,
                    $"{SupabaseUrl}/rest/v1/profiles?select=is_pro&id=eq.{userId}&limit=1");
                req.Headers.Add("apikey", SupabaseAnonKey);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var resp = await Http.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return false;

                var json = await resp.Content.ReadAsStringAsync();
                var arr = JsonDocument.Parse(json).RootElement;
                if (arr.GetArrayLength() == 0) return false;
                return arr[0].GetProperty("is_pro").GetBoolean();
            }
            catch { return false; }
        }

        private async Task<string?> RefreshTokenAsync(string? refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken)) return null;
            try
            {
                var body = JsonSerializer.Serialize(new { refresh_token = refreshToken });
                var req = new HttpRequestMessage(HttpMethod.Post,
                    $"{SupabaseUrl}/auth/v1/token?grant_type=refresh_token");
                req.Headers.Add("apikey", SupabaseAnonKey);
                req.Content = new StringContent(body, Encoding.UTF8, "application/json");
                var resp = await Http.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return null;
                var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
                return doc.GetProperty("access_token").GetString();
            }
            catch { return null; }
        }

        private void SaveSession(SessionData data)
        {
            try
            {
                Directory.CreateDirectory(_dir);
                File.WriteAllText(_sessionFile,
                    JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        private class SessionData
        {
            public string AccessToken { get; set; } = "";
            public string? RefreshToken { get; set; }
            public string? Email { get; set; }
            public bool WasPro { get; set; }
        }
    }
}

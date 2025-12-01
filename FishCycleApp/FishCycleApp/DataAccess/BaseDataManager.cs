using System;
using System.IO;
using System.Threading.Tasks;

namespace FishCycleApp.DataAccess
{
    public abstract class BaseDataManager
    {
        protected static Supabase.Client? _supabaseClient;

        protected async Task<Supabase.Client> GetClientAsync()
        {
            if (_supabaseClient != null)
                return _supabaseClient;

            LoadEnv();

            var url = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? "";
            var key = Environment.GetEnvironmentVariable("SUPABASE_KEY") ?? "";

            var options = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true
            };

            _supabaseClient = new Supabase.Client(url, key, options);
            await _supabaseClient.InitializeAsync();

            return _supabaseClient;
        }

        protected void LoadEnv()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");

                if (!File.Exists(path))
                {
                    string projectRoot =
                        Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?
                        .Parent?.Parent?.Parent?.FullName ?? "";

                    path = Path.Combine(projectRoot, ".env");
                }

                if (!File.Exists(path)) return;

                foreach (var line in File.ReadAllLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                        continue;

                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                        Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
                }
            }
            catch { }
        }
    }
}

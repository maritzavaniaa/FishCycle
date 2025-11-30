using Npgsql;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using FishCycleApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace FishCycleApp.DataAccess
{
    public class ClientDataManager
    {
        private static Supabase.Client? _supabaseClient;

        private async Task<Supabase.Client> GetClientAsync()
        {
            if (_supabaseClient != null) return _supabaseClient;

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

        private void LoadEnv()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
                if (!File.Exists(path))
                {
                    string projectRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.Parent?.Parent?.FullName ?? "";
                    path = Path.Combine(projectRoot, ".env");
                }
                if (!File.Exists(path)) return;

                foreach (var line in File.ReadAllLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2) Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
                }
            }
            catch { }
        }

        public async Task<List<Client>> LoadClientDataAsync()
        {
            try
            {
                var client = await GetClientAsync();
                var result = await client.From<Client>().Select("*").Get();
                return result.Models;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client] Load error: {ex.Message}");
                return new List<Client>();
            }
        }

        public async Task<bool> InsertClientAsync(Client c)
        {
            try
            {
                var client = await GetClientAsync();
                await client.From<Client>().Insert(c);
                return true;
            }
            catch (Supabase.Postgrest.Exceptions.PostgrestException pex)
            {
                Console.WriteLine($"[Client] Insert Rejected: {pex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client] Insert error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateClientAsync(Client c)
        {
            try
            {
                var client = await GetClientAsync();
                await client.From<Client>().Update(c);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client] Update error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteClientAsync(string clientID)
        {
            try
            {
                var client = await GetClientAsync();
                await client.From<Client>().Where(x => x.ClientID == clientID).Delete();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client] Delete error: {ex.Message}");
                return false;
            }
        }

        public async Task<Client?> GetClientByIDAsync(string clientID)
        {
            try
            {
                var client = await GetClientAsync();
                var result = await client
                    .From<Client>()
                    .Select("*")
                    .Where(x => x.ClientID == clientID)
                    .Get();

                return result.Models.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }
    }
}
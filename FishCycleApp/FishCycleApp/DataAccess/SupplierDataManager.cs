using Npgsql;
using NpgsqlTypes;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using FishCycleApp.Models;
using System.Collections.Generic;
using System.Linq;
using FishCycleApp.Models;
using System.IO;

namespace FishCycleApp.DataAccess
{
    public class SupplierDataManager
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

        public async Task<List<Supplier>> LoadSupplierDataAsync()
        {
            try
            {
                var client = await GetClientAsync();
                var result = await client.From<Supplier>().Select("*").Get();
                return result.Models;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Supplier] Load error: {ex.Message}");
                return new List<Supplier>();
            }
        }

        public async Task<bool> InsertSupplierAsync(Supplier s)
        {
            try
            {
                var client = await GetClientAsync();
                await client.From<Supplier>().Insert(s);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Supplier] Insert error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateSupplierAsync(Supplier s)
        {
            try
            {
                var client = await GetClientAsync();
                await client.From<Supplier>().Update(s);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Supplier] Update error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteSupplierAsync(string supplierID)
        {
            try
            {
                var client = await GetClientAsync();
                await client.From<Supplier>().Where(x => x.SupplierID == supplierID).Delete();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Supplier] Delete error: {ex.Message}");
                return false;
            }
        }

        public async Task<Supplier?> GetSupplierByIDAsync(string supplierID)
        {
            try
            {
                var client = await GetClientAsync();
                var result = await client
                    .From<Supplier>()
                    .Select("*")
                    .Where(x => x.SupplierID == supplierID)
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
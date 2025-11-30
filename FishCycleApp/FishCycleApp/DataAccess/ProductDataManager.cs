using Npgsql;
using NpgsqlTypes;
using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FishCycleApp.Models;
using System.Collections.Generic;
using System.Linq;

namespace FishCycleApp.DataAccess
{
    public class ProductDataManager
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
                    string root = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.Parent?.Parent?.FullName ?? "";
                    path = Path.Combine(root, ".env");
                }
                if (!File.Exists(path)) return;
                foreach (var line in File.ReadAllLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2) Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
                }
            }
            catch { }
        }

        public async Task<List<Product>> LoadProductDataAsync()
        {
            try
            {
                var client = await GetClientAsync();

                var result = await client
                    .From<Product>()
                    .Select("*")
                    .Get();

                System.Diagnostics.Debug.WriteLine($"DEBUG: Data loaded count = {result.Models.Count}");

                return result.Models;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Product] Load error: {ex.Message}");
                return new List<Product>();
            }
        }

        public async Task<bool> InsertProductAsync(Product p)
        {
            try
            {
                var client = await GetClientAsync();
                await client.From<Product>().Insert(p);
                return true;
            }
            catch (Supabase.Postgrest.Exceptions.PostgrestException pex)
            {
                System.Windows.MessageBox.Show($"Database Error: {pex.Message}", "Supabase Error");
                return false;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"General Error: {ex.Message}", "App Error");
                return false;
            }
        }

        public async Task<bool> UpdateProductAsync(Product p)
        {
            try
            {
                var client = await GetClientAsync();
                await client.From<Product>().Update(p);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Product] Update error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(string productID)
        {
            try
            {
                var client = await GetClientAsync();
                await client.From<Product>().Where(x => x.ProductID == productID).Delete();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Product] Delete error: {ex.Message}");
                return false;
            }
        }

        public async Task<Product?> GetProductByIDAsync(string productID, CancellationToken ct = default)
        {
            try
            {
                var client = await GetClientAsync();

                var result = await client
                    .From<Product>()
                    .Select("*, supplier(*)")
                    .Where(x => x.ProductID == productID)
                    .Get(ct); 

                return result.Models.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        public StockStatistics CalculateStatistics(List<Product> products)
        {
            if (products == null || !products.Any())
                return new StockStatistics();

            return new StockStatistics
            {
                TotalProductTypes = products.Count,
                TotalStockQuantity = products.Sum(p => p.Quantity),
                TotalStockValue = products.Sum(p => p.TotalValue),
                LowStockCount = products.Count(p => p.Quantity < 10)
            };
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading; 
using FishCycleApp.Models;
using System.IO;

namespace FishCycleApp.DataAccess
{
    public class TransactionDataManager
    {
        private static Supabase.Client? _supabaseClient;
        private readonly ProductDataManager _productManager = new ProductDataManager();

        private async Task<Supabase.Client> GetClientAsync()
        {
            if (_supabaseClient != null) return _supabaseClient;
            LoadEnv();
            var url = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? "";
            var key = Environment.GetEnvironmentVariable("SUPABASE_KEY") ?? "";
            var options = new Supabase.SupabaseOptions { AutoRefreshToken = true, AutoConnectRealtime = true };
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

        public async Task<List<Transaction>> LoadTransactionDataAsync()
        {
            try
            {
                var client = await GetClientAsync();
                var result = await client.From<Transaction>().Select("*").Get();
                return result.Models;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Transaction] Load error: {ex.Message}");
                return new List<Transaction>();
            }
        }

        public async Task<bool> InsertTransactionAsync(Transaction t)
        {
            try
            {
                var client = await GetClientAsync();
                await client.From<Transaction>().Insert(t);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Transaction] Insert Header error: {ex.Message}");
                return false; 
            }
        }

        public async Task<bool> UpdateTransactionAsync(Transaction t)
        {
            try
            {
                var client = await GetClientAsync();
                await client.From<Transaction>().Update(t);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Transaction] Update error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteTransactionAsync(string transactionID)
        {
            try
            {
                var client = await GetClientAsync();

                var itemsResult = await client
                    .From<TransactionItem>()
                    .Select("*")
                    .Where(x => x.TransactionID == transactionID)
                    .Get();

                var itemsToRestore = itemsResult.Models;

                foreach (var item in itemsToRestore)
                {
                    await _productManager.IncreaseStockAsync(item.ProductID, item.Quantity);
                }

                await client.From<Transaction>().Where(x => x.TransactionID == transactionID).Delete();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Transaction] Delete error: {ex.Message}");
                return false;
            }
        }

        public async Task<Transaction?> GetTransactionByIDAsync(string id, CancellationToken ct = default)
        {
            try
            {
                var client = await GetClientAsync();
                var result = await client
                    .From<Transaction>()
                    .Select("*")
                    .Where(x => x.TransactionID == id)
                    .Get(ct);

                return result.Models.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<TransactionItem>> GetTransactionItemsAsync(string transactionID)
        {
            try
            {
                var client = await GetClientAsync();
                var result = await client
                    .From<TransactionItem>()
                    .Select("*")
                    .Where(x => x.TransactionID == transactionID)
                    .Get();

                return result.Models;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Items] Error: {ex.Message}");
                return new List<TransactionItem>();
            }
        }

        public async Task<bool> InsertTransactionItemsAsync(List<TransactionItem> items)
        {
            try
            {
                var client = await GetClientAsync();
                await client.From<TransactionItem>().Insert(items);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TransactionItem] Insert Items Error: {ex.Message}");
                return false;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using FishCycleApp.Models;
using System.IO;

namespace FishCycleApp.DataAccess
{
    public class TransactionDataManager : BaseDataManager
    {
        private readonly ProductDataManager _productManager = new ProductDataManager();

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

        public async Task<decimal> GetMonthlyRevenueAsync(int year, int month)
        {
            try
            {
                var transactions = await LoadTransactionDataAsync();

                var revenue = transactions
                    .Where(t =>
                        t.PaymentStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase) &&
                        !t.DeliveryStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) &&
                        t.TransactionDate.Month == month &&
                        t.TransactionDate.Year == year
                    )
                    .Sum(t => t.TotalAmount);

                return revenue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Transaction] Revenue calculation error: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> GetMonthlyTransactionCountAsync(int year, int month)
        {
            try
            {
                var transactions = await LoadTransactionDataAsync();

                var count = transactions
                    .Count(t =>
                        t.PaymentStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase) &&
                        !t.DeliveryStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) &&
                        t.TransactionDate.Month == month &&
                        t.TransactionDate.Year == year
                    );

                return count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Transaction] Count error: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> GetActiveDeliveryTodayAsync()
        {
            try
            {
                var transactions = await LoadTransactionDataAsync();

                DateTime today = DateTime.Today;

                var count = transactions
                    .Count(t =>
                        t.TransactionDate.Date == today &&
                        !t.DeliveryStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) &&
                        !t.DeliveryStatus.Equals("Delivered", StringComparison.OrdinalIgnoreCase)
                    );

                return count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Transaction] Active delivery error: {ex.Message}");
                return 0;
            }
        }

        public async Task<List<Transaction>> GetTodayTransactionsAsync()
        {
            try
            {
                var client = await GetClientAsync();

                DateTime today = DateTime.Today;
                DateTime tomorrow = today.AddDays(1);

                var result = await client
                    .From<Transaction>()
                    .Select("*")
                    .Where(x => x.TransactionDate >= today && x.TransactionDate < tomorrow)
                    .Get();

                return result.Models;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Transaction] Today load error: {ex.Message}");
                return new List<Transaction>();
            }
        }
    }
}

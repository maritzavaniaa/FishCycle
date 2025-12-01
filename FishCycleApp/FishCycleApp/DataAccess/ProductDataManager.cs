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
    public class ProductDataManager : BaseDataManager
    {
 
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

        public async Task UpdateStockQuantityAsync(string productID, decimal quantitySold)
        {
            try
            {
                var client = await GetClientAsync();

                var product = await GetProductByIDAsync(productID);

                if (product != null)
                {
                    product.Quantity = product.Quantity - quantitySold;

                    await client.From<Product>().Update(product);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Stock Update] Error: {ex.Message}");
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

        public async Task IncreaseStockAsync(string productID, decimal quantityReturned)
        {
            try
            {
                var client = await GetClientAsync();

                var product = await GetProductByIDAsync(productID);

                if (product != null)
                {
                    product.Quantity += quantityReturned;

                    await client.From<Product>().Update(product);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Restock] Error: {ex.Message}");
            }
        }

        public async Task DecreaseStockAsync(string productID, decimal quantitySold)
        {
            await UpdateStockQuantityAsync(productID, quantitySold);
        }

        public async Task<List<Product>> GetTop5StockAsync()
        {
            try
            {
                var products = await LoadProductDataAsync();

                return products
                    .OrderByDescending(p => p.Quantity)
                    .Take(5)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Stock] Top 5 load error: {ex.Message}");
                return new List<Product>();
            }
        }
        public async Task<decimal> GetTotalStockQuantityAsync()
        {
            try
            {
                var products = await LoadProductDataAsync();
                return products.Sum(p => p.Quantity);
            }
            catch
            {
                return 0;
            }
        }


    }
}
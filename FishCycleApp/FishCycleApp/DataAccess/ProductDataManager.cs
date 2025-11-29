using Npgsql;
using NpgsqlTypes;
using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FishCycleApp.Models;

namespace FishCycleApp.DataAccess
{
    public class ProductDataManager
    {
        private readonly string _connectionString;
        private readonly string _schema;

        public ProductDataManager()
        {
            LoadEnv();

            var host = Environment.GetEnvironmentVariable("DB_HOST") ?? throw new Exception("DB_HOST is missing in .env");
            var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
            var username = Environment.GetEnvironmentVariable("DB_USERNAME") ?? throw new Exception("DB_USERNAME is missing in .env");
            var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? throw new Exception("DB_PASSWORD is missing in .env");
            var database = Environment.GetEnvironmentVariable("DB_DATABASE") ?? "postgres";

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = int.Parse(port),
                Username = username,
                Password = password,
                Database = database,
                KeepAlive = 30,
                Pooling = true
            };

            _connectionString = builder.ToString();
            _schema = "public";
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
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                        continue;

                    var parts = line.Split('=', 2);
                    if (parts.Length != 2) continue;

                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    Environment.SetEnvironmentVariable(key, value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to load .env file. {ex.Message}");
            }
        }

        private string Fn(string name) => $"{_schema}.{name}";

        public async Task<DataTable> LoadProductDataAsync(CancellationToken ct = default)
        {
            var dt = new DataTable();
            string sql = $@"SELECT productid, product_name, grade::text as grade, quantity, unit_price, 
                           (quantity * unit_price) as total_value, supplierid, supplier_name 
                           FROM {Fn("st_select_product")}()";

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct).ConfigureAwait(false);

                await using var cmd = new NpgsqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 20
                };

                await using var rd = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                dt.Load(rd);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Async] Error loading product data: {ex.Message}");
            }
            return dt;
        }

        public async Task<int> InsertProductAsync(Product productData, CancellationToken ct = default)
        {
            return await ExecuteUpsertOrDeleteAsync("st_insert_product", productData, ct);
        }

        public async Task<int> UpdateProductAsync(Product productData, CancellationToken ct = default)
        {
            return await ExecuteUpsertOrDeleteAsync("st_update_product", productData, ct);
        }

        public async Task<int> DeleteProductAsync(string productID, CancellationToken ct = default)
        {
            var dummyProduct = new Product { ProductID = productID };
            return await ExecuteUpsertOrDeleteAsync("st_delete_product", dummyProduct, ct, isDelete: true);
        }

        private async Task<int> ExecuteUpsertOrDeleteAsync(string spName, Product product, CancellationToken ct, bool isDelete = false)
        {
            string sql = isDelete
                ? $"SELECT {Fn(spName)}(@p_id)"
                : $"SELECT {Fn(spName)}(@p_id, @p_name, @p_grade, @p_quantity, @p_unit_price, @p_supplier_id)";

            int result = 0;

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct).ConfigureAwait(false);

                await using var cmd = new NpgsqlCommand(sql, conn) { CommandType = CommandType.Text };

                cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = product.ProductID;

                if (!isDelete)
                {
                    cmd.Parameters.Add("@p_name", NpgsqlDbType.Varchar).Value = product.ProductName ?? "";
                    cmd.Parameters.Add("@p_grade", NpgsqlDbType.Varchar).Value = product.Grade ?? "A";
                    cmd.Parameters.Add("@p_quantity", NpgsqlDbType.Numeric).Value = product.Quantity;
                    cmd.Parameters.Add("@p_unit_price", NpgsqlDbType.Numeric).Value = product.UnitPrice;
                    cmd.Parameters.Add("@p_supplier_id", NpgsqlDbType.Varchar).Value = product.SupplierID ?? (object)DBNull.Value;
                }

                var scalarResult = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                result = ConvertDbResultToInt(scalarResult);

                if (result == 0)
                {
                    await using var verify = new NpgsqlCommand($"select exists(select 1 from {Fn("st_select_product_by_id")}(@p_id))", conn);
                    verify.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = product.ProductID;
                    var existsObj = await verify.ExecuteScalarAsync(ct).ConfigureAwait(false);
                    bool exists = existsObj is bool b && b;

                    if (!isDelete && exists) result = 1;
                    if (isDelete && !exists) result = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Async] {spName} error: {ex.Message}");
            }
            return result;
        }

        public async Task<Product?> GetProductByIDAsync(string productID, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(productID)) return null;

            string sql = $@"SELECT productid, product_name, grade::text as grade, quantity, unit_price, 
                           supplierid, supplier_name 
                           FROM {Fn("st_select_product_by_id")}(@p_id)";

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct).ConfigureAwait(false);

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("p_id", productID.Trim());

                await using var rd = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                if (await rd.ReadAsync(ct).ConfigureAwait(false))
                {
                    return new Product
                    {
                        ProductID = rd["productid"].ToString(),
                        ProductName = rd["product_name"].ToString(),
                        Grade = rd["grade"].ToString(),
                        Quantity = Convert.ToDecimal(rd["quantity"]),
                        UnitPrice = Convert.ToDecimal(rd["unit_price"]),
                        SupplierID = rd["supplierid"] != DBNull.Value ? rd["supplierid"].ToString() : null,
                        SupplierName = rd["supplier_name"] != DBNull.Value ? rd["supplier_name"].ToString() : null
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Async] Get product by ID error: {ex.Message}");
            }
            return null;
        }

        public async Task<DataTable> SearchProductAsync(string searchText, string grade = null, CancellationToken ct = default)
        {
            var dt = new DataTable();
            string sql = $@"SELECT productid, product_name, grade::text as grade, quantity, unit_price, 
                           (quantity * unit_price) as total_value, supplier_name 
                           FROM {Fn("st_search_product")}(@p_search, @p_grade)";

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct).ConfigureAwait(false);

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("p_search", (object)searchText ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_grade", (object)grade ?? DBNull.Value);

                await using var rd = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                dt.Load(rd);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Async] Error searching products: {ex.Message}");
            }
            return dt;
        }

        public async Task<StockStatistics?> GetStockStatisticsAsync(CancellationToken ct = default)
        {
            string sql = $@"SELECT * FROM {Fn("st_get_stock_statistics")}()";

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct).ConfigureAwait(false);

                await using var cmd = new NpgsqlCommand(sql, conn);
                await using var rd = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

                if (await rd.ReadAsync(ct).ConfigureAwait(false))
                {
                    return new StockStatistics
                    {
                        TotalProductTypes = Convert.ToInt64(rd["total_product_types"]),
                        TotalStockQuantity = Convert.ToDecimal(rd["total_stock_quantity"]),
                        TotalStockValue = Convert.ToDecimal(rd["total_stock_value"]),
                        LowStockCount = Convert.ToInt64(rd["low_stock_count"])
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Async] Get stock statistics error: {ex.Message}");
            }
            return null;
        }

        private static int ConvertDbResultToInt(object? scalarResult)
        {
            if (scalarResult == null || scalarResult == DBNull.Value) return 0;
            return scalarResult switch
            {
                int i => i,
                long l => (int)l,
                bool b => b ? 1 : 0,
                decimal d => (int)d,
                short s => s,
                byte by => by,
                string st when int.TryParse(st, out var parsed) => parsed,
                _ => 0
            };
        }
    }
}
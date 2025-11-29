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
    public class TransactionDataManager
    {
        private readonly string _connectionString;
        private readonly string _schema;

        public TransactionDataManager()
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

        // ==========================================
        // LOAD ALL TRANSACTIONS
        // ==========================================
        public async Task<DataTable> LoadTransactionDataAsync(CancellationToken ct = default)
        {
            var dt = new DataTable();
            string sql = $@"SELECT 
                               transactionid as transaction_number,
                               transaction_date as transaction_time,
                               client_name,
                               employee_name,
                               payment_status,
                               delivery_status,
                               total_amount as sub_total
                           FROM {Fn("st_select_transaction")}()";

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
                Console.WriteLine($"[TransactionDataManager] Error loading transaction data: {ex.Message}");
            }
            return dt;
        }

        // ==========================================
        // GET TRANSACTION BY ID
        // ==========================================
        public async Task<Transaction?> GetTransactionByIDAsync(string transactionID, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(transactionID)) return null;

            // FIX: Add client_name, employee_name, delivery_status to SELECT
            string sql = $@"SELECT 
                               transactionid,
                               adminid,
                               clientid,
                               total_amount,
                               transaction_date,
                               payment_status,
                               delivery_status,
                               client_name,
                               employee_name
                           FROM {Fn("st_select_transaction_by_id")}(@p_id)";

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct).ConfigureAwait(false);

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("p_id", transactionID.Trim());

                await using var rd = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                if (await rd.ReadAsync(ct).ConfigureAwait(false))
                {
                    return new Transaction
                    {
                        TransactionID = rd["transactionid"].ToString() ?? "",
                        AdminID = rd["adminid"].ToString() ?? "",
                        ClientID = rd["clientid"].ToString() ?? "",
                        TotalAmount = rd["total_amount"] != DBNull.Value ? Convert.ToDecimal(rd["total_amount"]) : 0,
                        TransactionDate = rd["transaction_date"] != DBNull.Value ? Convert.ToDateTime(rd["transaction_date"]) : DateTime.Now,
                        PaymentStatus = rd["payment_status"]?.ToString() ?? "Unknown",
                        DeliveryStatus = rd["delivery_status"]?.ToString() ?? "Pending",
                        ClientName = rd["client_name"]?.ToString() ?? "Unknown",
                        EmployeeName = rd["employee_name"]?.ToString() ?? "Unknown"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TransactionDataManager] Get transaction by ID error: {ex.Message}");
            }
            return null;
        }

        // ==========================================
        // INSERT TRANSACTION
        // ==========================================
        public async Task<int> InsertTransactionAsync(Transaction transactionData, CancellationToken ct = default)
        {
            return await ExecuteUpsertOrDeleteAsync("st_insert_transaction", transactionData, ct);
        }

        // ==========================================
        // UPDATE TRANSACTION
        // ==========================================
        public async Task<int> UpdateTransactionAsync(Transaction transactionData, CancellationToken ct = default)
        {
            return await ExecuteUpsertOrDeleteAsync("st_update_transaction", transactionData, ct);
        }

        // ==========================================
        // DELETE TRANSACTION
        // ==========================================
        public async Task<int> DeleteTransactionAsync(string transactionID, CancellationToken ct = default)
        {
            var dummyTransaction = new Transaction { TransactionID = transactionID };
            return await ExecuteUpsertOrDeleteAsync("st_delete_transaction", dummyTransaction, ct, isDelete: true);
        }

        // ==========================================
        // PRIVATE HELPER - EXECUTE UPSERT/DELETE
        // ==========================================
        private async Task<int> ExecuteUpsertOrDeleteAsync(string spName, Transaction transaction, CancellationToken ct, bool isDelete = false)
        {
            string sql = isDelete
                ? $"SELECT {Fn(spName)}(@p_id)"
                : $"SELECT {Fn(spName)}(@p_id, @p_admin_id, @p_client_id, @p_total_amount, @p_transaction_date, @p_payment_status)";

            int result = 0;

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct).ConfigureAwait(false);

                await using var cmd = new NpgsqlCommand(sql, conn) { CommandType = CommandType.Text };

                cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = transaction.TransactionID ?? "";

                if (!isDelete)
                {
                    cmd.Parameters.Add("@p_admin_id", NpgsqlDbType.Varchar).Value = transaction.AdminID ?? "";
                    cmd.Parameters.Add("@p_client_id", NpgsqlDbType.Varchar).Value = transaction.ClientID ?? "";
                    cmd.Parameters.Add("@p_total_amount", NpgsqlDbType.Numeric).Value = transaction.TotalAmount;
                    cmd.Parameters.Add("@p_transaction_date", NpgsqlDbType.Timestamp).Value = transaction.TransactionDate;
                    cmd.Parameters.Add("@p_payment_status", NpgsqlDbType.Varchar).Value = transaction.PaymentStatus ?? "Pending";
                }

                var scalarResult = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                result = ConvertDbResultToInt(scalarResult);

                // FIX: Improved verification logic
                if (result == 0)
                {
                    await using var verify = new NpgsqlCommand($"SELECT EXISTS(SELECT 1 FROM {Fn("st_select_transaction_by_id")}(@p_id))", conn);
                    verify.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = transaction.TransactionID;
                    var existsObj = await verify.ExecuteScalarAsync(ct).ConfigureAwait(false);
                    bool exists = existsObj is bool b && b;

                    if (!isDelete && exists) result = 1;
                    if (isDelete && !exists) result = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TransactionDataManager] {spName} error: {ex.Message}");
            }
            return result;
        }

        // ==========================================
        // SEARCH TRANSACTIONS
        // ==========================================
        public async Task<DataTable> SearchTransactionAsync(string searchText, string paymentStatus = null, CancellationToken ct = default)
        {
            var dt = new DataTable();
            string sql = $@"SELECT 
                               transactionid as transaction_number,
                               transaction_date as transaction_time,
                               client_name,
                               employee_name,
                               payment_status,
                               delivery_status,
                               total_amount as sub_total
                           FROM {Fn("st_search_transaction")}(@p_search, @p_payment_status)";

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct).ConfigureAwait(false);

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("p_search", (object?)searchText ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_payment_status", (object?)paymentStatus ?? DBNull.Value);

                await using var rd = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                dt.Load(rd);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TransactionDataManager] Error searching transactions: {ex.Message}");
            }
            return dt;
        }

        // ==========================================
        // HELPER METHOD
        // ==========================================
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
using Npgsql;
using NpgsqlTypes;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using FishCycleApp.Models;

namespace FishCycleApp.DataAccess
{
    public class SupplierDataManager
    {
        private readonly string _connectionString;
        private readonly string _schema;

        public SupplierDataManager()
        {
            // Load env (sama seperti Employee)
            LoadEnv();

            var host = Environment.GetEnvironmentVariable("DB_HOST") ?? throw new Exception("DB_HOST missing");
            var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
            var username = Environment.GetEnvironmentVariable("DB_USERNAME") ?? throw new Exception("DB_USERNAME missing");
            var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? throw new Exception("DB_PASSWORD missing");
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
            _schema = Environment.GetEnvironmentVariable("DB_SCHEMA") ?? "public";
        }

        private void LoadEnv()
        {
            try
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
                if (!System.IO.File.Exists(path))
                {
                    var root = System.IO.Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.Parent?.Parent?.FullName ?? "";
                    path = System.IO.Path.Combine(root, ".env");
                }

                if (!System.IO.File.Exists(path)) return;

                foreach (var line in System.IO.File.ReadAllLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                    var parts = line.Split("=", 2);
                    if (parts.Length != 2) continue;

                    Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
                }
            }
            catch { }
        }

        private string Fn(string name) => $"{_schema}.{name}";

        // ==========================================================
        // LOAD LIST
        // ==========================================================
        public async Task<DataTable> LoadSupplierDataAsync(CancellationToken ct = default)
        {
            var dt = new DataTable();
            string sql = $"select supplierid, supplier_type, supplier_name, supplier_phone, supplier_address from {Fn("st_select_supplier")}()";

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct);

                await using var cmd = new NpgsqlCommand(sql, conn);
                await using var rd = await cmd.ExecuteReaderAsync(ct);

                dt.Load(rd);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Supplier] Load error: {ex.Message}");
            }

            return dt;
        }

        // ==========================================================
        // INSERT
        // ==========================================================
        public async Task<int> InsertSupplierAsync(Supplier s, CancellationToken ct = default)
        {
            string sql = $"select {Fn("st_insert_supplier")}(@p_id, @p_type, @p_name, @p_phone, @p_address)";
            return await ExecuteAsync(sql, s, ct);
        }

        // ==========================================================
        // UPDATE
        // ==========================================================
        public async Task<int> UpdateSupplierAsync(Supplier s, CancellationToken ct = default)
        {
            string sql = $"select {Fn("st_update_supplier")}(@p_id, @p_type, @p_name, @p_phone, @p_address)";
            return await ExecuteAsync(sql, s, ct);
        }

        // ==========================================================
        // DELETE
        // ==========================================================
        public async Task<int> DeleteSupplierAsync(string supplierID, CancellationToken ct = default)
        {
            string sql = $"select {Fn("st_delete_supplier")}(@p_id)";
            var dummy = new Supplier
            {
                SupplierID = supplierID,
                SupplierName = "",
                SupplierType = "",
                SupplierPhone = null,
                SupplierAddress = null
            };

            return await ExecuteAsync(sql, dummy, ct, isDelete: true);
        }

        // ==========================================================
        // GET BY ID
        // ==========================================================
        public async Task<Supplier?> GetSupplierByIDAsync(string supplierID, CancellationToken ct = default)
        {
            Supplier? supplier = null;
            string id = supplierID?.Trim() ?? "";

            string sql = $@"
                select supplierid, supplier_type, supplier_name, supplier_phone, supplier_address
                from {Fn("supplier")}
                where lower(trim(supplierid)) = lower(trim(@p_id))
                limit 1
            ";

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct);

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@p_id", id);

                await using var rd = await cmd.ExecuteReaderAsync(ct);

                if (await rd.ReadAsync(ct))
                    supplier = Map(rd);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Supplier] GetByID error: {ex.Message}");
            }

            return supplier;
        }

        // ==========================================================
        // SHARED EXECUTION FOR INSERT / UPDATE / DELETE
        // ==========================================================
        private async Task<int> ExecuteAsync(string sql, Supplier s, CancellationToken ct, bool isDelete = false)
        {
            int result = 0;

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct);

                await using var cmd = new NpgsqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("@p_id", s.SupplierID);

                if (!isDelete)
                {
                    cmd.Parameters.AddWithValue("@p_type", (object?)s.SupplierType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@p_name", (object?)s.SupplierName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@p_phone", (object?)s.SupplierPhone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@p_address", (object?)s.SupplierAddress ?? DBNull.Value);
                }

                var scalar = await cmd.ExecuteScalarAsync(ct);
                result = ConvertDbInt(scalar);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Supplier] Exec error: {ex.Message}");
            }

            return result;
        }

        private static Supplier Map(IDataRecord rd)
        {
            return new Supplier
            {
                SupplierID = rd["supplierid"].ToString(),
                SupplierType = rd["supplier_type"].ToString(),
                SupplierName = rd["supplier_name"].ToString(),
                SupplierPhone = rd["supplier_phone"] == DBNull.Value ? null : rd["supplier_phone"].ToString(),
                SupplierAddress = rd["supplier_address"] == DBNull.Value ? null : rd["supplier_address"].ToString()
            };
        }

        private static int ConvertDbInt(object? value)
        {
            if (value == null || value == DBNull.Value) return 0;
            return value switch
            {
                int i => i,
                long l => (int)l,
                bool b => b ? 1 : 0,
                _ => 0
            };
        }
    }
}

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
    public class EmployeeDataManager
    {
        private readonly string _connectionString;
        private readonly string _schema;

        public EmployeeDataManager()
        {
            // 1. LOAD FILE .ENV SECARA MANUAL
            // (Agar Environment.GetEnvironmentVariable bisa membaca isi file .env)
            LoadEnv();

            // 2. AMBIL VARIABEL SESUAI NAMA DI FILE .ENV KAMU
            var host = Environment.GetEnvironmentVariable("DB_HOST") ?? throw new Exception("DB_HOST is missing in .env");
            var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
            var username = Environment.GetEnvironmentVariable("DB_USERNAME") ?? throw new Exception("DB_USERNAME is missing in .env");
            var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? throw new Exception("DB_PASSWORD is missing in .env");
            var database = Environment.GetEnvironmentVariable("DB_DATABASE") ?? "postgres";

            // 3. RAKIT MENJADI CONNECTION STRING YANG VALID
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = int.Parse(port),
                Username = username,
                Password = password,
                Database = database,

                // Supaya koneksi tetap hidup (penting untuk Supabase/Cloud DB)
                KeepAlive = 30,
                Pooling = true
            };

            _connectionString = builder.ToString();
            _schema = "public"; // Default schema
        }

        // Helper untuk membaca file .env dan memuatnya ke memori aplikasi
        private void LoadEnv()
        {
            try
            {
                // Mencari file .env di folder tempat aplikasi berjalan (bin/Debug/...)
                // Atau mundur beberapa folder jika berjalan dari Visual Studio
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");

                // Jika tidak ada di folder bin, coba cari di root project (saat development)
                if (!File.Exists(path))
                {
                    string projectRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.Parent?.Parent?.FullName ?? "";
                    path = Path.Combine(projectRoot, ".env");
                }

                if (!File.Exists(path)) return; // Jika file tidak ada, biarkan (mungkin sudah diset di System Environment)

                foreach (var line in File.ReadAllLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                        continue; // Skip baris kosong atau komentar

                    var parts = line.Split('=', 2);
                    if (parts.Length != 2) continue;

                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    // Set ke environment variable process saat ini
                    Environment.SetEnvironmentVariable(key, value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to load .env file. {ex.Message}");
            }
        }

        // Helper function schema
        private string Fn(string name) => $"{_schema}.{name}";

        // ==========================================================
        // METHOD DATA (Sama seperti sebelumnya)
        // ==========================================================

        public async Task<DataTable> LoadEmployeeDataAsync(CancellationToken ct = default)
        {
            var dt = new DataTable();
            // Ganti nama function sesuai database kamu (st_select_employee)
            string sql = $"select employee_id, name, google_account from {Fn("st_select_employee")}()";

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
                Console.WriteLine($"[Async] Error loading employee data: {ex.Message}");
            }
            return dt;
        }

        public async Task<int> InsertEmployeeAsync(Employee employeeData, CancellationToken ct = default)
        {
            return await ExecuteUpsertOrDeleteAsync("st_insert_employee", employeeData, ct);
        }

        public async Task<int> UpdateEmployeeAsync(Employee employeeData, CancellationToken ct = default)
        {
            return await ExecuteUpsertOrDeleteAsync("st_update_employee", employeeData, ct);
        }

        public async Task<int> DeleteEmployeeAsync(string employeeID, CancellationToken ct = default)
        {
            var dummyEmployee = new Employee { EmployeeID = employeeID, EmployeeName = "", GoogleAccount = "" };
            return await ExecuteUpsertOrDeleteAsync("st_delete_employee", dummyEmployee, ct, isDelete: true);
        }

        private async Task<int> ExecuteUpsertOrDeleteAsync(string spName, Employee emp, CancellationToken ct, bool isDelete = false)
        {
            string sql = $"SELECT {Fn(spName)}(@p_id" + (isDelete ? ")" : ", @p_name, @p_ga)");
            int result = 0;

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct).ConfigureAwait(false);

                await using var cmd = new NpgsqlCommand(sql, conn) { CommandType = CommandType.Text };

                cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = emp.EmployeeID;
                if (!isDelete)
                {
                    cmd.Parameters.Add("@p_name", NpgsqlDbType.Varchar).Value = emp.EmployeeName;
                    cmd.Parameters.Add("@p_ga", NpgsqlDbType.Varchar).Value = emp.GoogleAccount;
                }

                var scalarResult = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                result = ConvertDbResultToInt(scalarResult);

                if (result == 0)
                {
                    await using var verify = new NpgsqlCommand($"select exists(select 1 from {Fn("st_select_employee_by_id")}(@p_id))", conn);
                    verify.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = emp.EmployeeID;
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

        public async Task<Employee?> GetEmployeeByIDAsync(string employeeID, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(employeeID)) return null;

            string id = employeeID.Trim();
            string normalizedParam = id
                    .Replace("EMP-", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("EID-", "", StringComparison.OrdinalIgnoreCase)
                    .Trim();

            string tryEmp = "EMP-" + normalizedParam;
            string tryEid = "EID-" + normalizedParam;

            string sql = $@"
                SELECT employee_id, name, google_account 
                FROM {Fn("st_select_employee")}()
                WHERE employee_id = @p_exact 
                   OR employee_id = @p_try_emp 
                   OR employee_id = @p_try_eid
                LIMIT 1";

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct).ConfigureAwait(false);

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("p_exact", id);
                cmd.Parameters.AddWithValue("p_try_emp", tryEmp);
                cmd.Parameters.AddWithValue("p_try_eid", tryEid);

                await using var rd = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                if (await rd.ReadAsync(ct).ConfigureAwait(false))
                {
                    return new Employee
                    {
                        EmployeeID = rd["employee_id"].ToString(),
                        EmployeeName = rd["name"].ToString(),
                        GoogleAccount = rd["google_account"].ToString()
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Async] Get by ID error: {ex.Message}");
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
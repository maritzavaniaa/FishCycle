using Npgsql;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using FishCycleApp.Models;

namespace FishCycleApp.DataAccess
{
    public class ClientDataManager
    {
        private readonly string _connectionString;
        private readonly string _schema;

        public ClientDataManager()
        {
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
                    var root = System.IO.Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)
                        ?.Parent?.Parent?.Parent?.FullName ?? "";
                    path = System.IO.Path.Combine(root, ".env");
                }

                if (!System.IO.File.Exists(path)) return;

                foreach (var line in System.IO.File.ReadAllLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                        Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
                }
            }
            catch { }
        }

        private string Fn(string name) => $"{_schema}.{name}";

        // ==========================================================
        // LOAD LIST
        // ==========================================================
        public async Task<DataTable> LoadClientDataAsync(CancellationToken ct = default)
        {
            var dt = new DataTable();
            string sql = $"select clientid, client_name, client_contact, client_address, client_category from {Fn("st_select_client")}()";

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
                Console.WriteLine($"[Client] Load error: {ex.Message}");
            }

            return dt;
        }

        // ==========================================================
        // INSERT
        // ==========================================================
        public async Task<int> InsertClientAsync(Client c, CancellationToken ct = default)
        {
            string sql = $"select {Fn("st_insert_client")}(@p_id, @p_name, @p_contact, @p_address, @p_category)";
            return await ExecuteAsync(sql, c, ct);
        }

        // ==========================================================
        // UPDATE
        // ==========================================================
        public async Task<int> UpdateClientAsync(Client c, CancellationToken ct = default)
        {
            string sql = $"select {Fn("st_update_client")}(@p_id, @p_name, @p_contact, @p_address, @p_category)";
            return await ExecuteAsync(sql, c, ct);
        }

        // ==========================================================
        // DELETE
        // ==========================================================
        public async Task<int> DeleteClientAsync(string clientID, CancellationToken ct = default)
        {
            string sql = $"select {Fn("st_delete_client")}(@p_id)";

            var dummy = new Client
            {
                ClientID = clientID,
                ClientName = "",
                ClientContact = "",
                ClientAddress = "",
                ClientCategory = ""
            };

            return await ExecuteAsync(sql, dummy, ct, isDelete: true);
        }

        // ==========================================================
        // GET BY ID
        // ==========================================================
        public async Task<Client?> GetClientByIDAsync(string clientID, CancellationToken ct = default)
        {
            Client? client = null;

            string sql = $@"
                select clientid, client_name, client_contact, client_address, client_category
                from {Fn("client")}
                where lower(trim(clientid)) = lower(trim(@p_id))
                limit 1
            ";

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct);

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@p_id", clientID.Trim());

                await using var rd = await cmd.ExecuteReaderAsync(ct);

                if (await rd.ReadAsync(ct))
                {
                    client = Map(rd);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client] GetByID error: {ex.Message}");
            }

            return client;
        }

        // ==========================================================
        // SHARED EXECUTION
        // ==========================================================
        private async Task<int> ExecuteAsync(string sql, Client c, CancellationToken ct, bool isDelete = false)
        {
            int result = 0;

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct);

                await using var cmd = new NpgsqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("@p_id", c.ClientID);

                if (!isDelete)
                {
                    cmd.Parameters.AddWithValue("@p_name", c.ClientName ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@p_contact", c.ClientContact ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@p_address", c.ClientAddress ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@p_category", c.ClientCategory ?? (object)DBNull.Value);
                }

                var scalar = await cmd.ExecuteScalarAsync(ct);
                result = ConvertDbInt(scalar);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client] Exec error: {ex.Message}");
            }

            return result;
        }

        private static Client Map(IDataRecord rd)
        {
            return new Client
            {
                ClientID = rd["clientid"].ToString(),
                ClientName = rd["client_name"].ToString(),
                ClientContact = rd["client_contact"].ToString(),
                ClientAddress = rd["client_address"].ToString(),
                ClientCategory = rd["client_category"].ToString()
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
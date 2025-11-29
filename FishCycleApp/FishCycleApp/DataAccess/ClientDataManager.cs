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
    public class ClientDataManager
    {
        private readonly string _connectionString;
        private readonly string _schema;

        public ClientDataManager()
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
        // LOAD ALL CLIENTS (For ComboBox)
        // ==========================================
        public async Task<DataTable> LoadClientDataAsync(CancellationToken ct = default)
        {
            var dt = new DataTable();
            string sql = $"SELECT clientid, client_name, client_contact, client_address, client_category FROM {Fn("st_select_client")}()";

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
                Console.WriteLine($"[ClientDataManager] Error loading client data: {ex.Message}");
            }
            return dt;
        }

        // ==========================================
        // GET CLIENT BY ID
        // ==========================================
        public async Task<Client?> GetClientByIDAsync(string clientID, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(clientID)) return null;

            string sql = $@"SELECT clientid, client_name, client_contact, client_address, client_category 
                           FROM {Fn("st_select_client")}()
                           WHERE clientid = @p_id
                           LIMIT 1";

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct).ConfigureAwait(false);

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("p_id", clientID.Trim());

                await using var rd = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                if (await rd.ReadAsync(ct).ConfigureAwait(false))
                {
                    return new Client
                    {
                        ClientID = rd["clientid"].ToString() ?? "",
                        ClientName = rd["client_name"].ToString() ?? "",
                        ClientContact = rd["client_contact"]?.ToString(),
                        ClientAddress = rd["client_address"]?.ToString(),
                        ClientCategory = rd["client_category"]?.ToString()
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClientDataManager] Get client by ID error: {ex.Message}");
            }
            return null;
        }

        // ==========================================
        // INSERT CLIENT
        // ==========================================
        public async Task<int> InsertClientAsync(Client clientData, CancellationToken ct = default)
        {
            return await ExecuteUpsertOrDeleteAsync("st_insert_client", clientData, ct);
        }

        // ==========================================
        // UPDATE CLIENT
        // ==========================================
        public async Task<int> UpdateClientAsync(Client clientData, CancellationToken ct = default)
        {
            return await ExecuteUpsertOrDeleteAsync("st_update_client", clientData, ct);
        }

        // ==========================================
        // DELETE CLIENT
        // ==========================================
        public async Task<int> DeleteClientAsync(string clientID, CancellationToken ct = default)
        {
            var dummyClient = new Client { ClientID = clientID };
            return await ExecuteUpsertOrDeleteAsync("st_delete_client", dummyClient, ct, isDelete: true);
        }

        // ==========================================
        // SYNCHRONOUS WRAPPERS
        // ==========================================
        public DataTable LoadClientData()
        {
            return LoadClientDataAsync().GetAwaiter().GetResult();
        }

        public Client? GetClientByID(string clientID)
        {
            return GetClientByIDAsync(clientID).GetAwaiter().GetResult();
        }

        public int InsertClient(Client clientData)
        {
            return InsertClientAsync(clientData).GetAwaiter().GetResult();
        }

        public int UpdateClient(Client clientData)
        {
            return UpdateClientAsync(clientData).GetAwaiter().GetResult();
        }

        public int DeleteClient(string clientID)
        {
            return DeleteClientAsync(clientID).GetAwaiter().GetResult();
        }

        // ==========================================
        // PRIVATE HELPER
        // ==========================================
        private async Task<int> ExecuteUpsertOrDeleteAsync(string spName, Client client, CancellationToken ct, bool isDelete = false)
        {
            string sql = isDelete
                ? $"SELECT {Fn(spName)}(@p_id)"
                : $"SELECT {Fn(spName)}(@p_id, @p_name, @p_contact, @p_address, @p_category)";

            int result = 0;

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct).ConfigureAwait(false);

                await using var cmd = new NpgsqlCommand(sql, conn) { CommandType = CommandType.Text };

                cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = client.ClientID ?? "";

                if (!isDelete)
                {
                    cmd.Parameters.Add("@p_name", NpgsqlDbType.Varchar).Value = client.ClientName ?? "";
                    cmd.Parameters.Add("@p_contact", NpgsqlDbType.Varchar).Value = client.ClientContact ?? (object)DBNull.Value;
                    cmd.Parameters.Add("@p_address", NpgsqlDbType.Varchar).Value = client.ClientAddress ?? (object)DBNull.Value;
                    cmd.Parameters.Add("@p_category", NpgsqlDbType.Varchar).Value = client.ClientCategory ?? "";
                }

                var scalarResult = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                result = ConvertDbResultToInt(scalarResult);

                if (result == 0)
                {
                    await using var verify = new NpgsqlCommand($"SELECT EXISTS(SELECT 1 FROM {Fn("st_select_client")}() WHERE clientid = @p_id)", conn);
                    verify.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = client.ClientID;
                    var existsObj = await verify.ExecuteScalarAsync(ct).ConfigureAwait(false);
                    bool exists = existsObj is bool b && b;

                    if (!isDelete && exists) result = 1;
                    if (isDelete && !exists) result = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClientDataManager] {spName} error: {ex.Message}");
            }
            return result;
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
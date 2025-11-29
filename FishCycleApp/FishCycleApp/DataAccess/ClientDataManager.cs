using Npgsql;
using NpgsqlTypes;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using FishCycleApp.Models;

namespace FishCycleApp.DataAccess
{
    public class ClientDataManager : DatabaseConnection
    {
        private readonly string _schema;

        public ClientDataManager() : base()
        {
            _schema = Environment.GetEnvironmentVariable("DB_SCHEMA") ?? "public";
        }

        private string Fn(string name) => $"{_schema}.{name}";

        public async Task<DataTable> LoadClientDataAsync(CancellationToken ct = default)
        {
            var dt = new DataTable();
            string sql = $"select clientid, client_name, client_contact, client_address, client_category from {Fn("st_select_client")}()";
            try
            {
                await OpenConnectionAsync(ct).ConfigureAwait(false);
                await using var cmd = new NpgsqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 15
                };
                await using var rd = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                dt.Load(rd);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Async] Error loading client data: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"[Async] Inner: {ex.InnerException.Message}");
            }
            finally
            {
                await CloseConnectionAsync().ConfigureAwait(false);
            }
            return dt;
        }

        public async Task<int> InsertClientAsync(Client clientData, CancellationToken ct = default)
        {
            string sql = $"SELECT {Fn("st_insert_client")}(@p_id, @p_name, @p_contact, @p_address, @p_category)";
            int result = 0;
            try
            {
                await OpenConnectionAsync(ct).ConfigureAwait(false);
                await using var cmd = new NpgsqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 15
                };
                cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = clientData.ClientID;
                cmd.Parameters.Add("@p_name", NpgsqlDbType.Varchar).Value = clientData.ClientName;
                cmd.Parameters.Add("@p_contact", NpgsqlDbType.Varchar).Value = clientData.ClientContact;
                cmd.Parameters.Add("@p_address", NpgsqlDbType.Varchar).Value = clientData.ClientAddress;
                cmd.Parameters.Add("@p_category", NpgsqlDbType.Varchar).Value = clientData.ClientCategory;

                var scalarResult = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                result = ConvertDbResultToInt(scalarResult);

                if (result == 0)
                {
                    await using var verify = new NpgsqlCommand(
                        $"select exists(select 1 from {Fn("st_select_client_by_id")}(@p_id))", conn);
                    verify.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = clientData.ClientID;
                    var existsObj = await verify.ExecuteScalarAsync(ct).ConfigureAwait(false);
                    if (existsObj is bool b && b) result = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Async] Insert client error: {ex.Message}");
            }
            finally
            {
                await CloseConnectionAsync().ConfigureAwait(false);
            }
            return result;
        }

        public async Task<int> UpdateClientAsync(Client clientData, CancellationToken ct = default)
        {
            string sql = $"SELECT {Fn("st_update_client")}(@p_id, @p_name, @p_contact, @p_address, @p_category)";
            int result = 0;
            try
            {
                await OpenConnectionAsync(ct).ConfigureAwait(false);
                await using var cmd = new NpgsqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 15
                };
                cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = clientData.ClientID;
                cmd.Parameters.Add("@p_name", NpgsqlDbType.Varchar).Value = clientData.ClientName;
                cmd.Parameters.Add("@p_contact", NpgsqlDbType.Varchar).Value = clientData.ClientContact;
                cmd.Parameters.Add("@p_address", NpgsqlDbType.Varchar).Value = clientData.ClientAddress;
                cmd.Parameters.Add("@p_category", NpgsqlDbType.Varchar).Value = clientData.ClientCategory;

                var scalarResult = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                result = ConvertDbResultToInt(scalarResult);

                if (result == 0)
                {
                    await using var verify = new NpgsqlCommand(
                        $"select exists(select 1 from {Fn("st_select_client_by_id")}(@p_id))", conn);
                    verify.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = clientData.ClientID;
                    var existsObj = await verify.ExecuteScalarAsync(ct).ConfigureAwait(false);
                    if (existsObj is bool b && b) result = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Async] Update client error: {ex.Message}");
            }
            finally
            {
                await CloseConnectionAsync().ConfigureAwait(false);
            }
            return result;
        }

        public async Task<int> DeleteClientAsync(string clientID, CancellationToken ct = default)
        {
            string sql = $"SELECT {Fn("st_delete_client")}(@p_id)";
            int result = 0;
            try
            {
                await OpenConnectionAsync(ct).ConfigureAwait(false);
                await using var cmd = new NpgsqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 15
                };
                cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = clientID;

                var scalarResult = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                result = ConvertDbResultToInt(scalarResult);

                if (result == 0)
                {
                    await using var verify = new NpgsqlCommand(
                        $"select exists(select 1 from {Fn("st_select_client_by_id")}(@p_id))", conn);
                    verify.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = clientID;
                    var existsObj = await verify.ExecuteScalarAsync(ct).ConfigureAwait(false);
                    bool exists = existsObj is bool b && b;
                    if (!exists) result = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Async] Delete client error: {ex.Message}");
            }
            finally
            {
                await CloseConnectionAsync().ConfigureAwait(false);
            }
            return result;
        }

        public DataTable LoadClientData()
        {
            var dt = new DataTable();
            string sql = $"select clientid, client_name, client_contact, client_address, client_category from {Fn("st_select_client")}()";
            try
            {
                OpenConnection();
                using var cmd = new NpgsqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 30
                };
                using var rd = cmd.ExecuteReader();
                dt.Load(rd);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading client data: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
            }
            finally
            {
                CloseConnection();
            }
            return dt;
        }

        public int InsertClient(Client clientData)
        {
            string sql = $"SELECT {Fn("st_insert_client")}(@p_id, @p_name, @p_contact, @p_address, @p_category)";
            int result = 0;
            try
            {
                OpenConnection();
                using var cmd = new NpgsqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 30
                };
                cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = clientData.ClientID;
                cmd.Parameters.Add("@p_name", NpgsqlDbType.Varchar).Value = clientData.ClientName;
                cmd.Parameters.Add("@p_contact", NpgsqlDbType.Varchar).Value = clientData.ClientContact;
                cmd.Parameters.Add("@p_address", NpgsqlDbType.Varchar).Value = clientData.ClientAddress;
                cmd.Parameters.Add("@p_category", NpgsqlDbType.Varchar).Value = clientData.ClientCategory;

                var scalarResult = cmd.ExecuteScalar();
                result = ConvertDbResultToInt(scalarResult);

                if (result == 0)
                {
                    using var verify = new NpgsqlCommand(
                        $"select exists(select 1 from {Fn("st_select_client_by_id")}(@p_id))", conn);
                    verify.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = clientData.ClientID;
                    var existsObj = verify.ExecuteScalar();
                    if (existsObj is bool b && b) result = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Insert client error: {ex.Message}");
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        public int UpdateClient(Client clientData)
        {
            string sql = $"SELECT {Fn("st_update_client")}(@p_id, @p_name, @p_contact, @p_address, @p_category)";
            int result = 0;
            try
            {
                OpenConnection();
                using var cmd = new NpgsqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 30
                };
                cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = clientData.ClientID;
                cmd.Parameters.Add("@p_name", NpgsqlDbType.Varchar).Value = clientData.ClientName;
                cmd.Parameters.Add("@p_contact", NpgsqlDbType.Varchar).Value = clientData.ClientContact;
                cmd.Parameters.Add("@p_address", NpgsqlDbType.Varchar).Value = clientData.ClientAddress;
                cmd.Parameters.Add("@p_category", NpgsqlDbType.Varchar).Value = clientData.ClientCategory;

                var scalarResult = cmd.ExecuteScalar();
                result = ConvertDbResultToInt(scalarResult);

                if (result == 0)
                {
                    using var verify = new NpgsqlCommand(
                        $"select exists(select 1 from {Fn("st_select_client_by_id")}(@p_id))", conn);
                    verify.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = clientData.ClientID;
                    var existsObj = verify.ExecuteScalar();
                    if (existsObj is bool b && b) result = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update client error: {ex.Message}");
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        public int DeleteClient(string clientID)
        {
            string sql = $"SELECT {Fn("st_delete_client")}(@p_id)";
            int result = 0;
            try
            {
                OpenConnection();
                using var cmd = new NpgsqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 30
                };
                cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = clientID;

                var scalarResult = cmd.ExecuteScalar();
                result = ConvertDbResultToInt(scalarResult);

                if (result == 0)
                {
                    using var verify = new NpgsqlCommand(
                        $"select exists(select 1 from {Fn("st_select_client_by_id")}(@p_id))", conn);
                    verify.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = clientID;
                    var existsObj = verify.ExecuteScalar();
                    bool exists = existsObj is bool b && b;
                    if (!exists) result = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Delete client error: {ex.Message}");
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        public Client GetClientByID(string clientID)
        {
            Client client = null;
            string id = (clientID ?? string.Empty).Trim();
            try
            {
                OpenConnection();

                using (var cmd = new NpgsqlCommand($"select * from {Fn("st_select_client_by_id")}(@p_id)", conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = 30;
                    cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = id;

                    using var rd = cmd.ExecuteReader();
                    if (rd.Read())
                    {
                        client = new Client
                        {
                            ClientID = rd["clientid"].ToString(),
                            ClientName = rd["client_name"].ToString(),
                            ClientContact = rd["client_contact"].ToString(),
                            ClientAddress = rd["client_address"].ToString(),
                            ClientCategory = rd["client_category"].ToString()
                        };
                    }
                }
                if (client != null) return client;

                string normalizedParam = id
                    .Replace("CID-", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("CLI-", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("ID-", "", StringComparison.OrdinalIgnoreCase)
                    .Trim();

                string tryCid = id.StartsWith("CID-", StringComparison.OrdinalIgnoreCase) ? id : "CID-" + normalizedParam;
                string tryCli = id.StartsWith("CLI-", StringComparison.OrdinalIgnoreCase) ? id : "CLI-" + normalizedParam;
                string tryId = id.StartsWith("ID-", StringComparison.OrdinalIgnoreCase) ? id : "ID-" + normalizedParam;

                string fallbackSql = $@"
                    with data as (
                        select clientid, client_name, client_contact, client_address, client_category from {Fn("st_select_client")}()
                    )
                    select *
                    from data
                    where trim(clientid) = @p_exact
                       or trim(upper(clientid)) = upper(@p_exact)
                       or trim(clientid) = @p_cid
                       or trim(clientid) = @p_cli
                       or trim(clientid) = @p_idpref
                       or replace(trim(clientid),'CID-','') = @p_norm
                       or replace(trim(clientid),'CLI-','') = @p_norm
                       or replace(trim(clientid),'ID-','')  = @p_norm
                    limit 1";

                using (var fb = new NpgsqlCommand(fallbackSql, conn))
                {
                    fb.CommandType = CommandType.Text;
                    fb.CommandTimeout = 30;
                    fb.Parameters.Add("@p_exact", NpgsqlDbType.Varchar).Value = id;
                    fb.Parameters.Add("@p_cid", NpgsqlDbType.Varchar).Value = tryCid;
                    fb.Parameters.Add("@p_cli", NpgsqlDbType.Varchar).Value = tryCli;
                    fb.Parameters.Add("@p_idpref", NpgsqlDbType.Varchar).Value = tryId;
                    fb.Parameters.Add("@p_norm", NpgsqlDbType.Varchar).Value = normalizedParam;

                    using var rd = fb.ExecuteReader();
                    if (rd.Read())
                    {
                        client = new Client
                        {
                            ClientID = rd["clientid"].ToString(),
                            ClientName = rd["client_name"].ToString(),
                            ClientContact = rd["client_contact"].ToString(),
                            ClientAddress = rd["client_address"].ToString(),
                            ClientCategory = rd["client_category"].ToString()
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get client by ID error: {ex.Message}");
            }
            finally
            {
                CloseConnection();
            }
            return client;
        }

        private async Task OpenConnectionAsync(CancellationToken ct = default)
        {
            if (conn.State == ConnectionState.Closed)
                await conn.OpenAsync(ct).ConfigureAwait(false);
        }

        private async Task CloseConnectionAsync()
        {
            if (conn.State == ConnectionState.Open)
                await conn.CloseAsync().ConfigureAwait(false);
        }

        private static int ConvertDbResultToInt(object? scalarResult)
        {
            if (scalarResult == null || scalarResult == DBNull.Value)
                return 0;
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
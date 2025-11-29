using Npgsql;
using NpgsqlTypes;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using FishCycleApp.Models;

namespace FishCycleApp.DataAccess
{
    public class SupplierDataManager : DatabaseConnection
    {
        private readonly string _schema;

        public SupplierDataManager() : base()
        {
            _schema = Environment.GetEnvironmentVariable("DB_SCHEMA") ?? "public";
        }

        private string Fn(string name) => $"{_schema}.{name}";
        private string Tn(string name) => $"{_schema}.{name}";

        public async Task<DataTable> LoadSupplierDataAsync(CancellationToken ct = default)
        {
            var dt = new DataTable();
            string sql = $"select supplierid, supplier_type, supplier_name, supplier_phone, supplier_address from {Fn("st_select_supplier")}()";
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
                Console.WriteLine($"[Async] Error loading supplier data: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"[Async] Inner: {ex.InnerException.Message}");
            }
            finally
            {
                await CloseConnectionAsync().ConfigureAwait(false);
            }
            return dt;
        }

        public async Task<int> InsertSupplierAsync(Supplier supplier, CancellationToken ct = default)
        {
            string sql = $"SELECT {Fn("st_insert_supplier")}(@p_id, @p_type, @p_name, @p_phone, @p_address)";
            int result = 0;
            try
            {
                await OpenConnectionAsync(ct).ConfigureAwait(false);
                await using var cmd = new NpgsqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 15
                };
                cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = supplier.SupplierID;
                cmd.Parameters.Add("@p_type", NpgsqlDbType.Varchar).Value = supplier.SupplierType;
                cmd.Parameters.Add("@p_name", NpgsqlDbType.Varchar).Value = supplier.SupplierName;
                cmd.Parameters.Add("@p_phone", NpgsqlDbType.Varchar).Value = (object?)supplier.SupplierPhone ?? DBNull.Value;
                cmd.Parameters.Add("@p_address", NpgsqlDbType.Text).Value = (object?)supplier.SupplierAddress ?? DBNull.Value;

                var scalarResult = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                result = ConvertDbResultToInt(scalarResult);

                if (result == 0)
                {
                    string verifySql = $@"
                        select exists(
                          select 1
                          from {Tn("supplier")} s
                          where lower(trim(s.supplierid)) = lower(trim(@p_id))
                             or regexp_replace(lower(trim(s.supplierid)),'^(sid-|sup-|id-)','') =
                                regexp_replace(lower(trim(@p_id)),'^(sid-|sup-|id-)','')
                        )";
                    await using var verify = new NpgsqlCommand(verifySql, conn);
                    verify.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = supplier.SupplierID;
                    var existsObj = await verify.ExecuteScalarAsync(ct).ConfigureAwait(false);
                    if (existsObj is bool b && b) result = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Async] Insert supplier error: {ex.Message}");
            }
            finally
            {
                await CloseConnectionAsync().ConfigureAwait(false);
            }
            return result;
        }

        public async Task<int> UpdateSupplierAsync(Supplier supplier, CancellationToken ct = default)
        {
            string sql = $"SELECT {Fn("st_update_supplier")}(@p_id, @p_type, @p_name, @p_phone, @p_address)";
            int result = 0;
            try
            {
                await OpenConnectionAsync(ct).ConfigureAwait(false);
                await using var cmd = new NpgsqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 15
                };
                cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = supplier.SupplierID;
                cmd.Parameters.Add("@p_type", NpgsqlDbType.Varchar).Value = supplier.SupplierType;
                cmd.Parameters.Add("@p_name", NpgsqlDbType.Varchar).Value = supplier.SupplierName;
                cmd.Parameters.Add("@p_phone", NpgsqlDbType.Varchar).Value = (object?)supplier.SupplierPhone ?? DBNull.Value;
                cmd.Parameters.Add("@p_address", NpgsqlDbType.Text).Value = (object?)supplier.SupplierAddress ?? DBNull.Value;

                var scalarResult = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                result = ConvertDbResultToInt(scalarResult);

                if (result == 0)
                {
                    string verifySql = $@"
                        select exists(
                          select 1
                          from {Tn("supplier")} s
                          where lower(trim(s.supplierid)) = lower(trim(@p_id))
                             or regexp_replace(lower(trim(s.supplierid)),'^(sid-|sup-|id-)','') =
                                regexp_replace(lower(trim(@p_id)),'^(sid-|sup-|id-)','')
                        )";
                    await using var verify = new NpgsqlCommand(verifySql, conn);
                    verify.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = supplier.SupplierID;
                    var existsObj = await verify.ExecuteScalarAsync(ct).ConfigureAwait(false);
                    if (existsObj is bool b && b) result = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Async] Update supplier error: {ex.Message}");
            }
            finally
            {
                await CloseConnectionAsync().ConfigureAwait(false);
            }
            return result;
        }

        public async Task<int> DeleteSupplierAsync(string supplierID, CancellationToken ct = default)
        {
            string sql = $"SELECT {Fn("st_delete_supplier")}(@p_id)";
            int result = 0;
            try
            {
                await OpenConnectionAsync(ct).ConfigureAwait(false);
                await using var cmd = new NpgsqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 15
                };
                cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = supplierID;

                var scalarResult = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                result = ConvertDbResultToInt(scalarResult);

                if (result == 0)
                {
                    string verifySql = $@"
                        select exists(
                          select 1
                          from {Tn("supplier")} s
                          where lower(trim(s.supplierid)) = lower(trim(@p_id))
                             or regexp_replace(lower(trim(s.supplierid)),'^(sid-|sup-|id-)','') =
                                regexp_replace(lower(trim(@p_id)),'^(sid-|sup-|id-)','')
                        )";
                    await using var verify = new NpgsqlCommand(verifySql, conn);
                    verify.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = supplierID;
                    var existsObj = await verify.ExecuteScalarAsync(ct).ConfigureAwait(false);
                    bool exists = existsObj is bool b && b;
                    if (!exists) result = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Async] Delete supplier error: {ex.Message}");
            }
            finally
            {
                await CloseConnectionAsync().ConfigureAwait(false);
            }
            return result;
        }

        public Supplier? GetSupplierByID(string supplierID)
        {
            Supplier? supplier = null;
            string id = (supplierID ?? string.Empty).Trim();

            string sql = $@"
                select s.supplierid, s.supplier_type, s.supplier_name, s.supplier_phone, s.supplier_address
                from {Tn("supplier")} s
                where lower(trim(s.supplierid)) = lower(trim(@p_id))
                   or regexp_replace(lower(trim(s.supplierid)),'^(sid-|sup-|id-)','') =
                      regexp_replace(lower(trim(@p_id)),'^(sid-|sup-|id-)','')
                limit 1";

            try
            {
                OpenConnection();
                using var cmd = new NpgsqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 30
                };
                cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = id;

                using var rd = cmd.ExecuteReader();
                if (rd.Read())
                {
                    supplier = MapSupplier(rd);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get supplier by ID error: {ex.Message}");
            }
            finally
            {
                CloseConnection();
            }
            return supplier;
        }

        public async Task<Supplier?> GetSupplierByIDAsync(string supplierID, CancellationToken ct = default)
        {
            Supplier? supplier = null;
            string id = (supplierID ?? string.Empty).Trim();

            string sql = $@"
                select s.supplierid, s.supplier_type, s.supplier_name, s.supplier_phone, s.supplier_address
                from {Tn("supplier")} s
                where lower(trim(s.supplierid)) = lower(trim(@p_id))
                   or regexp_replace(lower(trim(s.supplierid)),'^(sid-|sup-|id-)','') =
                      regexp_replace(lower(trim(@p_id)),'^(sid-|sup-|id-)','')
                limit 1";

            try
            {
                await OpenConnectionAsync(ct).ConfigureAwait(false);
                await using var cmd = new NpgsqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 15
                };
                cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = id;

                await using var rd = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                if (await rd.ReadAsync(ct).ConfigureAwait(false))
                {
                    supplier = MapSupplier(rd);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Async] Get supplier by ID error: {ex.Message}");
            }
            finally
            {
                await CloseConnectionAsync().ConfigureAwait(false);
            }
            return supplier;
        }

        public DataTable LoadSupplierData()
        {
            var dt = new DataTable();
            string sql = $"select supplierid, supplier_type, supplier_name, supplier_phone, supplier_address from {Fn("st_select_supplier")}()";
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
                Console.WriteLine($"Error loading supplier data: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
            }
            finally
            {
                CloseConnection();
            }
            return dt;
        }

        public int InsertSupplier(Supplier supplier)
        {
            string sql = $"SELECT {Fn("st_insert_supplier")}(@p_id, @p_type, @p_name, @p_phone, @p_address)";
            int result = 0;
            try
            {
                OpenConnection();
                using var cmd = new NpgsqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 30
                };
                cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = supplier.SupplierID;
                cmd.Parameters.Add("@p_type", NpgsqlDbType.Varchar).Value = supplier.SupplierType;
                cmd.Parameters.Add("@p_name", NpgsqlDbType.Varchar).Value = supplier.SupplierName;
                cmd.Parameters.Add("@p_phone", NpgsqlDbType.Varchar).Value = (object?)supplier.SupplierPhone ?? DBNull.Value;
                cmd.Parameters.Add("@p_address", NpgsqlDbType.Text).Value = (object?)supplier.SupplierAddress ?? DBNull.Value;

                var scalarResult = cmd.ExecuteScalar();
                result = ConvertDbResultToInt(scalarResult);

                if (result == 0)
                {
                    string verifySql = $@"
                        select exists(
                          select 1
                          from {Tn("supplier")} s
                          where lower(trim(s.supplierid)) = lower(trim(@p_id))
                             or regexp_replace(lower(trim(s.supplierid)),'^(sid-|sup-|id-)','') =
                                regexp_replace(lower(trim(@p_id)),'^(sid-|sup-|id-)','')
                        )";
                    using var verify = new NpgsqlCommand(verifySql, conn);
                    verify.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = supplier.SupplierID;
                    var existsObj = verify.ExecuteScalar();
                    if (existsObj is bool b && b) result = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Insert supplier error: {ex.Message}");
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        public int UpdateSupplier(Supplier supplier)
        {
            string sql = $"SELECT {Fn("st_update_supplier")}(@p_id, @p_type, @p_name, @p_phone, @p_address)";
            int result = 0;
            try
            {
                OpenConnection();
                using var cmd = new NpgsqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 30
                };
                cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = supplier.SupplierID;
                cmd.Parameters.Add("@p_type", NpgsqlDbType.Varchar).Value = supplier.SupplierType;
                cmd.Parameters.Add("@p_name", NpgsqlDbType.Varchar).Value = supplier.SupplierName;
                cmd.Parameters.Add("@p_phone", NpgsqlDbType.Varchar).Value = (object?)supplier.SupplierPhone ?? DBNull.Value;
                cmd.Parameters.Add("@p_address", NpgsqlDbType.Text).Value = (object?)supplier.SupplierAddress ?? DBNull.Value;

                var scalarResult = cmd.ExecuteScalar();
                result = ConvertDbResultToInt(scalarResult);

                if (result == 0)
                {
                    string verifySql = $@"
                        select exists(
                          select 1
                          from {Tn("supplier")} s
                          where lower(trim(s.supplierid)) = lower(trim(@p_id))
                             or regexp_replace(lower(trim(s.supplierid)),'^(sid-|sup-|id-)','') =
                                regexp_replace(lower(trim(@p_id)),'^(sid-|sup-|id-)','')
                        )";
                    using var verify = new NpgsqlCommand(verifySql, conn);
                    verify.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = supplier.SupplierID;
                    var existsObj = verify.ExecuteScalar();
                    if (existsObj is bool b && b) result = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update supplier error: {ex.Message}");
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        public int DeleteSupplier(string supplierID)
        {
            string sql = $"SELECT {Fn("st_delete_supplier")}(@p_id)";
            int result = 0;
            try
            {
                OpenConnection();
                using var cmd = new NpgsqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 30
                };
                cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = supplierID;

                var scalarResult = cmd.ExecuteScalar();
                result = ConvertDbResultToInt(scalarResult);

                if (result == 0)
                {
                    string verifySql = $@"
                        select exists(
                          select 1
                          from {Tn("supplier")} s
                          where lower(trim(s.supplierid)) = lower(trim(@p_id))
                             or regexp_replace(lower(trim(s.supplierid)),'^(sid-|sup-|id-)','') =
                                regexp_replace(lower(trim(@p_id)),'^(sid-|sup-|id-)','')
                        )";
                    using var verify = new NpgsqlCommand(verifySql, conn);
                    verify.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = supplierID;
                    var existsObj = verify.ExecuteScalar();
                    bool exists = existsObj is bool b && b;
                    if (!exists) result = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Delete supplier error: {ex.Message}");
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        private static Supplier MapSupplier(IDataRecord rd)
        {
            return new Supplier
            {
                SupplierID = rd["supplierid"].ToString() ?? string.Empty,
                SupplierType = rd["supplier_type"].ToString() ?? string.Empty,
                SupplierName = rd["supplier_name"].ToString() ?? string.Empty,
                SupplierPhone = rd["supplier_phone"] == DBNull.Value ? null : rd["supplier_phone"].ToString(),
                SupplierAddress = rd["supplier_address"] == DBNull.Value ? null : rd["supplier_address"].ToString()
            };
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
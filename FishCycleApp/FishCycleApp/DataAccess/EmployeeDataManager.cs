using Npgsql;
using NpgsqlTypes;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using FishCycleApp.Models;

namespace FishCycleApp.DataAccess
{
    public class EmployeeDataManager : DatabaseConnection
    {
        private readonly string _schema;

        public EmployeeDataManager() : base()
        {
            _schema = Environment.GetEnvironmentVariable("DB_SCHEMA") ?? "public";
        }

        private string Fn(string name) => $"{_schema}.{name}";

        public async Task<DataTable> LoadEmployeeDataAsync(CancellationToken ct = default)
        {
            var dt = new DataTable();
            string sql = $"select employee_id, name, google_account from {Fn("st_select_employee")}()";
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
                Console.WriteLine($"[Async] Error loading employee data: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"[Async] Inner: {ex.InnerException.Message}");
            }
            finally
            {
                await CloseConnectionAsync().ConfigureAwait(false);
            }
            return dt;
        }

        public async Task<int> InsertEmployeeAsync(Employee employeeData, CancellationToken ct = default)
        {
            string sql = $"SELECT {Fn("st_insert_employee")}(@p_id, @p_name, @p_ga)";
            int result = 0;
            try
            {
                await OpenConnectionAsync(ct).ConfigureAwait(false);
                await using var cmd = new NpgsqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 15
                };
                cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = employeeData.EmployeeID;
                cmd.Parameters.Add("@p_name", NpgsqlDbType.Varchar).Value = employeeData.EmployeeName;
                cmd.Parameters.Add("@p_ga", NpgsqlDbType.Varchar).Value = employeeData.GoogleAccount;

                var scalarResult = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                result = ConvertDbResultToInt(scalarResult);

                if (result == 0)
                {
                    await using var verify = new NpgsqlCommand(
                        $"select exists(select 1 from {Fn("st_select_employee_by_id")}(@p_id))", conn);
                    verify.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = employeeData.EmployeeID;
                    var existsObj = await verify.ExecuteScalarAsync(ct).ConfigureAwait(false);
                    if (existsObj is bool b && b) result = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Async] Insert error: {ex.Message}");
            }
            finally
            {
                await CloseConnectionAsync().ConfigureAwait(false);
            }
            return result;
        }

        public async Task<int> UpdateEmployeeAsync(Employee employeeData, CancellationToken ct = default)
        {
            string sql = $"SELECT {Fn("st_update_employee")}(@p_id, @p_name, @p_ga)";
            int result = 0;
            try
            {
                await OpenConnectionAsync(ct).ConfigureAwait(false);
                await using var cmd = new NpgsqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 15
                };
                cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = employeeData.EmployeeID;
                cmd.Parameters.Add("@p_name", NpgsqlDbType.Varchar).Value = employeeData.EmployeeName;
                cmd.Parameters.Add("@p_ga", NpgsqlDbType.Varchar).Value = employeeData.GoogleAccount;

                var scalarResult = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                result = ConvertDbResultToInt(scalarResult);

                if (result == 0)
                {
                    await using var verify = new NpgsqlCommand(
                        $"select exists(select 1 from {Fn("st_select_employee_by_id")}(@p_id))", conn);
                    verify.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = employeeData.EmployeeID;
                    var existsObj = await verify.ExecuteScalarAsync(ct).ConfigureAwait(false);
                    if (existsObj is bool b && b) result = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Async] Update error: {ex.Message}");
            }
            finally
            {
                await CloseConnectionAsync().ConfigureAwait(false);
            }
            return result;
        }

        public async Task<int> DeleteEmployeeAsync(string employeeID, CancellationToken ct = default)
        {
            string sql = $"SELECT {Fn("st_delete_employee")}(@p_id)";
            int result = 0;
            try
            {
                await OpenConnectionAsync(ct).ConfigureAwait(false);
                await using var cmd = new NpgsqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 15
                };
                cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = employeeID;

                var scalarResult = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                result = ConvertDbResultToInt(scalarResult);

                if (result == 0)
                {
                    await using var verify = new NpgsqlCommand(
                        $"select exists(select 1 from {Fn("st_select_employee_by_id")}(@p_id))", conn);
                    verify.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = employeeID;
                    var existsObj = await verify.ExecuteScalarAsync(ct).ConfigureAwait(false);
                    bool exists = existsObj is bool b && b;
                    if (!exists) result = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Async] Delete error: {ex.Message}");
            }
            finally
            {
                await CloseConnectionAsync().ConfigureAwait(false);
            }
            return result;
        }

        public async Task<Employee?> GetEmployeeByIDAsync(string employeeID, CancellationToken ct = default)
        {
            Employee? employee = null;
            string id = (employeeID ?? string.Empty).Trim();
            try
            {
                await OpenConnectionAsync(ct).ConfigureAwait(false);

                await using (var cmd = new NpgsqlCommand($"select * from {Fn("st_select_employee_by_id")}(@p_id)", conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = 15;
                    cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = id;

                    await using var rd = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                    if (await rd.ReadAsync(ct).ConfigureAwait(false))
                    {
                        employee = new Employee
                        {
                            EmployeeID = rd["employee_id"].ToString(),
                            EmployeeName = rd["name"].ToString(),
                            GoogleAccount = rd["google_account"].ToString()
                        };
                    }
                }

                if (employee != null) return employee;

                string normalizedParam = id
                    .Replace("EMP-", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("EID-", "", StringComparison.OrdinalIgnoreCase)
                    .Trim();

                string tryEmp = id.StartsWith("EID-", StringComparison.OrdinalIgnoreCase)
                    ? "EMP-" + normalizedParam
                    : id;
                string tryEid = id.StartsWith("EMP-", StringComparison.OrdinalIgnoreCase)
                    ? "EID-" + normalizedParam
                    : id;

                string fallbackSql = $@"
                    with data as (
                        select employee_id, name, google_account from {Fn("st_select_employee")}()
                    )
                    select *
                    from data
                    where trim(employee_id) = @p_exact
                       or trim(employee_id) = @p_try_emp
                       or trim(employee_id) = @p_try_eid
                       or replace(trim(employee_id),'EMP-','') = @p_norm
                       or replace(trim(employee_id),'EID-','') = @p_norm
                    limit 1";

                await using (var fb = new NpgsqlCommand(fallbackSql, conn))
                {
                    fb.CommandType = CommandType.Text;
                    fb.CommandTimeout = 15;
                    fb.Parameters.Add("@p_exact", NpgsqlDbType.Varchar).Value = id;
                    fb.Parameters.Add("@p_try_emp", NpgsqlDbType.Varchar).Value = tryEmp;
                    fb.Parameters.Add("@p_try_eid", NpgsqlDbType.Varchar).Value = tryEid;
                    fb.Parameters.Add("@p_norm", NpgsqlDbType.Varchar).Value = normalizedParam;

                    await using var rd = await fb.ExecuteReaderAsync(ct).ConfigureAwait(false);
                    if (await rd.ReadAsync(ct).ConfigureAwait(false))
                    {
                        employee = new Employee
                        {
                            EmployeeID = rd["employee_id"].ToString(),
                            EmployeeName = rd["name"].ToString(),
                            GoogleAccount = rd["google_account"].ToString()
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Async] Get by ID error: {ex.Message}");
            }
            finally
            {
                await CloseConnectionAsync().ConfigureAwait(false);
            }
            return employee;
        }

        public DataTable LoadEmployeeData()
        {
            DataTable dt = new DataTable();
            string sql = $"select * from {Fn("st_select_employee")}()";
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
                Console.WriteLine($"Error loading employee data: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
            }
            finally
            {
                CloseConnection();
            }
            return dt;
        }

        public int InsertEmployee(Employee employeeData)
        {
            string sql = $"SELECT {Fn("st_insert_employee")}(@p_id, @p_name, @p_ga)";
            int result = 0;
            try
            {
                OpenConnection();
                using var cmd = new NpgsqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 30
                };
                cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = employeeData.EmployeeID;
                cmd.Parameters.Add("@p_name", NpgsqlDbType.Varchar).Value = employeeData.EmployeeName;
                cmd.Parameters.Add("@p_ga", NpgsqlDbType.Varchar).Value = employeeData.GoogleAccount;

                var scalarResult = cmd.ExecuteScalar();
                result = ConvertDbResultToInt(scalarResult);

                if (result == 0)
                {
                    using var verify = new NpgsqlCommand(
                        $"select exists(select 1 from {Fn("st_select_employee_by_id")}(@p_id))", conn);
                    verify.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = employeeData.EmployeeID;
                    var existsObj = verify.ExecuteScalar();
                    if (existsObj is bool b && b) result = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Insert error: {ex.Message}");
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        public int UpdateEmployee(Employee employeeData)
        {
            string sql = $"SELECT {Fn("st_update_employee")}(@p_id, @p_name, @p_ga)";
            int result = 0;
            try
            {
                OpenConnection();
                using var cmd = new NpgsqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 30
                };
                cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = employeeData.EmployeeID;
                cmd.Parameters.Add("@p_name", NpgsqlDbType.Varchar).Value = employeeData.EmployeeName;
                cmd.Parameters.Add("@p_ga", NpgsqlDbType.Varchar).Value = employeeData.GoogleAccount;

                var scalarResult = cmd.ExecuteScalar();
                result = ConvertDbResultToInt(scalarResult);

                if (result == 0)
                {
                    using var verify = new NpgsqlCommand(
                        $"select exists(select 1 from {Fn("st_select_employee_by_id")}(@p_id))", conn);
                    verify.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = employeeData.EmployeeID;
                    var existsObj = verify.ExecuteScalar();
                    if (existsObj is bool b && b) result = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update error: {ex.Message}");
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        public int DeleteEmployee(string employeeID)
        {
            string sql = $"SELECT {Fn("st_delete_employee")}(@p_id)";
            int result = 0;
            try
            {
                OpenConnection();
                using var cmd = new NpgsqlCommand(sql, conn)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 30
                };
                cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = employeeID;

                var scalarResult = cmd.ExecuteScalar();
                result = ConvertDbResultToInt(scalarResult);

                if (result == 0)
                {
                    using var verify = new NpgsqlCommand(
                        $"select exists(select 1 from {Fn("st_select_employee_by_id")}(@p_id))", conn);
                    verify.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = employeeID;
                    var existsObj = verify.ExecuteScalar();
                    bool exists = existsObj is bool b && b;
                    if (!exists) result = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Delete error: {ex.Message}");
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        public Employee? GetEmployeeByID(string employeeID)
        {
            Employee? employee = null;
            string id = (employeeID ?? string.Empty).Trim();
            try
            {
                OpenConnection();

                using (var cmd = new NpgsqlCommand($"select * from {Fn("st_select_employee_by_id")}(@p_id)", conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = 30;
                    cmd.Parameters.Add("@p_id", NpgsqlDbType.Varchar).Value = id;

                    using var rd = cmd.ExecuteReader();
                    if (rd.Read())
                    {
                        employee = new Employee
                        {
                            EmployeeID = rd["employee_id"].ToString(),
                            EmployeeName = rd["name"].ToString(),
                            GoogleAccount = rd["google_account"].ToString()
                        };
                    }
                }
                if (employee != null) return employee;

                string normalizedParam = id
                    .Replace("EMP-", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("EID-", "", StringComparison.OrdinalIgnoreCase)
                    .Trim();

                string tryEmp = id.StartsWith("EID-", StringComparison.OrdinalIgnoreCase)
                    ? "EMP-" + normalizedParam
                    : id;
                string tryEid = id.StartsWith("EMP-", StringComparison.OrdinalIgnoreCase)
                    ? "EID-" + normalizedParam
                    : id;

                string fallbackSql = $@"
                    with data as (select * from {Fn("st_select_employee")}())
                    select *
                    from data
                    where trim(employee_id) = @p_exact
                       or trim(employee_id) = @p_try_emp
                       or trim(employee_id) = @p_try_eid
                       or replace(trim(employee_id),'EMP-','') = @p_norm
                       or replace(trim(employee_id),'EID-','') = @p_norm
                    limit 1";

                using (var fb = new NpgsqlCommand(fallbackSql, conn))
                {
                    fb.CommandType = CommandType.Text;
                    fb.CommandTimeout = 30;
                    fb.Parameters.Add("@p_exact", NpgsqlDbType.Varchar).Value = id;
                    fb.Parameters.Add("@p_try_emp", NpgsqlDbType.Varchar).Value = tryEmp;
                    fb.Parameters.Add("@p_try_eid", NpgsqlDbType.Varchar).Value = tryEid;
                    fb.Parameters.Add("@p_norm", NpgsqlDbType.Varchar).Value = normalizedParam;

                    using var rd = fb.ExecuteReader();
                    if (rd.Read())
                    {
                        employee = new Employee
                        {
                            EmployeeID = rd["employee_id"].ToString(),
                            EmployeeName = rd["name"].ToString(),
                            GoogleAccount = rd["google_account"].ToString()
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get by ID error: {ex.Message}");
            }
            finally
            {
                CloseConnection();
            }
            return employee;
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
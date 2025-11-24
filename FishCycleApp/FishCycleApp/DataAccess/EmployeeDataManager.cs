using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using FishCycleApp.Models;

namespace FishCycleApp.DataAccess
{
    public class EmployeeDataManager : DatabaseConnection
    {
        // Fungsi untuk Load Data Employee
        public DataTable LoadEmployeeData()
        {
            DataTable dt = new DataTable();
            string sql = "select * from st_select_employee()"; // Fungsi SQL untuk SELECT semua Employee

            try
            {
                OpenConnection();
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                {
                    using (NpgsqlDataReader rd = cmd.ExecuteReader())
                    {
                        dt.Load(rd);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading employee data: " + ex.Message,
                    "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CloseConnection();
            }
            return dt;
        }

        // Fungsi untuk Insert Employee Baru
        public int InsertEmployee(Employee employeeData)
        {
            string sql = "select * from st_insert_employee(:_id, :_name, :_google_account)";
            int result = 0;

            try
            {
                OpenConnection();
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.Add(new NpgsqlParameter("_id", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = employeeData.EmployeeID });
                    cmd.Parameters.Add(new NpgsqlParameter("_name", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = employeeData.EmployeeName });
                    cmd.Parameters.Add(new NpgsqlParameter("_google_account", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = employeeData.GoogleAccount });

                    result = (int)cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error inserting employee data: " + ex.Message,
                    "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        // Fungsi untuk Update Data Employee
        public int UpdateEmployee(Employee employeeData)
        {
            string sql = "select * from st_update_employee(:_id, :_name, :_google_account)";
            int result = 0;

            try
            {
                OpenConnection();
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("_id", employeeData.EmployeeID);
                    cmd.Parameters.AddWithValue("_name", employeeData.EmployeeName);
                    cmd.Parameters.AddWithValue("_google_account", employeeData.GoogleAccount);
                    result = (int)cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating employee data: " + ex.Message,
                    "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        // Fungsi untuk Delete Employee berdasarkan ID
        public int DeleteEmployee(string employeeID)
        {
            string sql = "select * from st_delete_employee(:_id)";
            int result = 0;

            try
            {
                OpenConnection();
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("_id", employeeID);
                    result = (int)cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting employee data: " + ex.Message,
                    "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        // Fungsi untuk Get Data Employee berdasarkan ID
        public Employee GetEmployeeByID(string employeeID)
        {
            Employee employee = null;
            string sql = "select * from st_select_employee_by_id(:_id)";

            try
            {
                OpenConnection();
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("_id", employeeID);

                    using (NpgsqlDataReader rd = cmd.ExecuteReader())
                    {
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
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error retrieving employee data: " + ex.Message,
                    "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CloseConnection();
            }
            return employee;
        }
    }
}

using Npgsql;
using NpgsqlTypes;
using System;
using System.Data;
using System.Windows;
using FishCycleApp.Models;

namespace FishCycleApp.DataAccess
{
    public class SupplierDataManager : DatabaseConnection
    {
        // LOAD ALL SUPPLIERS
        public DataTable LoadSupplierData()
        {
            DataTable dt = new DataTable();
            string sql = "select * from st_select_supplier()";

            try
            {
                OpenConnection();
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                using (NpgsqlDataReader rd = cmd.ExecuteReader())
                {
                    dt.Load(rd);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading supplier data: " + ex.Message,
                    "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CloseConnection();
            }

            return dt;
        }

        // INSERT SUPPLIER
        public int InsertSupplier(Supplier supplier)
        {
            string sql = "select * from st_insert_supplier(:_id, :_type, :_name, :_phone, :_address)";
            int result = 0;

            try
            {
                OpenConnection();

                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.Add(new NpgsqlParameter("_id", NpgsqlDbType.Varchar) { Value = supplier.SupplierID });
                    cmd.Parameters.Add(new NpgsqlParameter("_type", NpgsqlDbType.Varchar) { Value = supplier.SupplierType });
                    cmd.Parameters.Add(new NpgsqlParameter("_name", NpgsqlDbType.Varchar) { Value = supplier.SupplierName });
                    cmd.Parameters.Add(new NpgsqlParameter("_phone", NpgsqlDbType.Varchar) { Value = supplier.SupplierPhone });
                    cmd.Parameters.Add(new NpgsqlParameter("_address", NpgsqlDbType.Varchar) { Value = supplier.SupplierAddress });
                    

                    result = (int)cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error inserting supplier: " + ex.Message,
                    "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CloseConnection();
            }

            return result;
        }

        // GET SUPPLIER BY ID
        public Supplier GetSupplierByID(string id)
        {
            Supplier supplier = null;

            string sql = "select * from st_select_supplier_by_id(:_id)";

            try
            {
                OpenConnection();

                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("_id", id);

                    using (NpgsqlDataReader rd = cmd.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            supplier = new Supplier
                            {
                                SupplierID = rd["supplierid"].ToString(),
                                SupplierName = rd["supplier_name"].ToString(),
                                SupplierPhone = rd["supplier_phone"].ToString(),
                                SupplierAddress = rd["supplier_address"].ToString(),
                                SupplierType = rd["supplier_type"].ToString()
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error retrieving supplier: " + ex.Message,
                    "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CloseConnection();
            }

            return supplier;
        }

        // UPDATE SUPPLIER
        public int UpdateSupplier(Supplier supplier)
        {
            string sql = @"
                select * from st_update_supplier(:_id, :_type, :_name, :_phone, :_address)";

            int result = 0;

            try
            {
                OpenConnection();

                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("_id", supplier.SupplierID);
                    cmd.Parameters.AddWithValue("_type", supplier.SupplierType);
                    cmd.Parameters.AddWithValue("_name", supplier.SupplierName);
                    cmd.Parameters.AddWithValue("_phone", supplier.SupplierPhone);
                    cmd.Parameters.AddWithValue("_address", supplier.SupplierAddress);                

                    result = (int)cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating supplier: " + ex.Message,
                    "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CloseConnection();
            }

            return result;
        }

        // DELETE SUPPLIER
        public int DeleteSupplier(string id)
        {
            string sql = "select * from st_delete_supplier(:_id)";
            int result = 0;

            try
            {
                OpenConnection();

                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("_id", id);
                    result = (int)cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting supplier: " + ex.Message,
                    "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CloseConnection();
            }

            return result;
        }
    }
}

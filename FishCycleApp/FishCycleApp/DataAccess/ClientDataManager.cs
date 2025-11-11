using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Windows;
using FishCycleApp.Models;

namespace FishCycleApp.DataAccess
{
    public class ClientDataManager : DatabaseConnection
    {
        public DataTable LoadClientData()
        {
            DataTable dt = new DataTable();
            string sql = "select * from st_select_client()";

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
                MessageBox.Show("Error loading client data: " + ex.Message, 
                    "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CloseConnection();
            }
            return dt;
        }

        public int InsertClient(Client clientData)
        {
            string sql = "select * from st_insert_client(:_id, :_name, :_contact, " +
                ":_address, :_category)";
            int result = 0;

            try
            {
                OpenConnection();
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.Add(new NpgsqlParameter("_id", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = clientData.ClientID });
                    cmd.Parameters.Add(new NpgsqlParameter("_name", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = clientData.ClientName });
                    cmd.Parameters.Add(new NpgsqlParameter("_contact", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = clientData.ClientContact });
                    cmd.Parameters.Add(new NpgsqlParameter("_address", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = clientData.ClientAddress });
                    cmd.Parameters.Add(new NpgsqlParameter("_category", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = clientData.ClientCategory });
                    
                    result = (int)cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error inserting client data: " + ex.Message, 
                    "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        public int UpdateClient(Client clientData)
        {
            string sql = "select * from st_update_client(:_id, :_name, :_contact, " +
                ":_address, :_category)";
            int result = 0;

            try
            {
                OpenConnection();
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("_id", clientData.ClientID);
                    cmd.Parameters.AddWithValue("_name", clientData.ClientName);
                    cmd.Parameters.AddWithValue("_contact", clientData.ClientContact);
                    cmd.Parameters.AddWithValue("_address", clientData.ClientAddress);
                    cmd.Parameters.AddWithValue("_category", clientData.ClientCategory);
                    result = (int)cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating client data: " + ex.Message, 
                    "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        public int DeleteClient(string clientID)
        {
            string sql = "select * from st_delete_client(:_id)";
            int result = 0;

            try
            {
                OpenConnection();
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("_id", clientID);
                    result = (int)cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting client data: " + ex.Message, 
                    "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
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
            string sql = "select * from st_select_client_by_id(:_id)";

            try
            {
                OpenConnection();
                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("_id", clientID);

                    using (NpgsqlDataReader rd = cmd.ExecuteReader())
                    {
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
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error retrieving client data: " + ex.Message, 
                    "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CloseConnection();
            }
            return client;
        }
    }
}
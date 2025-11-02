using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Windows;

namespace FishCycleApp.DataAccess
{
    public class DatabaseConnection
    {
        protected NpgsqlConnection conn;
        private string connstring = "Host=localhost;Port=5432;Username=app_user;Password=app123;Database=FishCycleDB";

        public DatabaseConnection()
        {
            conn = new NpgsqlConnection(connstring);
        }

        public void OpenConnection()
        {
            try
            {
                if (conn.State == ConnectionState.Closed)
                {
                    conn.Open();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening database connection: " + ex.Message, "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CloseConnection()
        {
            if (conn.State == ConnectionState.Open)
            {
                conn.Close();
            }
        }
    }
}
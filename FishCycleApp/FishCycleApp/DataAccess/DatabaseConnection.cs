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
        private string connstring;

        public DatabaseConnection()
        {
            string host = Environment.GetEnvironmentVariable("DB_HOST") ?? "";
            string port = Environment.GetEnvironmentVariable("DB_PORT") ?? "";
            string username = Environment.GetEnvironmentVariable("DB_USERNAME") ?? "";
            string password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";
            string database = Environment.GetEnvironmentVariable("DB_DATABASE") ?? "";

            connstring = $"Host={host};Port={port};Username={username};" +
                        $"Password={password};Database={database}";

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
                MessageBox.Show("Error opening database connection: " + ex.Message, 
                    "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; 
using System.Threading.Tasks;
using FishCycleApp.Models;
using Supabase;

namespace FishCycleApp.DataAccess
{
    public class EmployeeDataManager
    {
        private static Supabase.Client? _supabaseClient;

        public EmployeeDataManager()
        {
        }

        private async Task<Supabase.Client> GetClientAsync()
        {
            if (_supabaseClient != null) return _supabaseClient;

            LoadEnv();

            var url = Environment.GetEnvironmentVariable("SUPABASE_URL")
                      ?? throw new Exception("SUPABASE_URL missing in .env");
            var key = Environment.GetEnvironmentVariable("SUPABASE_KEY")
                      ?? throw new Exception("SUPABASE_KEY missing in .env");

            var options = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true
            };

            _supabaseClient = new Supabase.Client(url, key, options);
            await _supabaseClient.InitializeAsync();

            return _supabaseClient;
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

                    Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Env Error: {ex.Message}");
            }
        }

        public async Task<List<Employee>> LoadEmployeeDataAsync()
        {
            try
            {
                var client = await GetClientAsync();

                var result = await client.From<Employee>().Select("*").Get();

                return result.Models;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading: {ex.Message}");
                return new List<Employee>();
            }
        }

        public async Task<Employee?> GetEmployeeByIDAsync(string employeeID, System.Threading.CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(employeeID)) return null;
            try
            {
                var client = await GetClientAsync();

                var result = await client
                    .From<Employee>()
                    .Select("*")
                    .Where(x => x.EmployeeID == employeeID)
                    .Get(ct);

                return result.Models.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error GetByID: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> InsertEmployeeAsync(Employee emp)
        {
            try
            {
                var client = await GetClientAsync();

                await client.From<Employee>().Insert(emp);
                return true;
            }
            catch (Supabase.Postgrest.Exceptions.PostgrestException pex)
            {
                System.Diagnostics.Debug.WriteLine($"SUPABASE REJECTED: {pex.Message}");

                System.Windows.MessageBox.Show($"Database Error: {pex.Message}", "Supabase Error");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"General Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateEmployeeAsync(Employee emp)
        {
            try
            {
                var client = await GetClientAsync();

                await client
                    .From<Employee>()
                    .Update(emp);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Update: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteEmployeeAsync(string employeeID)
        {
            try
            {
                var client = await GetClientAsync();

                await client
                    .From<Employee>()
                    .Where(x => x.EmployeeID == employeeID)
                    .Delete();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Delete: {ex.Message}");
                return false;
            }
        }
    }
}
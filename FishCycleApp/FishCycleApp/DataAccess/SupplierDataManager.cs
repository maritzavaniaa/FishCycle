using Npgsql;
using NpgsqlTypes;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using FishCycleApp.Models;
using System.Collections.Generic;
using System.Linq;
using FishCycleApp.Models;
using System.IO;

namespace FishCycleApp.DataAccess
{
    public class SupplierDataManager : BaseDataManager
    {
        public async Task<List<Supplier>> LoadSupplierDataAsync()
        {
            try
            {
                var client = await GetClientAsync();
                var result = await client.From<Supplier>().Select("*").Get();
                return result.Models;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Supplier] Load error: {ex.Message}");
                return new List<Supplier>();
            }
        }

        public async Task<bool> InsertSupplierAsync(Supplier s)
        {
            try
            {
                var client = await GetClientAsync();
                await client.From<Supplier>().Insert(s);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Supplier] Insert error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateSupplierAsync(Supplier s)
        {
            try
            {
                var client = await GetClientAsync();
                await client.From<Supplier>().Update(s);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Supplier] Update error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteSupplierAsync(string supplierID)
        {
            try
            {
                var client = await GetClientAsync();
                await client.From<Supplier>().Where(x => x.SupplierID == supplierID).Delete();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Supplier] Delete error: {ex.Message}");
                return false;
            }
        }

        public async Task<Supplier?> GetSupplierByIDAsync(string supplierID)
        {
            try
            {
                var client = await GetClientAsync();
                var result = await client
                    .From<Supplier>()
                    .Select("*")
                    .Where(x => x.SupplierID == supplierID)
                    .Get();

                return result.Models.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishCycleApp.Models
{
    public class Product
    {
        public string? ProductID { get; set; }
        public string? ProductName { get; set; }
        public string? Grade { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue => Quantity * UnitPrice;
        public string? SupplierID { get; set; }
        public string? SupplierName { get; set; }
    }

    public class StockStatistics
    {
        public long TotalProductTypes { get; set; }
        public decimal TotalStockQuantity { get; set; }
        public decimal TotalStockValue { get; set; }
        public long LowStockCount { get; set; }
    }
}
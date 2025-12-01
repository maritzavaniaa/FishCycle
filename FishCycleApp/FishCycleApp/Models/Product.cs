using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using Newtonsoft.Json;

namespace FishCycleApp.Models
{
    [Table("product")]
    public class Product : BaseModel
    {
        [PrimaryKey("productid", true)]
        public string ProductID { get; set; } = string.Empty;

        [Column("product_name")]
        public string ProductName { get; set; } = string.Empty;

        [Column("grade")]
        public string Grade { get; set; } = "A";

        [Column("quantity")]
        public decimal Quantity { get; set; }

        [Column("unit_price")]
        public decimal UnitPrice { get; set; }

        [Column("supplierid")]
        public string? SupplierID { get; set; }

        [JsonIgnore]
        public string SupplierName { get; set; } = "-";

        [JsonIgnore]
        public decimal TotalValue => Quantity * UnitPrice;
    }

    public class StockStatistics
    {
        public long TotalProductTypes { get; set; }
        public decimal TotalStockQuantity { get; set; }
        public decimal TotalStockValue { get; set; }
        public long LowStockCount { get; set; }
    }
}
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
    [Table("transaction_item")]
    public class TransactionItem : BaseModel
    {
        [PrimaryKey("id", false)] 
        public string ID { get; set; }

        [Column("transactionid")]
        public string TransactionID { get; set; }

        [Column("productid")]
        public string ProductID { get; set; }

        [Column("quantity")]
        public decimal Quantity { get; set; }

        [Column("unit_price")]
        public decimal UnitPrice { get; set; }

        [Reference(typeof(Product))]
        public Product? Product { get; set; }

        [JsonIgnore]
        public string ProductName { get; set; } = "Unknown Product";

        [JsonIgnore]
        public string ProductGrade { get; set; } = "-";

        [JsonIgnore]
        public decimal Subtotal => Quantity * UnitPrice;
    }
}
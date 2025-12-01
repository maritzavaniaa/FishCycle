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
    [Table("transaction")]
    public class Transaction : BaseModel
    {
        [PrimaryKey("transactionid", true)]
        public string TransactionID { get; set; } = string.Empty;

        [Column("adminid")]
        public string AdminID { get; set; } = string.Empty;

        [Column("clientid")]
        public string ClientID { get; set; } = string.Empty;

        [Column("total_amount")]
        public decimal TotalAmount { get; set; }

        [Column("transaction_date")]
        public DateTime TransactionDate { get; set; }

        [Column("payment_status")]
        public string PaymentStatus { get; set; } = "Pending";

        [Column("delivery_status")]
        public string? DeliveryStatus { get; set; } = "Pending";

        [JsonIgnore]
        public string ClientName { get; set; } = "-";

        [JsonIgnore]
        public string ClientContact { get; set; } = "-";

        [JsonIgnore]
        public string EmployeeName { get; set; } = "-";
    }

    public class TransactionDetail
    {
        public string ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal => Quantity * UnitPrice;
    }
}
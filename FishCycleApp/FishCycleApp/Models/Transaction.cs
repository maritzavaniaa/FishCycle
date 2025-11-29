using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishCycleApp.Models
{
    public class Transaction
    {
        public string TransactionID { get; set; } = string.Empty;
        public string AdminID { get; set; } = string.Empty;
        public string ClientID { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string PaymentStatus { get; set; } = "Pending";
        public string? DeliveryStatus { get; set; } = "Pending";

        // Additional properties for display purposes
        public string? ClientName { get; set; }
        public string? EmployeeName { get; set; }

        // Transaction details (items in transaction)
        public List<TransactionDetail>? Details { get; set; }
    }

    public class TransactionDetail
    {
        public string TransactionID { get; set; } = string.Empty;
        public string ProductID { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }
}
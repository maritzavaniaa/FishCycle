using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishCycleApp.Models
{
    public class Transaction
    {
        public string TransactionID { get; set; }
        public string AdminID { get; set; }
        public string ClientID { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string PaymentStatus { get; set; }
    }
}
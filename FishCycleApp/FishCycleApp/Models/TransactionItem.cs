using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishCycleApp.Models
{
    public class TransactionItem
    {
        public string TxItemID { get; set; }
        public string TransactionID { get; set; }
        public string ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int CurrentStock { get; set; }
    }
}
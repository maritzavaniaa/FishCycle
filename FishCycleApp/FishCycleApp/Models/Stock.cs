using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishCycleApp.Models
{
    public class Stock
    {
        public string StockID { get; set; }
        public string ProductID { get; set; }
        public string SupplierID { get; set; }
        public string TransactionID { get; set; }
        public DateTime Date { get; set; }
        public int Quantity { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishCycleApp.Models
{
    public class StockOut
    {
        public string StockOutID { get; set; }
        public string ProductID { get; set; }
        public string TransactionID { get; set; }
        public DateTime DateOut { get; set; }
        public int QuantityOut { get; set; }
    }
}
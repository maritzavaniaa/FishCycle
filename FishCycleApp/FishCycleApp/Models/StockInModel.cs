using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishCycleApp.Models
{
    public class StockInModel
    {
        public string StockInID { get; set; }
        public string ProductID { get; set; }
        public string SupplierID { get; set; }

        public DateTime DateIn { get; set; }
        public int QuantityIn { get; set; }
    }
}
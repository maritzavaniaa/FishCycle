using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishCycleApp.Models
{
    public class Stock
    {
        public Guid StockId { get; set; }
        public DateTime Date { get; set; }
        public int StockIn { get; set; }
        public int StockOut { get; set; }

        public bool CheckAvailability()
        {
            return (StockIn - StockOut) > 0;
        }
    }
}

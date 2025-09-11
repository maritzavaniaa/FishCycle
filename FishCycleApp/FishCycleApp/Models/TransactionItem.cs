using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishCycleApp.Models
{
    public class TransactionItem
    {
        public Guid TxItemId { get; set; }
        public int Qty { get; set; }
        public decimal UnitPrice { get; set; }

        // Relasi ke Product
        public Guid ProductId { get; set; }
        public Product Product { get; set; }

        public decimal Subtotal()
        {
            return Qty * UnitPrice;
        }
    }
}

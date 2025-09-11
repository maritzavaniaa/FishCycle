using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishCycleApp.Models
{
    public class Transaction
    {
        public Guid TransactionId { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }

        // Relasi ke Client
        public Guid ClientId { get; set; }
        public Client Client { get; set; }

        // Relasi ke TransactionItem
        public List<TransactionItem> Items { get; set; } = new List<TransactionItem>();

        public void Input(TransactionItem item)
        {
            Items.Add(item);
            RecalculateAmount();
        }

        public TransactionItem Search(Guid itemId)
        {
            return Items.FirstOrDefault(i => i.TxItemId == itemId);
        }

        private void RecalculateAmount()
        {
            Amount = Items.Sum(i => i.Subtotal());
        }
    }
}

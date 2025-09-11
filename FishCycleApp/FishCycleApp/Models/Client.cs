using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishCycleApp.Models
{
    internal class Client
    {
        public Guid ClientId { get; set; }
        public string Name { get; set; }
        public string Contact { get; set; }
        public string Address { get; set; }
        public string Category { get; set; }

        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}

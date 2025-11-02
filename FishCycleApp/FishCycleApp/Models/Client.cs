using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishCycleApp.Models
{
    public class Client
    {
        public string ClientID { get; set; }
        public string ClientName { get; set; }
        public string ClientContact { get; set; }
        public string ClientAddress { get; set; }
        public string ClientCategory { get; set; }

        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}

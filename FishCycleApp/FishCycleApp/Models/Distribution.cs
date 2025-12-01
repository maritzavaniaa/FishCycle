using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishCycleApp.Models
{
    public class Distribution
    {
        public string DistributionID { get; set; }
        public string TransactionID { get; set; }
        public string Method { get; set; }
        public string Status { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
    }
}
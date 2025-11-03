using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishCycleApp.Models
{
    public class Report
    {
        public string ReportID { get; set; }
        public string TransactionID { get; set; }
        public DateTime ReportDate { get; set; }
        public decimal SalesData { get; set; }
        public string ReportType { get; set; }
    }
}
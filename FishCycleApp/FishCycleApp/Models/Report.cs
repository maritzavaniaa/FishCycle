using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishCycleApp.Models
{
    public class Report
    {
        public Guid ReportId { get; set; }
        public DateTime Date { get; set; }
        public string SalesData { get; set; }

        // Relasi ke Transaction
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();

        public string GenerateSalesSummary()
        {
            decimal totalSales = Transactions.Sum(t => t.Amount);
            SalesData = $"Sales Report ({Date.ToShortDateString()}): Total Sales = {totalSales:C}";
            return SalesData;
        }
    }
}

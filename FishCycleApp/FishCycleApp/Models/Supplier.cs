using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishCycleApp.Models
{
    public class Supplier
    {
        public string SupplierID { get; set; } = string.Empty;
        public string SupplierType { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string? SupplierPhone { get; set; }
        public string? SupplierAddress { get; set; }
    }
}
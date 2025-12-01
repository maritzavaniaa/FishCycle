using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace FishCycleApp.Models
{
    [Table("supplier")]
    public class Supplier : BaseModel
    {
        [PrimaryKey("supplierid", true)]
        public string SupplierID { get; set; } = string.Empty;

        [Column("supplier_type")]
        public string SupplierType { get; set; } = string.Empty;

        [Column("supplier_name")]
        public string SupplierName { get; set; } = string.Empty;

        [Column("supplier_phone")]
        public string? SupplierPhone { get; set; }

        [Column("supplier_address")]
        public string? SupplierAddress { get; set; }
    }
}
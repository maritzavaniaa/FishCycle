using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace FishCycleApp.Models
{
    [Table("employee")]
    public class Employee : BaseModel
    {
        [PrimaryKey("employee_id", true)]
        public string EmployeeID { get; set; } = string.Empty;

        [Column("name")]
        public string EmployeeName { get; set; } = string.Empty;

        [Column("google_account")]
        public string? GoogleAccount { get; set; }
    }
}
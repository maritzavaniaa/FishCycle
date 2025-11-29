using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishCycleApp.Models
{
    public class Employee : Account
    {
        // Beri nilai default string.Empty agar tidak null saat objek baru dibuat
        public string EmployeeID { get; set; } = string.Empty;

        public string EmployeeName { get; set; } = string.Empty;

        // Jika Google Account boleh kosong di database, 
        // gunakan tanda tanya (?) untuk menandakan "Nullable"
        public string? GoogleAccount { get; set; }
    }
}
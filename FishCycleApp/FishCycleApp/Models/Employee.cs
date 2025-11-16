using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishCycleApp.Models
{
    public class Employee : Account
    {
        public string EmployeeID { get; set; }
        public string EmployeeName { get; set; }
        public string GoogleAccount { get; set; }
    }
}
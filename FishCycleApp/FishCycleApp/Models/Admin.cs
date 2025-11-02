using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishCycleApp.Models
{
    public class Admin : Account
    {
        public string AdminName { get; set; }
        public string AdminEmail { get; set; }
        public string GoogleID { get; set; }
    }
}
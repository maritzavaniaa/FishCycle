using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishCycleApp.Models
{
    public class Account
    {
        public Guid UserID { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }

        public virtual void Login(string email, string password)
        {
            if (Email == email && Password == password)
                Console.WriteLine("Login success!");
            else
                Console.WriteLine("Login failed!");
        }

        public virtual void Logout()
        {
            Console.WriteLine("User logged out.");
        }
    }
}

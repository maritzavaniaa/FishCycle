using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishCycleApp.Models
{
    public class Product
    {
        public Guid ProductId { get; set; }
        public string Name { get; set; }
        public string Grade { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }

        public List<Stock> StockRecords { get; set; } = new List<Stock>();

        public void AddProduct(int qty)
        {
            Quantity += qty;
        }

        public void RemoveProduct(int qty)
        {
            if (Quantity >= qty)
                Quantity -= qty;
        }

        public void EditProduct(string name, string grade, decimal price)
        {
            Name = name;
            Grade = grade;
            Price = price;
        }
    }
}

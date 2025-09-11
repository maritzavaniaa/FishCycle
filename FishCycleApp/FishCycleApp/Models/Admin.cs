using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FishCycleApp.Models
{
    public class Admin : Account
    {
        public List<Client> Clients { get; set; } = new List<Client>();
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();

        public void InputTransaction(Transaction trx)
        {
            Transactions.Add(trx);
        }

        public void ManageStock(Product product, int stockIn, int stockOut)
        {
            product.Quantity += stockIn - stockOut;
        }

        public void AddClient(Client client)
        {
            Clients.Add(client);
        }

        public void EditClient(Guid clientId, Client updated)
        {
            var client = Clients.Find(c => c.ClientId == clientId);
            if (client != null)
            {
                client.Name = updated.Name;
                client.Contact = updated.Contact;
                client.Address = updated.Address;
                client.Category = updated.Category;
            }
        }

        public void RemoveClient(Guid clientId)
        {
            Clients.RemoveAll(c => c.ClientId == clientId);
        }
    }
}

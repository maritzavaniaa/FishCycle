using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace FishCycleApp.Models
{
    [Table("client")]
    public class Client : BaseModel
    {
        [PrimaryKey("clientid", true)]
        public string ClientID { get; set; } = string.Empty;

        [Column("client_name")]
        public string ClientName { get; set; } = string.Empty;

        [Column("client_contact")]
        public string? ClientContact { get; set; }

        [Column("client_address")]
        public string? ClientAddress { get; set; }

        [Column("client_category")]
        public string ClientCategory { get; set; } = string.Empty;
    }
}
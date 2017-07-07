using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailPoCSimple.Models
{
    public class Item
    {
        public int ID { get; set; } // incremental number
        public string SKU { get; set; } // sample format: 0001, 0002...4999, 5000
        public string Name { get; set; }     
        public string ImgUri { get; set; }
        public ICollection<Attributes> Attributes { get; set; } = new HashSet<Attributes>();
    }
}

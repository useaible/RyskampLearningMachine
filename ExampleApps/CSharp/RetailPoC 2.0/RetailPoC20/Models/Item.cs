using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailPoC.Models
{
    public class Item
    {
        public int ID { get; set; } // incremental number
        public string SKU { get; set; } // sample format: 0001, 0002...4999, 5000
        public string Name { get; set; }
        public int Color { get; set; } //= new System.Windows.Media.Color(); // set R, G, B values - make a way that the colors aren't too close to each other i.e., 255,255,255 and 255,255,254 will be very similar and hard to distinguish so let's avoid this
        public ICollection<Attributes> Attributes { get; set; } = new HashSet<Attributes>();
    }
}

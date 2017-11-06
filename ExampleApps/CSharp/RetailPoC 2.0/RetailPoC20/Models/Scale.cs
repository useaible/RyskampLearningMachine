using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailPoC.Models
{
    public class Scale
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string NameDescription { get { return Name + " - " + Description; } }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailPoC20.Models
{
    public class Attributes
    {
        public int ID { get; set; }
        //public string Name { get; set; }

        // metrics
        // range 0~100
        public double Metric1 { get; set; }
        public double Metric2 { get; set; }
        public double Metric3 { get; set; }
        public double Metric4 { get; set; }
        public double Metric5 { get; set; }
        public double Metric6 { get; set; }
        public double Metric7 { get; set; }
        public double Metric8 { get; set; }
        public double Metric9 { get; set; }
        public double Metric10 { get; set; }

        public ICollection<Item> Items { get; set; } = new HashSet<Item>();
    }
}

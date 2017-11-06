using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoCTools.Settings;

namespace RetailPoCSimple.Models
{
    public class RPOCSimpleSimSettings : SimulationSettings
    {
        // planogram layout info
        public int NumItems { get; set; }
        public int NumShelves { get; set; } // 12 shelves
        public int NumSlots { get; set; } // 24 slots

        // metric % info
        public double Metric1 { get; set; }
        public double Metric2 { get; set; }
        public double Metric3 { get; set; }
        public double Metric4 { get; set; }
        public double Metric5 { get; set; }

        // item metric into
        public double ItemMetricMin { get; set; }
        public double ItemMetricMax { get; set; }

    }
}

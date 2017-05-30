using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailPoC.Models
{
    public class SimulationSettings
    {
        public const int NUM_SCORE_HITS = 10;
        public const int MAX_ITEMS = 10;

        // planogram layout info
        public int NumItems { get; set; }
        public int NumShelves { get; set; } // 12 shelves
        public int NumSlots { get; set; } // 24 slots
        // sim info
        public SimulationType SimType { get; set; } = SimulationType.Score;
        public int? Sessions { get; set; }
        public double? Hours { get; set; }
        public DateTime StartedOn { get; set; }
        public DateTime? EndsOn { get; set; }
        public double? Score { get; set; }
        public bool EnableSimDisplay { get; set; } = true;

        // metric % info
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

        // item metric into
        public double ItemMetricMin { get; set; }
        public double ItemMetricMax { get; set; }
        public double DefaultScorePercentage { get; set; }
    }
}

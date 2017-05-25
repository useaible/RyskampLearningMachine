using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailPoC.Models
{
    public class SimulationData
    {
        public int Session { get; set; }
        public double Score { get; set; }
        public TimeSpan Elapse { get; set; }
        public string Engine { get; set; }
    }
}

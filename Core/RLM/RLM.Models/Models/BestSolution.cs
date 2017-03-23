using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models
{
    public class BestSolution
    {
        public long RneuronId { get; set; }
        public long SolutionId { get; set; }
        public double CycleScore { get; set; }
        public double SessionScore { get; set; }
        public long CycleOrder { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RLM.Models.Interfaces;

namespace RLM.Models.Optimizer
{
    public class ScoringPhase : IRlmFormula
    {
        public string Name { get; set; }
        public List<Constraint> Constraints { get; set; } = new List<Constraint>();
        public IEnumerable<string> Formula { get; set; } = new List<string>();
    }
}

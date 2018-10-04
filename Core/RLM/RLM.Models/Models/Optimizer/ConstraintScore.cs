using RLM.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models.Optimizer
{
    public class ConstraintScore : IRlmFormula
    {
        public string Name { get; set; }
        public IEnumerable<string> Formula { get; set; }
    }
}

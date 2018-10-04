using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RLM.Models.Interfaces;

namespace RLM.Models.Optimizer
{
    public class Constraint : IRlmFormula
    {
        public string Name { get; set; }
        public IEnumerable<string> Formula { get; set; }
        public ConstraintScore SuccessScore { get; set; }
        public ConstraintScore FailScore { get; set; }
        public bool Result { get; set; }
    }    
}

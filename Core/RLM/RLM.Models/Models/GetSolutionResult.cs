using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models
{
    public class GetSolutionResult
    {
        public Solution Solution { get; set; }
        public bool ExistsInCache { get; set; }
    }
}

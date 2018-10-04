using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models.Interfaces
{
    public interface IRlmFormula
    {
        string Name { get; set; }
        IEnumerable<string> Formula { get; set; }
    }
}

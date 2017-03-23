using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models
{
    public class GetRneuronResult
    {
        public Rneuron Rneuron { get; set; }
        public bool ExistsInCache { get; set; }

    }
}

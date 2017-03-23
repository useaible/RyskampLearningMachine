using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.WebAPI.Models
{
    public class CreateLoadNetworkParams : RlmParams
    {
        public CreateLoadNetworkParams()
        {
            Inputs = new List<RlmIOParams>();
            Outputs = new List<RlmIOParams>();
        }

        public ICollection<RlmIOParams> Inputs { get; set; }
        public ICollection<RlmIOParams> Outputs { get; set; }
    }
}

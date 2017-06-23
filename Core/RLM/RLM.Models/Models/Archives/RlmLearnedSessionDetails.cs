using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models
{
    public class RlmLearnedSessionDetails
    {
        public long SessionId { get; set; }
        public IEnumerable<RlmIODetails> Inputs { get; set; }
        public IEnumerable<RlmIODetails> Outputs { get; set; }
        public bool IsCurrent { get; set; } = false;
    }
}

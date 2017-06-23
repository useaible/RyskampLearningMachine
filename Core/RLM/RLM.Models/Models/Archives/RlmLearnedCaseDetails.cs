using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models
{
    public class RlmLearnedCaseDetails
    {
        public long CaseId { get; set; }
        public long CycleNum { get; set; }
        public double CycleScore { get; set; }
        public long SessionId { get; set; }
        public double SessionScore { get; set; }
        public int SessionTime { get; set; }
        public long SessionNum { get; set; }
        public bool IsCurrent { get; set; } = false;

        public IEnumerable<RlmIODetails> Inputs { get; set; }
        public IEnumerable<RlmIODetails> Outputs { get; set; }
    }
}

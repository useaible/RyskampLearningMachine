using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models
{
    public class RlmLearnedCase
    {
        public long CaseId { get; set; }
        public double Score { get; set; }
        public int Time { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models
{
    public class RlmLearnedSession
    {
        public long SessionId { get; set; }
        public double Score { get; set; }
        public int Time { get; set; }
        public bool IsCurrent { get; set; }
        public long SessionNum { get; set; }
    }
}

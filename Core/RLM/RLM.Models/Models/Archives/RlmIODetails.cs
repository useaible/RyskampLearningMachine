using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models
{
    public class RlmIODetails
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsInput { get; set; } = true;

        // used for learning comparison data
        public long CaseId { get; set; }
        public double CycleScore { get; set; }
        public long SessionId { get; set; }
    }
}

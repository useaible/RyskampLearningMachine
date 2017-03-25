using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM
{
    public class RlmCaseHistory
    {
        public long Id { get; set; }
        public DateTime DateTimeStart { get; set; }
        public DateTime DateTimeStop { get; set; }
        public double CycleScore { get; set; }
        public long SessionId { get; set; }
        public long RneuronId { get; set; }
        public long SolutionId { get; set; }
        public TimeSpan Elapse
        {
            get
            {
                return DateTimeStop.Subtract(DateTimeStart);
            }
        }
    }
}

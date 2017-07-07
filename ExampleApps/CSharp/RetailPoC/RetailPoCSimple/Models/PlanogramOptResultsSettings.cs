using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailPoCSimple.Models
{
    public class PlanogramOptResultsSettings : PlanogramOptResults
    {
        public int MaxItems { get; set; }
        public int StartRandomness { get; set; }
        public int EndRandomness { get; set; }
        public int SessionsPerBatch { get; set; }
        public double CurrentRandomnessValue { get; set; }
        public string InputType { get; set; }
    }
}

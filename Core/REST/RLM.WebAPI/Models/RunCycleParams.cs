using RLM;
using RLM.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.WebAPI.Models
{
    public class RunCycleParams : RlmParams
    {
        public RunCycleParams()
        {
            Inputs = new List<RlmIOWithValuesParams>();
            Outputs = new List<RlmIOWithValuesParams>();
        }
        
        [Required()]
        public ICollection<RlmIOWithValuesParams> Inputs { get; set; }

        [Required()]
        public ICollection<RlmIOWithValuesParams> Outputs { get; set; }
        public bool Learn { get; set; }
    }

    public class RlmIOWithValuesParams
    {
        [Required(AllowEmptyStrings = false)]
        public string IOName { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Value { get; set; }
    }

    public class CycleOutputParams
    {
        public CycleOutputParams()
        {
            Outputs = new List<RlmIOWithValuesParams>();
        }

        public RlmNetworkType RlmType { get; set; }        
        public long CycleId { get; set; }
        public ICollection<RlmIOWithValuesParams> Outputs { get; set; }
    }    
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.WebAPI.Models
{
    public class ScoreCycleParams : RlmParams
    {
        [Required()]
        public long CycleID { get; set; }

        [Required()]
        public double Score { get; set; }

        //public dynamic LunarLanderSimulator { get; set; }
    }
}

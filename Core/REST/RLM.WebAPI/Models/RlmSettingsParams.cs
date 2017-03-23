using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.WebAPI.Models
{
    public class RlmSettingsParams : RlmParams
    {
        [Required()]
        public int NumSessions { get; set; }

        [Required()]
        public int StartRandomness { get; set; }

        [Required()]
        public int EndRandomness { get; set; }

        public int MaxLinearBracket { get; set; }

        public int MinLinearBracket { get; set; }
    }
}

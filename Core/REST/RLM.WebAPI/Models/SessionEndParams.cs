using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.WebAPI.Models
{
    public class SessionEndParams : RlmParams
    {
        [Required()]
        public double SessionScore { get; set; }
    }
}

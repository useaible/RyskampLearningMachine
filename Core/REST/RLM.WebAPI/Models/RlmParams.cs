using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RLM.WebAPI.Models
{
    public class RlmParams
    {
        [Required(AllowEmptyStrings = false)]
        public string RlmName { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string NetworkName { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RLM.WebAPI.Models
{
    public class RlmIOParams
    {
        [Required(AllowEmptyStrings = false)]
        public string IOName { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string DotNetType { get; set; }

        [Required()]
        public double Min { get; set; }

        [Required()]
        public double Max { get; set; }

        public int InputType { get; set; }
    }
}
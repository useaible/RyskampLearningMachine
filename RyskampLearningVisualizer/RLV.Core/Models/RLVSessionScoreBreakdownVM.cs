using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLV.Core.Models
{
    public class RLVSessionScoreBreakdownVM
    {
        public string Name { get; set; }
        public double Value { get; set; }
        public string ValueStr { get { return Value.ToString("#,###.##"); } }
    }
}

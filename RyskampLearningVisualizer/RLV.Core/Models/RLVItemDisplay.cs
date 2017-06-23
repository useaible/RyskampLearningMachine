using RLV.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RLV.Core.Enums;

namespace RLV.Core.Models
{
    public class RLVItemDisplay : IRLVItemDisplay
    {
        public string Name { get; set; }

        public object Value { get; set; }

        public RLVVisibility Visibility { get; set; }
    }
}

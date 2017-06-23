using RLV.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLV.Core.Models
{
    public class RLVIOValues : IRLVIOValues
    {
        public string IOName { get; set; }
        public string Value { get; set; }
    }
}

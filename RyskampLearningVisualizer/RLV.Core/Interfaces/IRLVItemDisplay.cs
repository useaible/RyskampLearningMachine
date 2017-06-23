using RLV.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLV.Core.Interfaces
{
    public interface IRLVItemDisplay
    {
        string Name { get; set; }
        object Value { get; set; }
        RLVVisibility Visibility { get; set; }
    }
}

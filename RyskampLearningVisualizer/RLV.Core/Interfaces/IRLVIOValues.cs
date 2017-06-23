using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLV.Core.Interfaces
{
    public interface IRLVIOValues
    {
        string IOName { get; set; }
        string Value { get; set; }
    }
}

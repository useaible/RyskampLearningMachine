using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Enums
{
    public enum RlmInputDataType
    {
        [RlmEnumDescription("String", typeof(System.String))]
        String,
        [RlmEnumDescription("Integer (16-bit)", typeof(System.Int16))]
        SmallInt,
        [RlmEnumDescription("Integer (32-bit)", typeof(System.Int32))]
        Int,
        [RlmEnumDescription("Integer (64-bit)", typeof(System.Int64))]
        BigInt,
        [RlmEnumDescription("Floating point", typeof(System.Double))]
        Double,
        [RlmEnumDescription("Boolean", typeof(System.Boolean))]
        Boolean
    }
}

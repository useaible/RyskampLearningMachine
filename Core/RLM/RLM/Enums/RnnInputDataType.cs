using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RNN.Enums
{
    public enum RnnInputDataType
    {
        [RnnEnumDescription("String", typeof(System.String))]
        String,
        [RnnEnumDescription("Integer (16-bit)", typeof(System.Int16))]
        SmallInt,
        [RnnEnumDescription("Integer (32-bit)", typeof(System.Int32))]
        Int,
        [RnnEnumDescription("Integer (64-bit)", typeof(System.Int64))]
        BigInt,
        [RnnEnumDescription("Floating point", typeof(System.Double))]
        Double,
        [RnnEnumDescription("Boolean", typeof(System.Boolean))]
        Boolean
    }
}

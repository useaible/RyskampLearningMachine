using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RNN.Enums
{
    public class RnnEnumDescriptionAttribute : DescriptionAttribute
    {
        public Type SystemType { get; private set; }

        public RnnEnumDescriptionAttribute(string description, Type systemType)
            : base(description)
        {
            SystemType = systemType;
        }
    }
}

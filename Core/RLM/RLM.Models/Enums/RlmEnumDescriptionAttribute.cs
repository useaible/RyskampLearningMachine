using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Enums
{
    public class RlmEnumDescriptionAttribute : DescriptionAttribute
    {
        public Type SystemType { get; private set; }

        public RlmEnumDescriptionAttribute(string description, Type systemType)
            : base(description)
        {
            SystemType = systemType;
        }
    }
}

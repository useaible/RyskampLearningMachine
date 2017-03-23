using RLM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Memory
{
    public class DynamicInputComparer : IEqualityComparer<RneuronInputSet>
    {
        public bool Equals(RneuronInputSet x, RneuronInputSet y)
        {
            return x.RneuronId == y.RneuronId;
        }

        public int GetHashCode(RneuronInputSet obj)
        {
            return obj == null ? 0 : obj.RneuronId.GetHashCode();
        }
    }
}

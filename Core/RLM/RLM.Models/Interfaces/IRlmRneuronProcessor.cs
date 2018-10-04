using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models.Interfaces
{
    public interface IRlmRneuronProcessor
    {
        void Execute(long[] rneurons, double[][] inputs, double[] from, double[] to, bool distinct);
        RlmCacheDataArray Execute(long[] rneurons, double[][] inputs, double[] from, double[] to, bool[] rneuronsCache, double[] fromCache, double[] toCache);
        void SetManagerReference(IManager mgr);
    }
}

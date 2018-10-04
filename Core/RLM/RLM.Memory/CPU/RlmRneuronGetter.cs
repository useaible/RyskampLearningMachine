using RLM.Models;
using RLM.Models.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Memory.CPU
{
    internal class RlmRneuronGetter : IRlmRneuronProcessor
    {
        public RlmRneuronGetter(IManager mgr)
        {
            SetManagerReference(mgr);
        }

        protected IManager ManagerReference { get; private set; }

        public void SetManagerReference(IManager mgr)
        {
            ManagerReference = mgr;
        }

        public void Execute(long[] rneurons, double[][] inputs, double[] from, double[] to, bool distinct)
        {
            Parallel.For(0, rneurons.Length, (r, loopState) =>
            {
                bool isMatch = true;
                for (int i = 0; i < inputs.Length; i++)
                {
                    if (!(inputs[i][r] >= from[i] && inputs[i][r] <= to[i]))
                    {
                        isMatch = false;
                        break;
                    }
                }

                if (isMatch)
                {
                    var rneuronId = rneurons[r];
                    ManagerReference.FindBestSolution(rneuronId);                    

                    if (distinct)
                        loopState.Stop();
                }
            });
        }

        public RlmCacheDataArray Execute(long[] rneurons, double[][] inputs, double[] from, double[] to, bool[] rneuronsCache, double[] fromCache, double[] toCache)
        {
            RlmCacheDataArray retVal = new RlmCacheDataArray(inputs.Length);

            Parallel.For(0, rneurons.Length, (r) =>
            {
                bool isMatch = true;
                bool isMatchCache = true;
                for (int i = 0; i < inputs.Length; i++)
                {
                    if (!(inputs[i][r] >= from[i] && inputs[i][r] <= to[i]))
                    {
                        isMatch = false;
                    }

                    if (!(inputs[i][r] >= fromCache[i] && inputs[i][r] <= toCache[i]))
                    {
                        isMatchCache = false;
                    }

                    if (!isMatch && !isMatchCache)
                        break;
                }

                if (isMatch)
                {
                    var rneuronId = rneurons[r];
                    ManagerReference.FindBestSolution(rneuronId);
                }

                if (isMatchCache)
                {
                    double[] values = new double[inputs.Length];
                    for (int j = 0; j < inputs.Length; j++)
                    {
                        values[j] = inputs[j][r];
                    }
                    retVal.Add(rneurons[r], values);
                }
            });
            
            return retVal;
        }
    }
}

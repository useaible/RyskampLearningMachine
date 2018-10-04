using Alea;
using RLM.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.GPU
{
    internal class RlmAleaGpuCopyPartial : RlmAleaGpuCopyAll
    {
        public RlmAleaGpuCopyPartial(int length) : base(length)
        {

        }

        protected override void Allocate(double[][] inputs, long[] rneurons, double[] from, double[] to)
        {
            rneuronsArr = gpu.Allocate(rneurons);
            resultsArr = gpu.Allocate<long>(rneurons.Length);
            inputsArr = gpu.Allocate(inputs);

            Gpu.Copy(from, fromArr);
            Gpu.Copy(to, toArr);
        }

        protected override void AllocateCache(double[][] inputs, long[] rneurons, double[] from, double[] to, bool[] rneuronsCache, double[] fromCache, double[] toCache)
        {
            Allocate(inputs, rneurons, from, to);

            rneuronsCacheArr = gpu.Allocate(rneuronsCache);

            Gpu.Copy(fromCache, fromCacheArr);
            Gpu.Copy(toCache, toCacheArr);
        }
    }
}

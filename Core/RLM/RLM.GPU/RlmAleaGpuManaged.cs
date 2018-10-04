using Alea;
using Alea.CSharp;
using RLM.Models;
using RLM.Models.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.GPU
{
    public class RlmAleaGpuManaged : RlmAleaGpu
    {     
        public RlmAleaGpuManaged()
        {

        }

        [GpuManaged]
        protected override void LaunchKernel(long[] rneurons, double[][] inputs, double[] from, double[] to, int lparam1, int lparam2)
        {
            var resultArr = new long[rneurons.Length];
            var lp = new LaunchParam(lparam1, lparam2);

            gpu.Launch(RlmAleaGpu.Kernel, lp, rneurons, inputs, resultArr, from, to);

            FindBestSolution(resultArr);
        }

        [GpuManaged]
        protected override RlmCacheDataArray LaunchKernel(long[] rneurons, double[][] inputs, double[] from, double[] to, bool[] rneuronsCache, double[] fromCache, double[] toCache, int lparam1, int lparam2)
        {
            var resultArr = new long[rneurons.Length];
            var lp = new LaunchParam(lparam1, lparam2);

            gpu.Launch(RlmAleaGpu.KernelCache, lp, rneurons, inputs, resultArr, from, to, rneuronsCache, fromCache, toCache);

            return FindBestSolutionAndBuildCache(rneurons, resultArr, inputs, rneuronsCache);
        }
    }
}

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
    internal class RlmAleaGpuCopyAll : RlmAleaGpu, IDisposable
    {
        protected long[] rneuronsArr;
        protected long[] resultsArr;
        protected double[][] inputsArr;
        protected double[] fromArr;
        protected double[] toArr;

        protected bool[] rneuronsCacheArr;
        protected double[] fromCacheArr;
        protected double[] toCacheArr;

        public RlmAleaGpuCopyAll(int length)
        {
            fromArr = gpu.Allocate<double>(length);
            toArr = gpu.Allocate<double>(length);
            fromCacheArr = gpu.Allocate<double>(length);
            toCacheArr = gpu.Allocate<double>(length);
        }
        
        public void Dispose()
        {
            Gpu.Free(fromArr);
            Gpu.Free(toArr);
            Gpu.Free(fromCacheArr);
            Gpu.Free(toCacheArr);
        }

        protected virtual void Allocate(double[][] inputs, long[] rneurons, double[] from, double[] to)
        {
            rneuronsArr = gpu.Allocate<long>(rneurons.Length);
            resultsArr = gpu.Allocate<long>(rneurons.Length);
            inputsArr = gpu.Allocate<double[]>(inputs.Length);

            Gpu.Copy(rneurons, rneuronsArr);
            Gpu.Copy(inputs, inputsArr);
            Gpu.Copy(from, fromArr);
            Gpu.Copy(to, toArr);
        }

        protected virtual void AllocateCache(double[][] inputs, long[] rneurons, double[] from, double[] to, bool[] rneuronsCache, double[] fromCache, double[] toCache)
        {
            Allocate(inputs, rneurons, from, to);

            rneuronsCacheArr = gpu.Allocate<bool>(rneuronsCache.Length);

            Gpu.Copy(rneuronsCache, rneuronsCacheArr);
            Gpu.Copy(fromCache, fromCacheArr);
            Gpu.Copy(toCache, toCacheArr);
        }

        protected virtual void Free()
        {
            Gpu.Free(rneuronsArr);
            Gpu.Free(resultsArr);
            Gpu.Free(inputsArr);
        }

        protected virtual void FreeCache()
        {
            Free();
            Gpu.Free(rneuronsCacheArr);
        }

        protected override void LaunchKernel(long[] rneurons, double[][] inputs, double[] from, double[] to, int lparam1, int lparam2)
        {
            var results = new long[rneurons.Length];
            var lp = new LaunchParam(lparam1, lparam2);

            Allocate(inputs, rneurons, from, to);

            gpu.Launch(RlmAleaGpu.Kernel, lp, rneuronsArr, inputsArr, resultsArr, fromArr, toArr);
            Gpu.Copy(resultsArr, results);

            Free();

            FindBestSolution(results);
        }

        protected override RlmCacheDataArray LaunchKernel(long[] rneurons, double[][] inputs, double[] from, double[] to, bool[] rneuronsCache, double[] fromCache, double[] toCache, int lparam1, int lparam2)
        {
            var results = new long[rneurons.Length];
            var lp = new LaunchParam(lparam1, lparam2);

            AllocateCache(inputs, rneurons, from, to, rneuronsCache, fromCache, toCache);

            gpu.Launch(RlmAleaGpu.KernelCache, lp, rneuronsArr, inputsArr, resultsArr, fromArr, toArr, rneuronsCacheArr, fromCacheArr, toCacheArr);
            Gpu.Copy(resultsArr, results);
            Gpu.Copy(rneuronsCacheArr, rneuronsCache);

            FreeCache();

            return FindBestSolutionAndBuildCache(rneurons, results, inputs, rneuronsCache);
        }
    }
}

using Alea;
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
    public abstract class RlmAleaGpu : IRlmRneuronProcessor
    {
        #region Static members
        public static void Kernel(long[] rneurons, double[][] inputs, long[] results, double[] from, double[] to)
        {
            var start = blockIdx.x * blockDim.x + threadIdx.x;
            var stride = gridDim.x * blockDim.x;

            for (int r = start; r < rneurons.Length; r += stride)
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
                    results[r] = rneurons[r];
                }
            }
        }

        public static void KernelCache(long[] rneurons, double[][] inputs, long[] results, double[] from, double[] to, bool[] rneuronsCache, double[] fromCache, double[] toCache)
        {
            var start = blockIdx.x * blockDim.x + threadIdx.x;
            var stride = gridDim.x * blockDim.x;

            for (int r = start; r < rneurons.Length; r += stride)
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
                    results[r] = rneurons[r];
                }

                if (isMatchCache)
                {
                    rneuronsCache[r] = true;
                }
            }
        }
        #endregion  

        public RlmAleaGpu()
        {
        }

        protected Gpu gpu = Gpu.Default;

        protected IManager ManagerReference { get; private set; }

        public void SetManagerReference(IManager mgr)
        {
            ManagerReference = mgr;
        }

        protected abstract void LaunchKernel(long[] rneurons, double[][] inputs, double[] from, double[] to, int lparam1, int lparam2);
        protected abstract RlmCacheDataArray LaunchKernel(long[] rneurons, double[][] inputs, double[] from, double[] to, bool[] rneuronsCache, double[] fromCache, double[] toCache, int lparam1, int lparam2);
        public virtual void Execute(long[] rneurons, double[][] inputs, double[] fromArr, double[] toArr, bool distinct)
        {
            int lparam1, lparam2;
            GetGpuParams(rneurons.Length, out lparam1, out lparam2);

            LaunchKernel(
                rneurons,
                inputs,
                fromArr,
                toArr,
                lparam1,
                lparam2);
        }


        public virtual RlmCacheDataArray Execute(long[] rneurons, double[][] inputs, double[] fromArr, double[] toArr, bool[] rneuronsCache, double[] fromCache, double[] toCache)
        {
            int lparam1, lparam2;
            GetGpuParams(rneurons.Length, out lparam1, out lparam2);

            var rneuronIdsGpuKernel = LaunchKernel(
                rneurons,
                inputs,
                fromArr,
                toArr,
                rneuronsCache,
                fromCache,
                toCache,
                lparam1,
                lparam2);

            return rneuronIdsGpuKernel;
        }

        public virtual void FindBestSolution(long[] arr)
        {
            Parallel.For(0, arr.Length, (i) =>
            {
                if (arr[i] != 0)
                {
                    ManagerReference.FindBestSolution(arr[i]);
                }
            });
        }

        public virtual RlmCacheDataArray FindBestSolutionAndBuildCache(long[] rneurons, long[] results, double[][] inputs, bool[] rneuronsCache)
        {
            RlmCacheDataArray cachedData = new RlmCacheDataArray(inputs.Length);

            Parallel.For(0, results.Length, (i) =>
            {
                if (results[i] != 0)
                {
                    ManagerReference.FindBestSolution(results[i]);
                }

                if (rneuronsCache[i])
                {
                    double[] values = new double[inputs.Length];
                    for (int j = 0; j < inputs.Length; j++)
                    {
                        values[j] = inputs[j][i];
                    }
                    cachedData.Add(rneurons[i], values);
                }
            });

            return cachedData;
        }

        protected virtual void GetGpuParams(int length, out int lparam1, out int lparam2)
        {
            lparam1 = 32;
            lparam2 = 512;

            if (length > 10000 && length <= 100000)
            {
                lparam1 = 20;
                lparam2 = 256;
            }
            else if (length > 100000)
            {
                lparam1 = 32;
                lparam2 = 512;
            }
        }
    }
}

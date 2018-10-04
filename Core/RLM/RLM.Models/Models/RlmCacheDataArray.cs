using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models
{
    public class RlmCacheDataArray
    {
        public RlmCacheDataArray(int inputLength)
        {
            Rneurons = new ConcurrentBag<long>();
            Inputs = new ConcurrentBag<double>[inputLength];

            for (int i = 0; i < inputLength; i++)
            {
                Inputs[i] = new ConcurrentBag<double>();
            }
        }

        public ConcurrentBag<long> Rneurons { get; set; }
        public ConcurrentBag<double>[] Inputs { get; set; }
        public ConcurrentBag<long> Results { get; set; }

        //private object lockObj = new object();

        public void Add(long rneuronId, double[] inputs)
        {
            Rneurons.Add(rneuronId);
            for (int i = 0; i < Inputs.Length; i++)
            {
                Inputs[i].Add(inputs[i]);
            }
        }
    }
}

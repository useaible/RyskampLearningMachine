using System;
using System.Collections.Generic;
using System.Data.HashFunction;
using System.Data.HashFunction.xxHash;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Memory
{
    public static class Util
    {
        static Util()
        {
            var factory = xxHashFactory.Instance;
            _xxHash64 = factory.Create(new xxHashConfig() { HashSizeInBits = 64 });
        }


        public static Random Randomizer
        {
            get
            {
                Random randomizer = new Random(Guid.NewGuid().GetHashCode());
                return randomizer;
            }
        }

        public static double GetRandomDoubleNumber(double min, double max)
        {
            var next = Randomizer.NextDouble();
            return min + (next * (max - min));
        }

        private static readonly IxxHash _xxHash64;
        public static IxxHash xxHash64
        {
            get
            {
                return _xxHash64;
            }
        }

        private static readonly DynamicInputComparer dynaInputComp = new DynamicInputComparer();
        public static DynamicInputComparer DynamicInputComparer
        {
            get
            {
                return dynaInputComp;
            }
        }

        public static long GenerateHashKey(params object[] values)
        {
            string aggregatedValues = string.Join("_", values);

            var hashValue = xxHash64.ComputeHash(aggregatedValues);
            return BitConverter.ToInt64(hashValue.Hash, 0);
        }

        public static double NextDouble(this Random rand, double minVal, double maxVal)
        {
            return rand.NextDouble() * (maxVal - minVal) + minVal;
        }

        public static decimal NextDecimal(this Random rand, decimal minVal, decimal maxVal)
        {
            return Convert.ToDecimal(rand.NextDouble()) * (maxVal - minVal) + minVal;
        }
    }
}
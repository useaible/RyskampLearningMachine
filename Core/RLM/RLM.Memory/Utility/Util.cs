using System;
using System.Collections.Generic;
using System.Data.HashFunction;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Memory
{
    public static class Util
    {
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

        private static readonly xxHash _xxHash64 = new xxHash(64);
        public static xxHash xxHash64
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
            byte[] hashedKey = xxHash64.ComputeHash(aggregatedValues);
            return BitConverter.ToInt64(hashedKey, 0);
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

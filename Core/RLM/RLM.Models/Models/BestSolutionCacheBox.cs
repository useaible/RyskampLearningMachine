﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models
{
    public class BestSolutionCacheBox
    {
        public BestSolutionCacheBox() { }
        public BestSolutionCacheBox(IEnumerable<InputRangeInfo> ranges)
        {
            SetRanges(ranges);
        }

        public Dictionary<long, InputRangeInfo> Ranges { get; set; } = new Dictionary<long, InputRangeInfo>();
        //public IEnumerable<long> RneuronIds { get; set; }
        public SortedList<RlmInputKey, RlmInputValue> CachedInputs{ get; set; }

        public void Clear()
        {
            Ranges?.Clear();
            //Rneurons.Clear();
            //RneuronIds = null;
        }

        public void SetRanges(IEnumerable<InputRangeInfo> ranges)
        {
            foreach (var item in ranges)
            {
                Ranges.Add(item.InputId, item);
            }
        }

        public bool IsWithinRange(IDictionary<int, InputRangeInfo> dataRangeInfos, double linearTolerance = 0)
        {
            bool retVal = true;

            if (Ranges.Count == 0)
            {
                retVal = false;
            }
            else
            {
                foreach(var item in dataRangeInfos)
                {
                    var dataRangeinfo = item.Value; 
                    var cacheRangeInfo = Ranges[item.Value.InputId];
                    if (dataRangeinfo.InputType == Enums.RlmInputType.Linear)
                    {
                        if (cacheRangeInfo.FromValue > dataRangeinfo.FromValue || cacheRangeInfo.ToValue < dataRangeinfo.ToValue)
                        {
                            retVal = false;
                            break;
                        }
                    }
                    else
                    {
                        if (dataRangeinfo.Value != cacheRangeInfo.Value)
                        {
                            retVal = false;
                            break;
                        }
                    }
                }
            }

            return retVal;
        }
    }

    public class InputRangeInfo
    {
        public long InputId { get; set; }
        public Enums.RlmInputType InputType { get; set; }
        public double FromValue { get; set; }
        public double ToValue { get; set; }
        public string Value { get; set; }
    }

    public class RlmInputKey
    {
        public int InputNum { get; set; }
        public Enums.RlmInputType Type { get; set; }
        public double DoubleValue { get; set; }
        public string Value { get; set; }
    }

    public class RlmInputKeyDistinctComparer : IComparer<RlmInputKey>
    {
        public int Compare(RlmInputKey x, RlmInputKey y)
        {
            return x.Value.CompareTo(y.Value);
        }
    }

    public class RlmInputKeyLinearComparer : IComparer<RlmInputKey>
    {
        public int Compare(RlmInputKey x, RlmInputKey y)
        {
            return x.DoubleValue.CompareTo(y.DoubleValue);
        }
    }

    public class RlmInputValue
    {
        public long? RneuronId { get; set; }
        public SortedList<RlmInputKey, RlmInputValue> RelatedInputs { get; set; }

        //public static void TraverseInputForMatchingRneurons(KeyValuePair<RnnInputKey, RnnInputValue> inputPair, IDictionary<int, InputRangeInfo> rangeInfos, List<long> rneuronsFound)
        //{
        //    var key = inputPair.Key;
        //    var value = inputPair.Value;
        //    if (!value.RneuronId.HasValue)
        //    {
        //        var range = rangeInfos[key.InputNum];
        //        var filtered = (key.Type == Enums.RlmInputType.Linear) ? 
        //            value.RelatedInputs.Where(a => a.Key.DoubleValue >= range.FromValue && a.Key.DoubleValue <= range.ToValue) : 
        //            value.RelatedInputs.Where(a => a.Key.Value == range.Value);

        //        foreach (var relatedInput in filtered)
        //        {
        //            TraverseInputForMatchingRneurons(relatedInput, rangeInfos, rneuronsFound);
        //        }
        //    }
        //    else
        //    {
        //        rneuronsFound.Add(value.RneuronId.Value);
        //    }
        //}

        public static void RecurseInputForMatchingRneurons(SortedList<RlmInputKey, RlmInputValue> inputs, IDictionary<int, InputRangeInfo> rangeInfos, List<long> rneuronsFound)
        {
            //if (inputs.Count == 0) return;

            var key = inputs.First().Key;
            var range = rangeInfos[key.InputNum];
            var filtered = (key.Type == Enums.RlmInputType.Linear) ?
                inputs.Where(a => a.Key.DoubleValue >= range.FromValue && a.Key.DoubleValue <= range.ToValue) :
                inputs.Where(a => a.Key.Value == range.Value);
            
            foreach(var item in filtered)
            {                
                if (!item.Value.RneuronId.HasValue)
                {
                    RecurseInputForMatchingRneurons(item.Value.RelatedInputs, rangeInfos, rneuronsFound);
                }
                else
                {
                    rneuronsFound.Add(item.Value.RneuronId.Value);
                }
            }
        }

        public static SortedList<RlmInputKey, RlmInputValue> RecurseInputForMatchingRneuronsForCaching(SortedList<RlmInputKey, RlmInputValue> inputs, IDictionary<int, InputRangeInfo> rangeInfos, IDictionary<int, InputRangeInfo> rneuronRangeInfos, List<long> rneuronsFound, bool prevItemMatch = true)
        {
            var key = inputs.First().Key;
            var range = rangeInfos[key.InputNum];

            // for cache
            IComparer<RlmInputKey> comparer = (key.Type == Enums.RlmInputType.Linear) ? (IComparer<RlmInputKey>)new RlmInputKeyLinearComparer() : (IComparer<RlmInputKey>)new RlmInputKeyDistinctComparer();
            SortedList<RlmInputKey, RlmInputValue> cacheInput = new SortedList<RlmInputKey, RlmInputValue>(comparer);

            var filtered = (key.Type == Enums.RlmInputType.Linear) ?
                inputs.Where(a => a.Key.DoubleValue >= range.FromValue && a.Key.DoubleValue <= range.ToValue) :
                inputs.Where(a => a.Key.Value == range.Value);

            foreach (var item in filtered)
            {
                var cacheItemVal = new RlmInputValue() { RneuronId = item.Value.RneuronId };
                bool currentItemMatch = false;

                var rneuronRange = rneuronRangeInfos[item.Key.InputNum];
                if (prevItemMatch)
                    currentItemMatch = item.Key.Type == Enums.RlmInputType.Linear ? item.Key.DoubleValue >= rneuronRange.FromValue && item.Key.DoubleValue <= rneuronRange.ToValue : item.Key.Value == rneuronRange.Value;

                if (!item.Value.RneuronId.HasValue)
                {
                    cacheItemVal.RelatedInputs = RecurseInputForMatchingRneuronsForCaching(item.Value.RelatedInputs, rangeInfos, rneuronRangeInfos, rneuronsFound, currentItemMatch);

                    if (cacheItemVal.RelatedInputs.Count > 0)
                        cacheInput.Add(item.Key, cacheItemVal);
                }
                else
                {
                    if (prevItemMatch && currentItemMatch)
                        rneuronsFound.Add(cacheItemVal.RneuronId.Value);

                    cacheInput.Add(item.Key, cacheItemVal);
                }         
            }

            return cacheInput;
        }
    }

    public class RlmInputMomentum
    {
        bool hasValues = false;
        private double lastValue = 0;
        private List<int> momentums = new List<int>();
        private List<double> valDiffs = new List<double>();

        public long InputID { get; set; }
        public double MomentumDirection
        {
            get
            {
                return momentums.Average();
            }
        }
        public double MomentumValue
        {
            get
            {
                return valDiffs.Average();
            }
        }

        public void SetInputValue(double val)
        {
            int momentum = 0;
            double valDiff = 0;

            if (hasValues)
            {
                if (val < lastValue)
                {
                    momentum = -1;
                    valDiff = lastValue - val;
                }
                else if (val > lastValue)
                {
                    momentum = 1;
                    valDiff = val - lastValue;
                }
            }

            hasValues = true;
            lastValue = val;
            valDiffs.Add(valDiff);
            momentums.Add(momentum);
        }

        public void Reset()
        {
            hasValues = false;
            lastValue = 0;
            valDiffs.Clear();
            momentums.Clear();
        }        
    }
}

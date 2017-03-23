using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models.Utility
{
    public class RlmBenchmarkStats
    {
        public IDictionary<string, IEnumerable<RlmSessionBenchmarkStats>> Sessions { get; set; } = new SortedDictionary<string, IEnumerable<RlmSessionBenchmarkStats>>();
        public IDictionary<string, RlmSummaryBenchmarkStats> Summary { get; set; } = new SortedDictionary<string, RlmSummaryBenchmarkStats>();
    }

    public class RlmSessionBenchmarkStats
    {
        public int SessionNumber { get; set; }
        public double Score { get; set; }
        public double Totaltime { get; set; }
        public double TrainingTime { get; set; }
        public double GetBestSolution { get; set; }
        public double GetBestSolutionOverTrainingTime { get; set; }
        public double RebuildCacheBox { get; set; }
        public double RebuildCacheBoxOverTrainingTime { get; set; }
        public int NumberOfCycles { get; set; }
        public int NumberOfCacheBoxRebuilds { get; set; }
        public double LinearTolerance { get; set; }
        public double RandomnessLeft { get; set; }
    }

    public class RlmSummaryBenchmarkStats
    {
        public double AverageNumberOfCycles { get; set; }
        public double AverageNumberOfCacheBoxRebuild { get; set; }
        public double AverageTimeForGetBestSolution { get; set; }
        public double AverageTimeForCacheBoxRebuild { get; set; }
        public double AverageTimeForTraining { get; set; }
        public double AverageTimeForBestSolutionRebuild { get; set; }
        public double AverageTimeForEachSession { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public double AverageScore { get; set; }
        public long RnueronCount { get; set; }
    }
}

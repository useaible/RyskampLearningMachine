// Copyright 2017 Ryskamp Innovations LLC
// License Available through the RLM License Agreement
// https://github.com/useaible/RyskampLearningMachine/blob/dev-branch/License.md

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models
{
    public class RlmStats
    {
        public int AvgTimePerSessionInSeconds { get; set; }
        public TimeSpan AvgTimePerSession
        {
            get
            {
                return TimeSpan.FromSeconds(AvgTimePerSessionInSeconds);
            }
        }

        public int TotalSessionTimeInSeconds { get; set; }
        public TimeSpan TotalSessionTime
        {
            get
            {
                return TimeSpan.FromSeconds(TotalSessionTimeInSeconds);
            }
        }

        public int NumSessionsSinceLastBestScore { get; set; }
        public int TotalSessions { get; set; }
        public double MaxSessionScore { get; set; }
        public double AvgSessionScore { get; set; }
        public double LastSessionScore { get; set; }
        public long LastSessionId { get; set; }
        public int LastSessionTimeInSeconds { get; set; }
        public TimeSpan LastSessionTime
        {
            get
            {
                return TimeSpan.FromSeconds(LastSessionTimeInSeconds);
            }
        }
    }

    public class RlmSessionSummary
    {
        public long GroupId { get; set; }
        public double Score { get; set; }
        public int TimeInSeconds { get; set; }

        public TimeSpan Time
        {
            get
            {
                return TimeSpan.FromSeconds(TimeInSeconds);
            }
        }
    }
}

// Copyright 2017 Ryskamp Innovations LLC
// License Available through the RLM License Agreement
// https://github.com/useaible/RyskampLearningMachine/blob/dev-branch/License.md

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM
{
    public class RlmSessionHistory
    {
        public long Id { get; set; }
        public long SessionNumber { get; set; }
        public double SessionScore { get; set; }
        public DateTime DateTimeStart { get; set; }
        public DateTime DateTimeStop { get; set; }
        public TimeSpan Elapse
        {
            get
            {
                return DateTimeStop.Subtract(DateTimeStart);
            }
        }
    }
}

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
    public class RlmScoreInfo
    {
        public virtual long ID { get; protected set; }
        public virtual double TotalScore { get; protected set; }
        public virtual int Count { get; protected set; }

        public RlmScoreInfo(long id, double totalScore, int count)
        {
            ID = id;
            TotalScore = totalScore;
            Count = count;
        }
    }
}

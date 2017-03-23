using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM
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

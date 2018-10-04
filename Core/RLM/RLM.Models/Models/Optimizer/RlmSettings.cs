using RLM.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models.Optimizer
{
    public class RlmSettings
    {
        public virtual int StartRandomness { get; set; }
        public virtual int EndRandomness { get; set; }
        public virtual int MinLinearBracket { get; set; }
        public virtual int MaxLinearBracket { get; set; }
        public virtual int NumOfSessionsReset { get; set; }
        public virtual double SimulationTarget { get; set; }
        public virtual RlmSimulationType SimulationType { get; set; }
        public virtual TimeSpan Time { get; set; }
        public virtual int NumScoreHits { get; set; }
        public virtual string Name { get; set; }
        public virtual bool PersistData { get; set; }
        public virtual string DatabaseName { get; set; }
    }
}



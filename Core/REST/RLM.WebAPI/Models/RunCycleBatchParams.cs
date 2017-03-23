using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.WebAPI.Models
{
    public class RunCycleBatchParams
    {
        public RunCycleBatchParams()
        {
            Items = new List<RunCycleBatchItemParams>();
        }

        public long SessionId { get; set; }
        public bool Learn { get; set; }
        public ICollection<RunCycleBatchItemParams> Items { get; set; }
    }

    public class RunCycleBatchItemParams
    {
        public RunCycleBatchItemParams()
        {
            Inputs = new List<RlmIOWithValuesParams>();
            Outputs = new List<RlmIOWithValuesParams>();
        }

        public ICollection<RlmIOWithValuesParams> Inputs { get; set; }
        public ICollection<RlmIOWithValuesParams> Outputs { get; set; }
    }
}

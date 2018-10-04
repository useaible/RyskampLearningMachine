using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Database
{
    public class RlmBCQueues<T>
    {
        public RlmBCQueues(int id)
        {
            Id = id;
            WorkerQueues = new BlockingCollection<T>();
        }

        public int Id { get; set; }
        public BlockingCollection<T> WorkerQueues { get; private set; }
        public bool IsBusy { get; set; } = false;
    }

}

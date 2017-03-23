using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models
{
    public delegate int RlmIdeaOutputEquivalentDelegate(int output);

    public abstract class RlmIdea
    {
        // TODO determine what base structure we need for a rnn idea instance
    }

    public class RlmOutputLimiter : RlmIdea
    {
        public RlmOutputLimiter(long rlm_io_id, int indexMax, RlmIdeaOutputEquivalentDelegate method = null)
        {
            RlmIOId = rlm_io_id;
            IndexMax = indexMax;
            GetIndexEquivalent = method;
        }
        public long RlmIOId { get; set; }
        public int IndexMax { get; set; }
        public RlmIdeaOutputEquivalentDelegate GetIndexEquivalent { get; private set; }
    }

    public class RlmOutputExclude : RlmIdea
    {
        public RlmOutputExclude(long outputId)
        {
            OutputID = outputId;
            Values = new List<string>();
        }

        public RlmOutputExclude(long outputId, ICollection<string> values) : this(outputId)
        {
            Values = values;
        }

        public long OutputID { get; set; }
        public ICollection<string> Values { get; set; }
    }
}

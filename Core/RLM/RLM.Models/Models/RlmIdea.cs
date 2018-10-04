using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models
{
    public delegate int RlmIdeaOutputEquivalentDelegate(int output);
    public delegate void RlmIdeaRemoveActualOutputDelegate(int output);

    public abstract class RlmIdea
    {
        // TODO determine what base structure we need for a rnn idea instance

        public long RlmIOId { get; set; }
    }

    public class RlmOutputLimiter : RlmIdea
    {
        public RlmOutputLimiter(long rlm_io_id, int indexMax, RlmIdeaOutputEquivalentDelegate method = null, RlmIdeaRemoveActualOutputDelegate method2 = null)
        {
            RlmIOId = rlm_io_id;
            IndexMax = indexMax;
            GetIndexEquivalent = method;
            RemoveActualOutput = method2;
        }
        public int IndexMax { get; set; }
        public RlmIdeaOutputEquivalentDelegate GetIndexEquivalent { get; private set; }
        public RlmIdeaRemoveActualOutputDelegate RemoveActualOutput { get; private set; }
    }

    public class RlmOutputExclude : RlmIdea
    {
        public RlmOutputExclude(long outputId)
        {
            RlmIOId = outputId;
            Values = new List<string>();
        }

        public RlmOutputExclude(long outputId, ICollection<string> values) : this(outputId)
        {
            Values = values;
        }
        
        public ICollection<string> Values { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RNN
{    
    public delegate int RnnIdeaOutputEquivalentDelegate(int output);

    public abstract class rnn_idea
    {
        // TODO determine what base structure we need for a rnn idea instance
    }

    public class rnn_output_limiter : rnn_idea
    {
        public rnn_output_limiter(long rnn_io_id, int indexMax, RnnIdeaOutputEquivalentDelegate method = null)
        {
            RnnIOId = rnn_io_id;
            IndexMax = indexMax;
            GetIndexEquivalent = method;
        }
        public long RnnIOId { get; set; }
        public int IndexMax { get; set; }
        public RnnIdeaOutputEquivalentDelegate GetIndexEquivalent { get; private set; }
    }
    
    public class rnn_output_exclude : rnn_idea
    {
        public rnn_output_exclude(long outputId)
        {
            OutputID = outputId;
            Values = new List<string>();
        }

        public rnn_output_exclude(long outputId, ICollection<string> values) : this(outputId)
        {
            Values = values;
        }

        public long OutputID { get; set; }
        public ICollection<string> Values { get; set; }
    }
}

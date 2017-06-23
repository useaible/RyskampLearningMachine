using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RNN.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace RNN
{
    public class rnn_io
    {
        public long ID { get; set; }
        public rnn_network Network {get; set;} 
        public String Name {get;set;}
        public String DotNetType { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public RnnInputType Type { get; set; }
        
        [NotMapped]
        public rnn_output_limiter Idea { get; set; }

        //Constructor
        public rnn_io() { }

        public rnn_io(rnn_network rnn_net, String name, String dotnettype, double min, double max, long id = 0)
        {
            //ToDo:  Check for valid net and name
            this.Network = rnn_net;
            this.Name = name;
            this.DotNetType = dotnettype;
            this.Min = min;
            this.Max = max;
            this.ID = id;
        }

        public rnn_io(rnn_network rnn_net, String name, String dotnettype, double min, double max, RnnInputType type, long id = 0)
            : this (rnn_net, name, dotnettype, min, max, id)
        {            
            this.Type = type;
        }
    }

    public class rnn_io_with_value:rnn_io
    {
        public String Value {get; set; }

        public rnn_io_with_value() { }

        public rnn_io_with_value(rnn_io inp, String value)
            : base(inp.Network, inp.Name, inp.DotNetType, inp.Min,inp.Max, inp.Type, inp.ID)
        {
            //ToDo:  Check for valid value based upon type, min, max, etc.
            this.Value = value;
        }
    }
}

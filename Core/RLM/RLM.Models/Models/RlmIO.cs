using RLM.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models
{
    public class RlmIO
    {
        public long ID { get; set; }
        public String Name { get; set; }
        public String DotNetType { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public Enums.RlmInputType Type { get; set; }

        [NotMapped]
        public RlmOutputLimiter Idea { get; set; }

        //Constructor
        public RlmIO() { }
        /// <summary>
        /// object type for input and output settings
        /// </summary>
        /// <param name="name">Sets RlmIO Name property</param>
        /// <param name="dotnettype">Sets rlm_io DotNetType property which assigns the object type in .NET</param>
        /// <param name="min">Sets RlmIO Min property which sets the minimum range value of the input or output</param>
        /// <param name="max">Sets RlmIO Max property which sets the maximum range value of the input or output</param>
        /// <param name="id">Assigns unique identifier to the input/output</param>
        public RlmIO(String name, String dotnettype, double min, double max, long id = 0)
        {
            //ToDo:  Check for valid net and name
            this.Name = name;
            this.DotNetType = dotnettype;
            this.Min = min;
            this.Max = max;
            this.ID = id;
        }
        /// <summary>
        /// object type for input and output settings
        /// </summary>
        /// <param name="name">Sets RlmIO Name property</param>
        /// <param name="dotnettype">Sets rlm_io DotNetType property which assigns the object type in .NET</param>
        /// <param name="min">Sets RlmIO Min property which sets the minimum range value of the input or output</param>
        /// <param name="max">Sets RlmIO Max property which sets the maximum range value of the input or output</param>
        /// <param name="type"></param>
        /// <param name="id">Assigns unique identifier to the input/output</param>
        public RlmIO(String name, String dotnettype, double min, double max, Enums.RlmInputType type, long id = 0)
            : this(name, dotnettype, min, max, id)
        {
            this.Type = type;
        }
    }

    public class RlmIOWithValue : RlmIO
    {
        public String Value { get; set; }

        //public double CacheBoxTolerance { get; set; }
        
        public RlmInputMomentum InputMomentum { get; set; }

        public RlmIOWithValue() { }

        public RlmIOWithValue(RlmIO inp, String value)
            : base(inp.Name, inp.DotNetType, inp.Min, inp.Max, inp.Type, inp.ID)
        {
            //ToDo:  Check for valid value based upon type, min, max, etc.
            this.Value = value;
        }        
    }
}

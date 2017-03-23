using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RNN.Models
{
    public class Input_Values_Rneuron
    {
        public Int64 ID { get; set; }
        public String Value { get; set; }
        public Int64 Input_ID { get; set; }
        public Int64 Rneuron_ID { get; set; }

        [ForeignKey("Input_ID")]
        public virtual Input Input { get; set; }
        [ForeignKey("Rneuron_ID")]
        public virtual Rneuron Rneuron { get; set; }

        [NotMapped]
        public string DotNetType { get; set; }

        //Constructors
        //One parameterless constructor is required by EF for auto creation
        public Input_Values_Rneuron()
        {

        }
    }
}

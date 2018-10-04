using RLM.Enums;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace RLM.Models
{
    public class _Input_Values_Rneuron
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 ID { get; set; }
        public String Value { get; set; }
        [ForeignKey("_Input")]
        public Int64 Input_ID { get; set; }
        [ForeignKey("_Rneuron")]
        public Int64 Rneuron_ID { get; set; }
    }

    public class Input_Values_Rneuron : _Input_Values_Rneuron
    {
        public virtual Input Input { get; set; }
        public virtual Rneuron Rneuron { get; set; }

        [NotMapped]
        public string DotNetType { get; set; }
        [NotMapped]
        public RlmInputType InputType { get; set; }

        //Constructors
        //One parameterless constructor is required by EF for auto creation
        public Input_Values_Rneuron()
        {

        }
    }
}

using System;
using System.Collections.Generic;
using RLM.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace RLM.Models
{
    public class _Input
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Int64 ID { get; set; }
        public String Name { get; set; }
        public double Max { get; set; }
        public double Min { get; set; }
        public RlmInputType Type { get; set; }
        [ForeignKey("_Rnetwork")]
        public long Rnetwork_ID { get; set; }
        [ForeignKey("_Input_Output_Type")]
        public long Input_Output_Type_ID { get; set; }
        public int Order { get; set; }
        
        // TODO remove, deprecated
        public long HashedKey { get; set; }
    }

    public class Input : _Input
    {
        //Navigation Proprties
        public virtual Rnetwork Rnetwork { get; set; }
        public virtual Input_Output_Type Input_Output_Type { get; set; }
        public virtual ICollection<Input_Values_Rneuron> Input_Values_Reneurons { get; set; }

        //Constructors
        //One parameterless constructor is required by EF for auto creation
        public Input()
        {
            Input_Values_Reneurons = new HashSet<Input_Values_Rneuron>();
        }

    }
}

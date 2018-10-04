using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace RLM.Models
{
    public class _Output
    {
        public long HashedKey { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Int64 ID { get; set; }
        public String Name { get; set; }
        public double Max { get; set; }
        public double Min { get; set; }
        [ForeignKey("_Rnetwork")]
        public Int64 Rnetwork_ID { get; set; }
        [ForeignKey("_Input_Output_Type")]
        public Int64 Input_Output_Type_ID { get; set; }
        public int Order { get; set; }
    }

    public class Output : _Output
    {     
        //Navigation Properties
        public virtual Rnetwork Rnetwork { get; set; }
        public virtual Input_Output_Type Input_Output_Type { get; set; }
        public virtual ICollection<Output_Values_Solution> Output_Values_Solutions { get; set; }

        //Constructors
        //One parameterless constructor is required by EF for auto creation
        public Output()
        {
            Output_Values_Solutions = new HashSet<Output_Values_Solution>();
        }
    }
}

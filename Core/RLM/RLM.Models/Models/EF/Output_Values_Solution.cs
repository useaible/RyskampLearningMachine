using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace RLM.Models
{
    public class _Output_Values_Solution
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Int64 ID { get; set; }
        public String Value { get; set; }
        [ForeignKey("_Output")]
        public Int64 Output_ID { get; set; }
        [ForeignKey("_Solution")]
        public Int64 Solution_ID { get; set; }
    }

    public class Output_Values_Solution : _Output_Values_Solution
    {
        public virtual Output Output { get; set; }
        public virtual Solution Solution { get; set; }

        //Constructors
        //One parameterless constructor is required by EF for auto creation
        public Output_Values_Solution()
        {

        }
    }
}

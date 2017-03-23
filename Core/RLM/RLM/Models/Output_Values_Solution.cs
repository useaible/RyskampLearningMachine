using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RNN.Models
{
    public class Output_Values_Solution
    {
        public Int64 ID { get; set; }
        public String Value { get; set; }
        public Int64 Output_ID { get; set; }
        public Int64 Solution_ID { get; set; }

        [ForeignKey("Output_ID")]
        public virtual Output Output { get; set; }
        [ForeignKey("Solution_ID")]
        public virtual Solution Solution { get; set; }

        //Constructors
        //One parameterless constructor is required by EF for auto creation
        public Output_Values_Solution()
        {

        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RNN.Models
{
    public class Output
    {
        public Int64 ID { get; set; }
        public String Name { get; set; }
        public double Max { get; set; }
        public double Min { get; set; }        
        public Int64 Rnetwork_ID { get; set; }
        public Int64 Input_Output_Type_ID { get; set; }

        //Navigation Properties
        [ForeignKey("Rnetwork_ID")]
        public virtual Rnetwork Rnetwork { get; set; }
        [ForeignKey("Input_Output_Type_ID")]
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

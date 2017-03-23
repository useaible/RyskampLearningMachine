using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models
{
    public class Input_Output_Type
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Int64 ID { get; set; }
        public String Name { get; set; }
        public String DotNetTypeName { get; set; }

        //Navigation Properties
        public virtual ICollection<Input> Inputs { get; set; }
        public virtual ICollection<Output> Outputs { get; set; }


        //Constructors
        //One parameterless constructor is required by EF for auto creation
        public Input_Output_Type()
        {
            Inputs = new HashSet<Input>();
            Outputs = new HashSet<Output>();
        }
    }
}

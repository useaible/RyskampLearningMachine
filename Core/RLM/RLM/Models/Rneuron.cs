using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RNN.Models
{
    public class Rneuron
    {
        public Int64 ID { get; set; }
        public double RandomizationFactor { get; set; }
        public Int64? Rnetwork_ID { get; set; }

        // Navigation Properties
        [ForeignKey("Rnetwork_ID")]
        public virtual Rnetwork Rnetwork { get; set; }
        public virtual ICollection<Case> Cases { get; set; }
        public virtual ICollection<Input_Values_Rneuron> Input_Values_Reneurons { get; set; }

        //Constructors
        //One parameterless constructor is required by EF for auto creation
        public Rneuron()
        {
            Cases = new List<Case>();
            Input_Values_Reneurons = new List<Input_Values_Rneuron>();
        }
    }
}

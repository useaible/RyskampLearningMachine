using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace RLM.Models
{
    public class _Rneuron
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Int64 ID { get; set; }
        public double RandomizationFactor { get; set; }
        [ForeignKey("_Rnetwork")]
        public Int64? Rnetwork_ID { get; set; }
    }

    public class Rneuron : _Rneuron
    {
        private ICollection<Case> cases;
        private ICollection<Input_Values_Rneuron> input_values_rneurons;

        // Navigation Properties
        public virtual Rnetwork Rnetwork { get; set; }
        public virtual ICollection<Case> Cases
        {
            get
            {
                if (cases == null)
                {
                    cases = new HashSet<Case>();
                }

                return cases;
            }
            set
            {
                cases = value;
            }
        }

        public virtual ICollection<Input_Values_Rneuron> Input_Values_Rneurons
        {
            get
            {
                if (input_values_rneurons == null)
                {
                    input_values_rneurons = new HashSet<Input_Values_Rneuron>();
                }

                return input_values_rneurons;
            }
            set
            {
                input_values_rneurons = value;
            }
        }

        [NotMapped]
        public bool SavedToDb { get; set; }

        //Constructors
        //One parameterless constructor is required by EF for auto creation
        public Rneuron()
        {
            //Cases = new List<Case>();
            //Input_Values_Rneurons = new List<Input_Values_Rneuron>();
        }
    }
}

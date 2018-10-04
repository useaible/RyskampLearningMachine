using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace RLM.Models
{
    public class _Solution
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Int64 ID { get; set; }
    }

    public class Solution : _Solution
    {
        private ICollection<Output_Values_Solution> output_values_solutions;
        private ICollection<Case> cases;

        //Navigation Properties
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

        public virtual ICollection<Output_Values_Solution> Output_Values_Solutions
        {
            get
            {
                if (output_values_solutions == null)
                {
                    output_values_solutions = new HashSet<Output_Values_Solution>();
                }

                return output_values_solutions;
            }
            set
            {
                output_values_solutions = value;
            }
        }

        [NotMapped]
        public bool SavedToDb { get; set; }

        //Constructors
        //One parameterless constructor is required by EF for auto creation
        public Solution()
        {
            //Cases = new HashSet<Case>();
            //Output_Values_Solutions = new HashSet<Output_Values_Solution>();
        }
    }
}

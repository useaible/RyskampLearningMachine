using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models
{
    public class Solution
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Int64 ID { get; set; }

        //Navigation Properties
        public virtual ICollection<Case> Cases { get; set; }
        public virtual ICollection<Output_Values_Solution> Output_Values_Solutions { get; set; }

        [NotMapped]
        public bool SavedToDb { get; set; }

        //Constructors
        //One parameterless constructor is required by EF for auto creation
        public Solution()
        {
            Cases = new HashSet<Case>();
            Output_Values_Solutions = new HashSet<Output_Values_Solution>();
        }
    }
}

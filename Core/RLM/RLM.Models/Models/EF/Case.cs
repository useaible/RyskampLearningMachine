using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models
{
    public class Case
    {
        //[DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Int64 ID { get; set; }
        public DateTime CycleStartTime { get; set; }
        public DateTime CycleEndTime {get; set; }
        public double CycleScore  { get; set; }
        public double CurrentRFactor { get; set;}
        public Int16 CurrentMFactor { get; set; }
        public Boolean ResultCompletelyRandom { get; set; }
        public Int16 SequentialMFactorSuccessesCount { get; set; }
        public Int64 Rneuron_ID { get; set; }
        public Int64 Session_ID { get; set; }
        public Int64 Solution_ID { get; set; }
        public Int64 Order { get; set; }

        //Navigation Properties
        [ForeignKey("Rneuron_ID")]
        public virtual Rneuron Rneuron { get; set; }
        [ForeignKey("Session_ID")]
        public virtual Session Session { get; set; }
        [ForeignKey("Solution_ID")]
        public virtual Solution Solution { get; set; }
        public virtual ICollection< Idea_Implementation> Idea_Implementations { get; set; }

        [NotMapped]
        public bool SavedToDb { get; set; }

        //One parameterless constructor is required by EF for auto creation
        public Case ()
        {
            Idea_Implementations = new HashSet<Idea_Implementation>();
        }
    }
}

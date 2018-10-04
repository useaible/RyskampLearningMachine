using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace RLM.Models
{
    public class _Case
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 ID { get; set; }
        public DateTime CycleStartTime { get; set; }
        public DateTime CycleEndTime { get; set; }
        public double CycleScore { get; set; }
        public double CurrentRFactor { get; set; }
        public Int16 CurrentMFactor { get; set; }
        public Boolean ResultCompletelyRandom { get; set; }
        public Int16 SequentialMFactorSuccessesCount { get; set; }
        [ForeignKey("_Rneuron")]
        public Int64 Rneuron_ID { get; set; }
        [ForeignKey("_Session")]
        public Int64 Session_ID { get; set; }
        [ForeignKey("_Solution")]
        public Int64 Solution_ID { get; set; }
        public Int64 Order { get; set; }
    }
    public class Case : _Case
    {
        private ICollection<Idea_Implementation> idea_Implementations;
                
        //Navigation Properties
        public virtual Rneuron Rneuron { get; set; }
        public virtual Session Session { get; set; }
        public virtual Solution Solution { get; set; }
        public virtual ICollection< Idea_Implementation> Idea_Implementations
        {
            get
            {
                if(idea_Implementations == null)
                {
                    idea_Implementations = new HashSet<Idea_Implementation>();
                }

                return idea_Implementations;
            }
            set
            {
                idea_Implementations = value;
            }
        }

        [NotMapped]
        public bool SavedToDb { get; set; }

        //One parameterless constructor is required by EF for auto creation
        public Case ()
        {
            //Idea_Implementations = new HashSet<Idea_Implementation>();
        }
    }
}

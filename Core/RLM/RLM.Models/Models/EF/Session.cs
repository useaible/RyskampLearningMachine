using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace RLM.Models
{
    public class _Session
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Int64 ID { get; set; }
        public String SessionGuid { get; set; }
        public DateTime DateTimeStart { get; set; }
        public DateTime DateTimeStop { get; set; }
        public double SessionScore { get; set; }
        public bool Hidden { get; set; }
        [ForeignKey("_Rnetwork")]
        public Int64 Rnetwork_ID { get; set; }
    }

    public class Session : _Session
    {
        private ICollection<Case> cases;
               
        //Navigation Properties
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

        // TEMPORARY for benchmarks only
        [NotMapped]
        public TimeSpan TrainingTime { get; set; }
        [NotMapped]
        public TimeSpan BestSolutionTimeRebuild { get; set; }
        [NotMapped]
        public TimeSpan GetBestSolutionTime { get; set; }
        [NotMapped]
        public TimeSpan RebuildCacheTime { get; set; }
        [NotMapped]
        public double CurrLinearToleranceValue { get; set; }
        [NotMapped]
        public double CurrRandomnessValue { get; set; }
        [NotMapped]
        public int CycleCount { get; set; }
        [NotMapped]
        public int CacheBoxCount { get; set; }
        // TEMPORARY for benchmarks only

        [NotMapped]
        public bool CreatedToDb { get; set; }

        [NotMapped]
        public bool UpdatedToDb { get; set; }

        //Constructors
        //One parameterless constructor is required by EF for auto creation
        public Session()
        {
            //Cases = new HashSet<Case>();
            Hidden = false;
        }

        public void EndSession()
        {
            DateTimeStop = DateTime.Now;
        }

    }
}

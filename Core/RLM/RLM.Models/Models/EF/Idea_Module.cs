using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models
{
    public class Idea_Module
    {
        public Int64 ID { get; set; }
        public String Name { get; set; }
        public String PatternRecognitionCode { get; set; }
        public Int16 MinumumCasesToDetectPatern { get; set; }
        public String PatternPredictionCode { get; set; }
        public Int64 Rnetwork_ID { get; set; }

        // Navigation Properties
        [ForeignKey("Rnetwork_ID")]
        public virtual Rnetwork Rnetwork { get; set; }
        public ICollection<Idea_Implementation> IdeaImplementations { get; set; }

        //Constructors
        //One parameterless constructor is required by EF for auto creation
        public Idea_Module()
        {
            IdeaImplementations = new HashSet<Idea_Implementation>();
        }
    }
}

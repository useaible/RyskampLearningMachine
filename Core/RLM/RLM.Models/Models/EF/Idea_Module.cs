using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace RLM.Models
{
    public class _Idea_Module
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 ID { get; set; }
        public String Name { get; set; }
        public String PatternRecognitionCode { get; set; }
        public Int16 MinumumCasesToDetectPatern { get; set; }
        public String PatternPredictionCode { get; set; }
        [ForeignKey("_Rnetwork")]
        public Int64 Rnetwork_ID { get; set; }
    }

    public class Idea_Module : _Idea_Module
    {

        // Navigation Properties
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

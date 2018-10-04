using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace RLM.Models
{
    public class _Idea_Implementation
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 ID { get; set; }
        public Int64 IdeaRatingEndOfCycleCase { get; set; }
        [ForeignKey("_Case")]
        public Int64? Case_ID { get; set; }
        [ForeignKey("_Session")]
        public Int64? Session_ID { get; set; }
        [ForeignKey("_Idea_Module")]
        public Int64? Idea_Module_ID { get; set; }
    }

    public class Idea_Implementation : _Idea_Implementation
    {
        // Navigation Properties
        public virtual Case Case { get; set; }
        public virtual Session Session { get; set; }
        public virtual Idea_Module Idea_Module { get; set; }

        //Constructors
        //One parameterless constructor is required by EF for auto creation
        public Idea_Implementation()
        {

        }
    }
}


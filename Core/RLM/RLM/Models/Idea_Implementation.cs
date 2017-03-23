using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RNN.Models
{
    public class Idea_Implementation
    {
        public Int64 ID { get; set; }
        public Int64 IdeaRatingEndOfCycleCase { get; set;}
        public Int64? Case_ID { get; set; }
        public Int64? Session_ID { get; set; }
        public Int64? Idea_Module_ID { get; set; }

        // Navigation Properties
        [ForeignKey("Case_ID")]
        public virtual Case Case { get; set; }
        [ForeignKey("Session_ID")]
        public virtual Session Session { get; set; }
        [ForeignKey("Idea_Module_ID")]
        public virtual Idea_Module Idea_Module { get; set; }

        //Constructors
        //One parameterless constructor is required by EF for auto creation
        public Idea_Implementation()
        {

        }
    }
}


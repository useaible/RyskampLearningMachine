using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace RLM.Models
{
    public class _Rnetwork
    {
        //Core Fields
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Int64 ID { get; set; }
        public String Name { get; set; }
        public DateTime DateTimeCreated { get; set; }
        public short MFactor { get; set; }
    }

    public class Rnetwork : _Rnetwork
    {   
        //Navigation Properties
        //public virtual ICollection<Rneuron> Rneurons { get; set; }
        public virtual ICollection<Session> Sessions { get; set; }
        public virtual ICollection<RnetworkSetting> RnetworkSettings { get; set; }
        public virtual ICollection<Idea_Module> Idea_Modules { get; set; }
        public virtual ICollection<Input> Inputs { get; set; }
        public virtual ICollection<Output> Outputs { get; set; }

        //Constructors
        //One parameterless constructor is required by EF for auto creation
        public Rnetwork()
        {
            Sessions = new HashSet<Session>();
            RnetworkSettings = new HashSet<RnetworkSetting>();
            Idea_Modules = new HashSet<Idea_Module>();
            Inputs = new HashSet<Input>();
            Outputs = new HashSet<Output>();
        }

    }
}

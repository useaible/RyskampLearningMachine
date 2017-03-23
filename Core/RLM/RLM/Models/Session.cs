using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RNN.Models
{
    public class Session
    {
        public Int64 ID { get; set; }
        public String SessionGuid { get; set; }
        public DateTime DateTimeStart { get; set; }
        public DateTime DateTimeStop { get; set; }
        public double SessionScore { get; set; }
        public bool Hidden { get; set; }
        public Int64 Rnetwork_ID { get; set; }

        //Navigation Properties
        [ForeignKey("Rnetwork_ID")]
        public virtual Rnetwork Rnetwork { get; set; }
        public virtual ICollection<Case> Cases { get; set; }

        //Constructors
        //One parameterless constructor is required by EF for auto creation
        public Session()
        {
            Cases = new HashSet<Case>();
            Hidden = false;
        }

        public void EndSession()
        {
            DateTimeStop = DateTime.Now;
        }

    }
}

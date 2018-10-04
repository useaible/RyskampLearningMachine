using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace RLM.Models
{
    public class _RnetworkSetting
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 ID { get; set; }
        public String Name { get; set; }
        public String Value { get; set; }
        [ForeignKey("_Rnetwork")]
        public Int64 Rnetwork_ID { get; set; }
    }

    public class RnetworkSetting : _RnetworkSetting
    {
        // Navigation Properties
        public virtual Rnetwork Rnetwork { get; set; }

        //Constructors
        //One parameterless constructor is required by EF for auto creation
        public RnetworkSetting()
        {

        }
    }
}

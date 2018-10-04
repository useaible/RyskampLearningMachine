using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models.Optimizer
{
    public class ResourceAttribute
    {
        public ResourceAttribute()
        {
            ResourceAttributeValues = new List<string>();
        }
        public string Name { get; set; }
        public List<string> ResourceAttributeValues { get; set; }
        public Type AttributeDataType { get; set; }

    }

    public class ResourceAttributeDetails
    {
        public ResourceAttributeDetails(ResourceAttribute attr)
        {
            Name = attr.Name;
            AttributeDataType = attr.AttributeDataType;
        }

        public string Name { get; set; }
        public Type AttributeDataType { get; set; }
        public object Value { get; set; }
    }
}

using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RequirementsChecklistTool
{
    public class RAMRequirementChecker : FeatureChecker
    {
        public RAMRequirementChecker()
        {
            Name = "RAM >= 2Gb";
        }

        public override bool Check()
        {
            HasCorrectVersion = false;
            Message = $"\n{Name} not installed on your machine.";

            ComputerInfo info = new ComputerInfo();

            var totalMemoryPh = info.TotalPhysicalMemory;

            int gb = 1073741824;
            var totalMemoryGb = Convert.ToDouble(totalMemoryPh) / gb;

            if (totalMemoryGb >= 2)
            {
                HasCorrectVersion = true;
                Message = $"{Name}... OK";
            }

            return HasCorrectVersion;
        }

        protected override string GetDetailedMessageInformation()
        {
            if (HasCorrectVersion)
            {
                return Message;
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine($"{Message} Unfortunately, your current RAM is not able to support our requirements.\n");
                foreach (var version in Versions)
                {
                    sb.AppendLine($" -> {version}");
                }
                return sb.ToString();
            }
        }
    }
}

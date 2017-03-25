using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RequirementsChecklistTool
{
    public class OSRequirementChecker : FeatureChecker
    {
        public OSRequirementChecker()
        {
            Name = "Windows 8 or 10 versions";
            Url = "https://www.microsoft.com/en-us/windows/";
        }
        public override bool Check()
        {
            HasCorrectVersion = false;
            Message = $"\n{Name} not installed on your machine.";

            ComputerInfo info = new ComputerInfo();
            var osName = info.OSFullName;
            var osVer = info.OSVersion;
            var osplatform = info.OSPlatform;

            var win8V = "6.2";
            var win10V1 = "6.4";
            var win10V2 = "10.0";

            if (osVer.StartsWith(win8V) || osVer.StartsWith(win10V1) || osVer.StartsWith(win10V2))
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
                sb.AppendLine($"{Message} Please install any of windows versions 6.2.9200.0 and above.\n");
                sb.AppendLine($"Get {Name} here: {Url}");
                return sb.ToString();
            }
        }
    }
}

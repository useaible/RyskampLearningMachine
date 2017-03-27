using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RequirementsTool
{
    public class IISExpressChecker : RegistryChecker
    {
        const string RLM_BASE_URL = "http://localhost:22222/api/rlm/";
        const string REGISTRY_BASE = @"SOFTWARE\Microsoft\IISExpress";
        const double IISEXPRESS_VERSION_IDENTIFIER = 7.5;

        public IISExpressChecker()
        {
            Name = "Internet Information Services (IIS) Express";
            Url = "https://www.microsoft.com/en-us/download/details.aspx?id=48264";
            TutorialUrl = "https://www.iis.net/learn/extensions/using-iis-express/running-iis-express-from-the-command-line";

            Versions.Add("7.5 or later");
        }

        public string TutorialUrl { get; protected set; }

        protected override RegistryHive BaseKey { get; set; } = RegistryHive.LocalMachine;
        
        protected override void CheckRegistry(RegistryKey localKey)
        {
            using (var rootKey = localKey.OpenSubKey(REGISTRY_BASE))
            {
                if (rootKey != null)
                {
                    var version = rootKey.GetValue("Version");
                    if (version != null)
                    {
                        double versionVal = Convert.ToDouble(version.ToString().Substring(0, version.ToString().IndexOf(".") + 1));
                        if (versionVal >= IISEXPRESS_VERSION_IDENTIFIER)
                        {
                            HasCorrectVersion = true;
                            Message = $"{Name}...OK";
                        }
                        else
                        {
                            Message = $"{Name} installed on your machine is incompatible.";
                        }
                    }
                }
            }
        }

        protected override string GetDetailedMessageInformation()
        {
            var detailedMsg = base.GetDetailedMessageInformation();

            var sb = new StringBuilder();
            sb.Append(detailedMsg);
            sb.AppendLine($"Web API URL when hosting on {Name}: {RLM_BASE_URL}");

            if (!HasCorrectVersion)
            {
                sb.AppendLine($"Help on running IIS Express via CMD: {TutorialUrl}");
                sb.AppendLine("NOTE: If you have Visual Studio installed then IIS Express should be installed as well and you can then run the Web API (debug or without debugging) there instead of via CMD.");                
            }

            return sb.ToString();
        }
    }
}

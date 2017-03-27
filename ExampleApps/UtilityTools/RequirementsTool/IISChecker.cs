using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RequirementsTool
{
    public class IISChecker : RegistryChecker
    {
        const string REGISTRY_BASE = @"SOFTWARE\Microsoft\InetStp";
        const double IIS_VERSION_IDENTIFIER = 7.5;

        public IISChecker()
        {
            Name = "Internet Information Services (IIS)";
            Url = "https://msdn.microsoft.com/en-us/library/ms181052(v=vs.80).aspx";

            Versions.Add("7.5 or later");
        }

        protected override RegistryHive BaseKey { get; set; } = RegistryHive.LocalMachine;

        protected override void CheckRegistry(RegistryKey localKey)
        {
            using (var rootKey = localKey.OpenSubKey(REGISTRY_BASE))
            {
                if (rootKey != null)
                {
                    var version = rootKey.GetValue("VersionString");
                    if (version != null)
                    {
                        var versionVal = Convert.ToDouble(version.ToString().Replace("Version", "").Trim());
                        if (versionVal >= IIS_VERSION_IDENTIFIER)
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
    }
}

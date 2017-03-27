using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;

namespace RequirementsTool
{
    public class VisualStudioChecker : RegistryChecker
    {
        const string REGISTRY_BASE_VS = @"SOFTWARE\Microsoft\VisualStudio";
        const string REGISTRY_VS_SETUP = @"{{versionNumber}}\Setup";
        const int VS_VERSION_IDENTIFIER = 14; // 2015        
        readonly Regex REGEX = new Regex("Microsoft Visual Studio (Professional|Premium|Enterprise|Community|Express).*", RegexOptions.IgnoreCase);

        public VisualStudioChecker()
        {
            Name = "Visual Studio";
            Url = "https://www.visualstudio.com/downloads/";

            Versions.Add("2015 or later");
        }

        protected override RegistryHive BaseKey { get; set; } = RegistryHive.LocalMachine;

        protected override void CheckRegistry(RegistryKey localkey)
        {
            using (var rootKey = localkey.OpenSubKey(REGISTRY_BASE_VS))
            {
                if (rootKey != null)
                {
                    bool hasMatchingVersion = false;

                    var subkeyNames = rootKey.GetSubKeyNames();
                    foreach(var keyName in subkeyNames)
                    {
                        double versionNumber;
                        if (double.TryParse(keyName, out versionNumber))
                        {
                            if (versionNumber >= VS_VERSION_IDENTIFIER)
                            {
                                using (var setupVsKey = rootKey.OpenSubKey(REGISTRY_VS_SETUP.Replace("{{versionNumber}}", keyName)))
                                {
                                    if (setupVsKey != null)
                                    {
                                        var setupSubkeys = setupVsKey.GetSubKeyNames();
                                        var filteredSubkeys = setupSubkeys.Where(a => REGEX.IsMatch(a)).ToList();

                                        foreach(var item in filteredSubkeys)
                                        {
                                            using (var setupItemKey = setupVsKey.OpenSubKey(item))
                                            {
                                                if (setupItemKey != null)
                                                {
                                                    hasMatchingVersion = true;
                                                    var installInfo = setupItemKey.GetValue("InstallSuccess");

                                                    if (installInfo != null && Convert.ToInt32(installInfo) == 1)
                                                    {
                                                        HasCorrectVersion = true;
                                                        Message = $"{Name}...OK";
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (HasCorrectVersion)
                            break;
                    }

                    if (!HasCorrectVersion && hasMatchingVersion)
                    {
                        Message = $"{Name} installed on your machine is incompatible.";
                    }
                }
            }
        }
    }
}

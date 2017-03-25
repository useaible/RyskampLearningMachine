using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;

namespace RequirementsChecklistTool
{
    public class NetFrameworkChecker : FeatureChecker
    {
        const string REGISTRY_BASE = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";
        const int RELEASEKEY_VALUE_FOR_462 = 394802;
        
        public NetFrameworkChecker()
        {
            Name = ".NET Framework";
            Url = "https://www.microsoft.com/net/download/framework";

            Versions.Add("4.6.2 or later");
        }

        public override bool Check()
        {
            HasCorrectVersion = false;
            Message = $"{Name} version installed is incompatible.";

            var local32Key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            var local64Key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);

            try
            {
                // try 32 bit
                CheckRegistry(local32Key);

                if (!HasCorrectVersion)
                {
                    // try 64 bit
                    CheckRegistry(local64Key);
                }
            }
            finally
            {
                if (local32Key != null) local32Key.Dispose();
                if (local64Key != null) local64Key.Dispose();
            }

            return HasCorrectVersion;
        }

        private void CheckRegistry(RegistryKey localKey)
        {
            using (var rootKey = localKey.OpenSubKey(REGISTRY_BASE))
            {
                if (rootKey != null)
                {
                    var releaseKey = rootKey.GetValue("Release");
                    if (releaseKey != null)
                    {
                        int releaseKeyVal = Convert.ToInt32(releaseKey);
                        if (releaseKeyVal >= RELEASEKEY_VALUE_FOR_462)
                        {
                            HasCorrectVersion = true;
                            Message = $"{Name}...OK";
                        }
                    }
                }
            }
        }
    }
}

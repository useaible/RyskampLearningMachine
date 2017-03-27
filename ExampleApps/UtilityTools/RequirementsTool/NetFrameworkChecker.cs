using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;

namespace RequirementsTool
{
    public class NetFrameworkChecker : RegistryChecker
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
            HasCorrectVersion = base.Check();

            if (!HasCorrectVersion)
                Message = $"{Name} version installed is incompatible.";
            
            return HasCorrectVersion;
        }

        protected override RegistryHive BaseKey { get; set; } = RegistryHive.LocalMachine;

        protected override void CheckRegistry(RegistryKey localKey)
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

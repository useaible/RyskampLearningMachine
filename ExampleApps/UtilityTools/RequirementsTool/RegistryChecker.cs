using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RequirementsTool
{
    public abstract class RegistryChecker : FeatureChecker
    {
        protected abstract RegistryHive BaseKey { get; set; }

        protected abstract void CheckRegistry(RegistryKey localKey);

        public override bool Check()
        {
            HasCorrectVersion = false;
            Message = $"{Name} is not installed on your machine.";

            var local32Key = RegistryKey.OpenBaseKey(BaseKey, RegistryView.Registry32);
            var local64Key = RegistryKey.OpenBaseKey(BaseKey, RegistryView.Registry64);

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
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;

namespace RequirementsChecklistTool
{
    public class PythonChecker : FeatureChecker
    {
        const string REGISTRY_BASE = @"SOFTWARE\Python\PythonCore";

        private PythonNetChecker pythonNet;

        public PythonChecker()
        {
            Name = "Python";
            Url = $"https://www.python.org/downloads";

            Versions.Add("3.5.0");
            Versions.Add("3.5.1");
            Versions.Add("3.5.2");
            Versions.Add("3.5.3");

            pythonNet = new PythonNetChecker();
        }

        public override bool Check()
        {
            HasCorrectVersion = false;
            Message = $"{Name} not installed on your machine.";

            var local32Key = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
            var local64Key = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);

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

            pythonNet.Check();

            return HasCorrectVersion;
        }

        private void CheckRegistry(RegistryKey localKey)
        {
            using (var pythonCore = localKey.OpenSubKey(REGISTRY_BASE))
            {
                if (pythonCore != null)
                {
                    var subKeyNames = pythonCore.GetSubKeyNames();
                    foreach (var subKeyName in subKeyNames)
                    {
                        using (var subKey = pythonCore.OpenSubKey(subKeyName))
                        {
                            if (subKey != null)
                            {
                                using (var installedFeaturesKey = subKey.OpenSubKey("InstalledFeatures"))
                                {
                                    var value = installedFeaturesKey.GetValue("exe");
                                    if (value != null)
                                    {
                                        var version = value.ToString().Substring(0, 5);
                                        foreach (var reqVersion in Versions)
                                        {
                                            if (reqVersion == version)
                                            {
                                                HasCorrectVersion = true;
                                                Message = $"{Name}... OK";
                                                pythonNet.PythonKeyPath = subKeyName;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (HasCorrectVersion) break;
                    }

                    if (!HasCorrectVersion)
                    {
                        Message = $"{Name} installed on your machine is incompatible.";
                    }
                }
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(base.ToString());
            sb.AppendLine(pythonNet.ToString());
            return sb.ToString();
        }
    }
}
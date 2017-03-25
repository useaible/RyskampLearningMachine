using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RequirementsChecklistTool
{
    public class PythonNetChecker : FeatureChecker
    {
        const string REGISTRY_BASE = @"SOFTWARE\Python\PythonCore";

        public string PythonKeyPath { get; set; }

        public PythonNetChecker()
        {
            Name = "PythonNET";
            Url = "https://pypi.python.org/pypi/pythonnet";

            Versions.Add("2.3.0");
        }

        public override bool Check()
        {
            HasCorrectVersion = false;

            if (string.IsNullOrEmpty(PythonKeyPath))
            {
                Message = $"{Name} not found. Python is a prerequisite for {Name}.";
            }
            else
            {
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
            }

            return HasCorrectVersion;
        }

        private void CheckRegistry(RegistryKey localKey)
        {
            using (var rootKey = localKey.OpenSubKey($@"{REGISTRY_BASE}\{PythonKeyPath}\InstallPath"))
            {
                if (rootKey != null)
                {
                    object rootValue = rootKey.GetValue(null);
                    if (rootValue != null)
                    {
                        bool hasMatch = false;
                        string path = Path.Combine(rootValue.ToString(), "Lib", "site-packages");
                        var dir = new DirectoryInfo(path);
                        
                        foreach(var subDir in dir.GetDirectories())
                        {
                            if (subDir.Name.Contains("pythonnet"))
                            {
                                hasMatch = true;

                                foreach(var version in Versions)
                                {
                                    if (subDir.Name.Contains(version))
                                    {
                                        HasCorrectVersion = true;
                                        Message = $"{Name}...OK";
                                        break;
                                    }
                                }
                            }

                            if (HasCorrectVersion) break;
                        }

                        if (!HasCorrectVersion && hasMatch)
                        {
                            Message = $"{Name} installed on your machine is incompatible.";
                        }
                    }
                }
            }
        }
    }
}

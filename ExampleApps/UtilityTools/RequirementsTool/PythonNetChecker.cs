using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RequirementsTool
{
    public class PythonNetChecker : RegistryChecker
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
                HasCorrectVersion = base.Check();
            }

            return HasCorrectVersion;
        }
        
        protected override RegistryHive BaseKey { get; set; } = RegistryHive.CurrentUser;

        protected override void CheckRegistry(RegistryKey localKey)
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

                        if (dir != null && dir.Exists)
                        {
                            foreach (var subDir in dir.GetDirectories())
                            {
                                if (subDir.Name.Contains("pythonnet"))
                                {
                                    hasMatch = true;

                                    foreach (var version in Versions)
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
}

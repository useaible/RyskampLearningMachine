using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;

namespace RequirementsTool
{
    public class PythonChecker : RegistryChecker
    {
        const string REGISTRY_BASE = @"SOFTWARE\Python\PythonCore";

        public PythonNetChecker PythonNet { get; protected set; }

        public PythonChecker()
        {
            Name = "Python";
            Url = $"https://www.python.org/downloads";

            Versions.Add("3.5.0");
            Versions.Add("3.5.1");
            Versions.Add("3.5.2");
            Versions.Add("3.5.3");

            PythonNet = new PythonNetChecker();
        }

        public override bool Check()
        {
            HasCorrectVersion = base.Check();

            PythonNet.Check();

            return HasCorrectVersion;
        }
        
        protected override RegistryHive BaseKey { get; set; } = RegistryHive.CurrentUser;

        protected override void CheckRegistry(RegistryKey localKey)
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
                                    if (installedFeaturesKey != null)
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
                                                    PythonNet.PythonKeyPath = subKeyName;
                                                    break;
                                                }
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
            sb.Append(PythonNet.ToString());
            return sb.ToString();
        }
    }
}
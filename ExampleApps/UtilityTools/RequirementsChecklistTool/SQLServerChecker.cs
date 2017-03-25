using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;

namespace RequirementsChecklistTool
{
    public class SQLServerChecker : FeatureChecker
    {
        const string REGISTRY_BASE_FOR_INSTANCES = @"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL\";
        const string REGISTRY_BASE_FOR_SQLSERVER_INSTANCE_VERSION = @"SOFTWARE\Microsoft\Microsoft SQL Server\{{instanceName}}\MSSQLServer\CurrentVersion";
        const int SQLSERVER_VERSION_IDENTIFIER = 12; // 2014

        public SQLServerChecker()
        {
            Name = "MS SQL Server";
            Url = "https://www.microsoft.com/en-us/sql-server/sql-server-downloads";

            Versions.Add("SQL Server 2014 or later");
        }

        public override bool Check()
        {
            HasCorrectVersion = false;
            Message = $"{Name} is not installed on your machine.";

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

        private void CheckRegistry(RegistryKey localkey)
        {
            using (var rootKey = localkey.OpenSubKey(REGISTRY_BASE_FOR_INSTANCES))
            {
                if (rootKey != null)
                {
                    var instanceNames = rootKey.GetValueNames();
                    foreach (var instance in instanceNames)
                    {
                        string data = rootKey.GetValue(instance).ToString();
                        string instanceKeyName = REGISTRY_BASE_FOR_SQLSERVER_INSTANCE_VERSION.Replace("{{instanceName}}", data);

                        using (var instanceKey = localkey.OpenSubKey(instanceKeyName))
                        {
                            if (instanceKey != null)
                            {
                                object version = instanceKey.GetValue("CurrentVersion");
                                if (version != null)
                                {
                                    int versionIdentifier = Convert.ToInt32(version.ToString().Substring(0, 2));
                                    if (versionIdentifier >= SQLSERVER_VERSION_IDENTIFIER)
                                    {
                                        HasCorrectVersion = true;
                                        Message = $"{Name}...OK";
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (!HasCorrectVersion && instanceNames.Length > 0)
                    {
                        Message = $"{Name} installed on your machine is incompatible.";
                    }
                }
            }
        }
    }
}

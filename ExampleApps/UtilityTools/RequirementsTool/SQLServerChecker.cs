using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace RequirementsTool
{
    public class SQLServerChecker : RegistryChecker
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

        public string GetInstancesInfo()
        {
            var sb = new StringBuilder();

            var instances = SQLServerInstanceInfo.EnumerateSQLInstances();
            if (instances.Count() > 0)
            {
                sb.AppendLine($"{Name} instances found on your machine (localhost):");
                sb.Append("\tINSTANCE NAME\t\t");
                sb.Append("SQL SERVER EDITION\t\t");
                sb.Append("VERSION NUMBER\t\t");
                sb.AppendLine("SERVER NAME (for connection string)");
                foreach (var item in instances)
                {
                    sb.AppendLine($" {item.ToString()}");
                }
                sb.AppendLine();
                sb.AppendLine("Connection string templates:");
                sb.AppendLine($"\tw/ Integrated Security: {SQLServerInstanceInfo.CONN_STR_TEMPLATE}");
                sb.AppendLine($"\tw/ User and Password: {SQLServerInstanceInfo.CONN_STR_WITH_USER_TEMPLATE}");
                sb.AppendLine("NOTE: Replace placeholders (i.e, <your_server_name>, <your_user>, etc.) with appropriate values to connect to your SQL Server instance");
            }
            else
            {
                sb.Append("No SQL Server instances found");
            }

            return sb.ToString();
        }


        protected override RegistryHive BaseKey { get; set; } = RegistryHive.LocalMachine;

        protected override void CheckRegistry(RegistryKey localkey)
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

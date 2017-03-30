using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace RequirementsTool
{
    public class VSPythonToolsChecker : RegistryChecker
    {
        const string REGISTRY_BASE = @"Software\Microsoft\VisualStudio\{{vsVersionNum}}\PythonTools\Interpreters";

        public VSPythonToolsChecker()
        {
            Name = "Visual Studio Python Tools";
            Url = "https://microsoft.github.io/PTVS/";
        }

        public string VSVersionNum { get; set; }

        protected override RegistryHive BaseKey { get; set; } = RegistryHive.CurrentUser;

        protected override void CheckRegistry(RegistryKey localKey)
        {
            using (var rootKey = localKey.OpenSubKey(REGISTRY_BASE.Replace("{{vsVersionNum}}", VSVersionNum)))
            {
                if (rootKey != null)
                {
                    var interpreters = rootKey.GetSubKeyNames();
                    if (interpreters.Length > 0)
                    {
                        HasCorrectVersion = true;
                        Message = $"{Name}...OK";
                    }
                }
            }
        }
    }
}

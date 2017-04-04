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

            Versions.Add("2.2.0 or later");

            SetAdditionalHelpFormatted("If you have Visual Studio installed, you can get Python Tools through the Extension manager instead:",
                new string[] {
                    "On Visual Studio, go to the 'Tools' menu",
                    "Then, to the 'Extensions and Updates' submenu",
                    "Click on the 'Online' side tab and type in 'Python Tools' on the Search box",
                    "On the listed results, find 'Python Tools for Visual Studio' and hit Download"
                });
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

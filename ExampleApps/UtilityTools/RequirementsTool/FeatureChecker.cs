using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;

namespace RequirementsTool
{
    public abstract class FeatureChecker
    {
        public FeatureChecker()
        {
            Versions = new List<string>();
        }

        public virtual string Name { get; protected set; }
        public virtual string Url { get; protected set; }
        public virtual List<string> Versions { get; protected set; }
        public virtual bool HasCorrectVersion { get; protected set; }
        public abstract bool Check();

        protected virtual string Message { get; set; }
        protected virtual string AdditionalHelp { get; set; }

        protected virtual void SetAdditionalHelpFormatted(string head, params string[] steps)
        {
            var sb = new StringBuilder();
            sb.AppendLine(head);

            for (int i = 0; i < steps.Length; i++)
            {
                sb.AppendLine($"\t{i+1}. {steps[i]}");
            }

            AdditionalHelp = sb.ToString();
        }


        protected virtual string GetDetailedMessageInformation()
        {
            var sb = new StringBuilder();

            if (HasCorrectVersion)
            {
                sb.AppendLine(Message);
            }
            else
            {
                sb.AppendLine($"{Message} Please install any of these version(s):");
                foreach (var version in Versions)
                {
                    sb.AppendLine($" -> {version}");
                }
                sb.AppendLine($"Get {Name} here: {Url}");

                if (!string.IsNullOrEmpty(AdditionalHelp))
                    sb.Append(AdditionalHelp);
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            return GetDetailedMessageInformation();
        }
    }
}

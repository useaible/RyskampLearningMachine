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
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            return GetDetailedMessageInformation();
        }
    }
}

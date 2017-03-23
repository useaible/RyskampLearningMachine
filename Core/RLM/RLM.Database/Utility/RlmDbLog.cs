using Newtonsoft.Json;
using RLM.Database.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Database
{
    public enum RlmDbLogType
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public class RlmDbLog
    {
        public RlmDbLog() { }
        public RlmDbLog(string msg, string source, LogType type, Exception exception = null)
        {
            Message = msg;
            Source = source;
            Type = type;
            Date = DateTime.Now;
            if (exception != null)
            {
                Exception = JsonConvert.SerializeObject(exception, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented });
            }
        }

        public string Source { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
        public DateTime Date { get; set; }
        public LogType Type { get; set; }
        //public string StackTrace { get; set; }
        public string Task { get; set; }
    }
}

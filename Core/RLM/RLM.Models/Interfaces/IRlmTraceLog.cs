using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models.Interfaces
{
    public delegate void RlmTraceLogDelegate(string data);
    public interface IRlmTraceLog
    {
        event RlmTraceLogDelegate OnLog;
        void TraceLog(string data = "");
    }
}

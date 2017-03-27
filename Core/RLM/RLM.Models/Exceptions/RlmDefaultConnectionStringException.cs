using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models.Exceptions
{
    public class RlmDefaultConnectionStringException : Exception
    {
        public RlmDefaultConnectionStringException() { }
        public RlmDefaultConnectionStringException(string message) : base(message) { }
        public RlmDefaultConnectionStringException(string message, Exception inner) : base (message, inner) { }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Database.Utility
{
    public enum LogLevel
    {
        Production = 0, // logs Warning, Error & Critical msgs
        Test = 1, // logs Info + Production logs
        Debug = 2 // logs all
    }
}

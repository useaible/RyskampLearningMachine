using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Database
{
    public interface IRlmDbLogger
    {
        /// <summary>
        /// Detailed information, typically of interest only when diagnosing problems.
        /// </summary>
        void Debug(string msg, string dbName);

        /// <summary>
        /// Log traces and confirmation that things are working as expected.
        /// </summary>
        void Info(string msg);

        /// <summary>
        /// An indication that something unexpected happened, or indicative of some problem in the near future (e.g. ‘disk space low’). 
        /// The software is still working as expected.
        /// </summary>
        void Warning(string msg, string dbName, Exception ex = null);

        /// <summary>
        /// Due to a more serious problem, the software has not been able to perform some function. 
        /// </summary>
        void Error(Exception ex, string dbName);

        //void Error(Exception ex, useAIbleTask task);

        /// <summary>
        /// A serious error, indicating that the program itself may be unable to continue running. (e.g. DB connection problem)
        /// </summary>
        void Critical(Exception ex);
    }
}

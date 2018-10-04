using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Database.Utility
{
    public static class ConfigFile
    {
        public static LogLevel LogLevel
        {
            get
            {
                var retVal = LogLevel.Production;

                string strlogLvl = ConfigurationManager.AppSettings["RlmLogLevel"];
                if (!Enum.TryParse(strlogLvl, true, out retVal))
                {
                    retVal = LogLevel.Production; // defaults to Production if can't be parsed (due to setting not in config file or wrong value)
                }

                return retVal;
            }
        }
        
        public static string RlmLogLocation
        {
            get
            {
                var retVal = string.Empty;
                retVal = ConfigurationManager.AppSettings["RLMLogLocation"];
                if (string.IsNullOrEmpty(retVal))
                {
                    retVal = @"C:\RLM\Logs";
                }
                return retVal;
            }
        }

        public static bool DropDb
        {
            get
            {
                var retVal = false;

                string strlogLvl = ConfigurationManager.AppSettings["RLMDropDb"];
                if (!string.IsNullOrEmpty(strlogLvl))
                {
                    retVal = Convert.ToBoolean(ConfigurationManager.AppSettings["RLMDropDb"]);
                }

                return retVal;
            }
        }

        public static string BcpConfig
        {
            get
            {
                var retVal = string.Empty;

                retVal = ConfigurationManager.AppSettings["BcpConfig"];
                //if (string.IsNullOrEmpty(retVal))
                //{
                //    retVal = "-T";
                //}

                return retVal;
            }
        }

        public static string BcpPath
        {
            get
            {
                var retVal = string.Empty;

                retVal = ConfigurationManager.AppSettings["BcpPath"];
                if (string.IsNullOrEmpty(retVal))
                {
                    retVal = "bcp";
                }

                return retVal;
            }
        }
    }
}

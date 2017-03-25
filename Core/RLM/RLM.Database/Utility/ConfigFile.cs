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

                string strlogLvl = ConfigurationManager.AppSettings["useAIbleLogLevel"];
                if (!Enum.TryParse(strlogLvl, true, out retVal))
                {
                    retVal = LogLevel.Production; // defaults to Production if can't be parsed (due to setting not in config file or wrong value)
                }

                return retVal;
            }
        }
        public static LogLevel RlmDbLogLevel
        {
            get
            {
                var retVal = LogLevel.Production;

                string strlogLvl = ConfigurationManager.AppSettings["rnnDbLogLevel"];
                if (!Enum.TryParse(strlogLvl, true, out retVal))
                {
                    retVal = LogLevel.Debug; // defaults to Production if can't be parsed (due to setting not in config file or wrong value)
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
    }
}

using RLM.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RLM.WebAPI.Models
{
    public class RlmNetworkWebAPI : RlmNetwork
    {
        const int EXPIRES_AFTER = 15; // minutes

        private DateTime expiresOn = DateTime.Now.AddMinutes(EXPIRES_AFTER);

        public RlmNetworkWebAPI() { }
        public RlmNetworkWebAPI(IRlmDbData rlmDbData) : base(rlmDbData) { }

        
        public bool IsExpired
        {
            get
            {
                return DateTime.Now > expiresOn;
            }
        }

        public void ResetExpiry()
        {
            expiresOn = DateTime.Now.AddMinutes(EXPIRES_AFTER);
        }

        // TODO other utility properties to protect RlmNetwork state
    }
}
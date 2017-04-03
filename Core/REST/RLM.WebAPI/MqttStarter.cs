using RLM.WebAPI.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RLM.WebAPI
{
    public class MqttStarter
    {
        public static void Start()
        {
            RLMMqttManager manager = new RLMMqttManager();
        }
    }
}
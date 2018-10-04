using Newtonsoft.Json;
using RLM.SQLServer;
using RLM.Models;
using RLM.Models.Interfaces;
using RLM.WebAPI.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Exceptions;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace RLM.WebAPI.Manager
{
    internal class RLMMqttManager
    {
        private MqttClient MQTT_CLIENT;
        private  string TOPIC_UID;
        private RlmNetwork network;

        public RLMMqttManager()
        {
            MQTT_CLIENT = new MqttClient("dev.useaible.com");

            MQTT_CLIENT.Connect(Guid.NewGuid().ToString("N"));

            MQTT_CLIENT.Subscribe(new string[] { "init" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });

            MQTT_CLIENT.MqttMsgPublishReceived += MqttClient_MqttMsgPublishReceived;
        }

        private void publish(string topic, string message)
        {
            try
            {
                MQTT_CLIENT.Publish(topic, Encoding.UTF8.GetBytes(message), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
            }
            catch (MqttClientException e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
            catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }

        private void subscribe(string topic)
        {
            MQTT_CLIENT.Subscribe(new string[] { TOPIC_UID + "/" + topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        private void MqttClient_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string topic = e.Topic;
            string msg = Encoding.UTF8.GetString(e.Message);

            if(topic == "init")
            {
                TOPIC_UID = msg; //Get unique id from client

                subscribe("create_load_network");
                subscribe("configure_network");
                subscribe("start_session");
                subscribe("run_cycle");
                subscribe("score_cycle");
                subscribe("end_session");
                subscribe("sessions");
                subscribe("session_cases");
                subscribe("io_details");
                subscribe("disconnect");

                publish(msg + "/init_result", TOPIC_UID);
            }
            else if(topic == createTopic("create_load_network"))
            {
                CreateLoadNetworkParams data = JsonConvert.DeserializeObject<CreateLoadNetworkParams>(msg);
                IRlmDbData rlmDbData = new RlmDbDataSQLServer(data.RlmName);
                network = new RlmNetwork(rlmDbData);

                if(!network.LoadNetwork(data.NetworkName))
                {
                    IEnumerable<RlmIO> inputs = data.Inputs.Select(a => new RlmIO(a.IOName, a.DotNetType, a.Min, a.Max, a.InputType)).ToList();
                    IEnumerable<RlmIO> outputs = data.Outputs.Select(a => new RlmIO(a.IOName, a.DotNetType, a.Min, a.Max)).ToList();

                    network.NewNetwork(data.NetworkName, inputs, outputs);
                }

                publish(createTopic("create_network_result"), "Network successfully loaded!");
            }
            else if(topic == createTopic("configure_network"))
            {
                RlmSettingsParams data = JsonConvert.DeserializeObject<RlmSettingsParams>(msg);

                network.NumSessions = data.NumSessions;
                network.StartRandomness = data.StartRandomness;
                network.EndRandomness = data.EndRandomness;
                network.MaxLinearBracket = data.MaxLinearBracket;
                network.MinLinearBracket = data.MinLinearBracket;

                publish(createTopic("configure_result"), "Network successfully configured!");
            }
            else if(topic == createTopic("start_session"))
            {
                long sessionId = network.SessionStart();

                publish(createTopic("start_session_result"), sessionId.ToString());
            }
            else if(topic == createTopic("run_cycle"))
            {
                RunCycleParams data = JsonConvert.DeserializeObject<RunCycleParams>(msg);
                var retVal = new CycleOutputParams();


                var inputsCycle = new List<RlmIOWithValue>();
                foreach (var ins in data.Inputs)
                {
                    inputsCycle.Add(new RlmIOWithValue(network.Inputs.Where(item => item.Name == ins.IOName).First(), ins.Value));
                }

                var Cycle = new RlmCycle();
                RlmCyclecompleteArgs cycleOutput = null;

                // supervised training
                if (data.Outputs != null && data.Outputs.Count > 0)
                {
                    var outputsCycle = new List<RlmIOWithValue>();
                    foreach (var outs in data.Outputs)
                    {
                        outputsCycle.Add(new RlmIOWithValue(network.Outputs.First(a => a.Name == outs.IOName), outs.Value));
                    }

                    cycleOutput = Cycle.RunCycle(network, network.CurrentSessionID, inputsCycle, data.Learn, outputsCycle);
                }
                else // unsupervised training
                {
                    cycleOutput = Cycle.RunCycle(network, network.CurrentSessionID, inputsCycle, data.Learn);
                }

                if (cycleOutput != null)
                {
                    retVal = new CycleOutputParams { RlmType = cycleOutput.RlmType, CycleId = cycleOutput.CycleOutput.CycleID };
                    var outputs = cycleOutput.CycleOutput.Outputs;
                    for (int i = 0; i < outputs.Count(); i++)
                    {
                        RlmIOWithValue output = outputs.ElementAt(i);
                        retVal.Outputs.Add(new RlmIOWithValuesParams() { IOName = output.Name, Value = output.Value });
                    }
                }

                var resultStr = JsonConvert.SerializeObject(retVal);

                publish(createTopic("run_cycle_result"), resultStr);
            }
            else if(topic == createTopic("score_cycle"))
            {
                ScoreCycleParams data = JsonConvert.DeserializeObject<ScoreCycleParams>(msg);

                network.ScoreCycle(data.CycleID, data.Score);

                publish(createTopic("score_cycle_result"), "Scoring cycle...");
            }
            else if(topic == createTopic("end_session"))
            {
                SessionEndParams data = JsonConvert.DeserializeObject<SessionEndParams>(msg);

                network.SessionEnd(data.SessionScore);

                publish(createTopic("end_session_result"), "Session ended!");
            }
            else if(topic == createTopic("sessions"))
            {
                dynamic data = JsonConvert.DeserializeObject<dynamic>(msg);


                string dbName = (String)data.RlmName;
                bool withSignificantLearning = Convert.ToBoolean(((String)data.WithLearning).ToLower());
                int? skip = Convert.ToInt32(((Int32)data.Skip));
                int? take = Convert.ToInt32(((Int32)data.Take));

                if(skip == 0)
                {
                    skip = null;
                }

                if(take == 0)
                {
                    take = null;
                }

                string resultStr = "";
                if(withSignificantLearning)
                {
                    resultStr = JsonConvert.SerializeObject(getSessionsWithSignificantLearning(new RlmFilterResultParams { Skip = skip, Take = take, RlmName = dbName }));
                }
                else
                {
                    resultStr = JsonConvert.SerializeObject(getSessionHistory(new RlmFilterResultParams { Skip = skip, Take = take, RlmName = dbName }));
                }

                publish(createTopic("sessions_result"), resultStr);
            }
            else if(topic == createTopic("session_cases"))
            {
                RlmGetSessionCaseParams data = JsonConvert.DeserializeObject<RlmGetSessionCaseParams>(msg);

                RlmSessionCaseHistory hist = network.SessionCaseHistory;

                var resultStr = JsonConvert.SerializeObject(hist.GetSessionCaseHistory(data.SessionId, data.Skip, data.Take));

                publish(createTopic("session_cases_result"), resultStr);
            }
            else if(topic == createTopic("io_details"))
            {
                RlmGetCaseIOParams data = JsonConvert.DeserializeObject<RlmGetCaseIOParams>(msg);

                RlmSessionCaseHistory hist = network.SessionCaseHistory;

                var resultStr = JsonConvert.SerializeObject(hist.GetCaseIOHistory(data.CaseId, data.RneuronId, data.SolutionId));

                publish(createTopic("io_details_result"), resultStr);
            }
            else if(topic == createTopic("disconnect"))
            {
                if(MQTT_CLIENT != null && MQTT_CLIENT.IsConnected)
                {
                    MQTT_CLIENT.Disconnect();
                }
            }
        }

        private IEnumerable<RlmSessionHistory> getSessionHistory(RlmFilterResultParams data)
        {
            RlmSessionCaseHistory hist = network.SessionCaseHistory;

            return hist.GetSessionHistory(data.Skip, data.Take);
        }

        private IEnumerable<RlmSessionHistory> getSessionsWithSignificantLearning(RlmFilterResultParams data)
        {
            RlmSessionCaseHistory hist = network.SessionCaseHistory;

            return hist.GetSignificantLearningEvents(data.Skip, data.Take);
        }

        private  string createTopic(string name)
        {
            return TOPIC_UID + "/" + name;
        }
    }
}
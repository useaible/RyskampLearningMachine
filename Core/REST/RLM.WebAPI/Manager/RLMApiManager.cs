using RLM.Models;
using RLM.WebAPI.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Web;

namespace RLM.WebAPI.Manager
{
    internal class RLMApiManager
    {
        private static readonly ConcurrentDictionary<string, RlmNetworkWebAPI> RLM_NETWORKS = new ConcurrentDictionary<string, RlmNetworkWebAPI>();
        private static readonly Timer RLM_PURGE_TIMER = new Timer(60 * 1000); // every 1 minute

        static RLMApiManager()
        {
            StartPurger();
        }
        
        private static void StartPurger()
        {
            RLM_PURGE_TIMER.Elapsed += (obj, e) =>
            {
                var expiredNetworks = RLM_NETWORKS.Where(a => a.Value.IsExpired).Select(a => a.Key).ToList();
                foreach (var item in expiredNetworks)
                {
                    RlmNetworkWebAPI temp;
                    RLM_NETWORKS.TryRemove(item, out temp);
                }
            };
            RLM_PURGE_TIMER.Start();
        }

        public void CreateOrLoadNetwork(CreateLoadNetworkParams data)
        {
            RlmNetworkWebAPI network = new RlmNetworkWebAPI(data.RlmName);
            
            if (network.LoadNetwork(data.NetworkName))
            {
                RlmNetworkWebAPI cache;
                if (RLM_NETWORKS.TryGetValue(data.RlmName, out cache))
                {
                    cache = network;
                }
                else
                {
                    RLM_NETWORKS.TryAdd(data.RlmName, cache = network);
                }

                cache.ResetExpiry();
            }
            else
            {
                IEnumerable<RlmIO> inputs = data.Inputs.Select(a => new RlmIO(a.IOName, a.DotNetType, a.Min, a.Max, a.InputType)).ToList();
                IEnumerable<RlmIO> outputs = data.Outputs.Select(a => new RlmIO(a.IOName, a.DotNetType, a.Min, a.Max)).ToList();

                network.NewNetwork(data.NetworkName, inputs, outputs);
                RLM_NETWORKS.TryAdd(data.RlmName, network);
            }
        }

        public void SetRLMSettings(RlmSettingsParams data)
        {
            RlmNetworkWebAPI network = LoadNetworkFromCache(data);

            network.NumSessions = data.NumSessions;
            network.StartRandomness = data.StartRandomness;
            network.EndRandomness = data.EndRandomness;
            network.MaxLinearBracket = data.MaxLinearBracket;
            network.MinLinearBracket = data.MinLinearBracket;
        }

        public long StartSession(RlmParams data)
        {
            long retVal = 0;

            RlmNetworkWebAPI network = LoadNetworkFromCache(data);
            retVal = network.SessionStart();

            return retVal;
        }

        public CycleOutputParams RunCycle(RunCycleParams data)
        {
            var retVal = new CycleOutputParams();

            RlmNetworkWebAPI network = LoadNetworkFromCache(data);

            var inputsCycle = new List<RlmIOWithValue>();
            foreach (var ins in data.Inputs)
            {
                inputsCycle.Add(new RlmIOWithValue(network.Inputs.Where(item => item.Name == ins.IOName).First(), ins.Value));
            }

            var Cycle = new RlmCycle();
            RlmCyclecompleteArgs cycleOutput = null;

            // supervised training
            if (data.Outputs == null ||
                (data.Outputs != null && data.Outputs.Count > 0))
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
                        
            return retVal;
        }

        public void ScoreCycle(ScoreCycleParams data)
        {
            RlmNetworkWebAPI network = LoadNetworkFromCache(data);
            network.ScoreCycle(data.CycleID, data.Score);
        }

        public void EndSession(SessionEndParams data)
        {
            RlmNetworkWebAPI network = LoadNetworkFromCache(data);
            network.SessionEnd(data.SessionScore);
        }

        public IEnumerable<RlmSessionHistory> GetSessions(bool withLearning, RlmFilterResultParams data)
        {
            if(withLearning)
            {
                return getSessionsWithSignificantLearning(data);
            }
            else
            {
                return getSessionHistory(data);
            }
        }

        private IEnumerable<RlmSessionHistory> getSessionHistory(RlmFilterResultParams data)
        {
            RlmSessionCaseHistory hist = new RlmSessionCaseHistory(data.RlmName);

            return hist.GetSessionHistory(data.Skip, data.Take);
        }

        private IEnumerable<RlmSessionHistory> getSessionsWithSignificantLearning(RlmFilterResultParams data)
        {
            RlmSessionCaseHistory hist = new RlmSessionCaseHistory(data.RlmName);

            return hist.GetSignificantLearningEvents(data.Skip, data.Take);
        }

        public IEnumerable<RlmCaseHistory> GetSessionCases(RlmGetSessionCaseParams data)
        {
            RlmSessionCaseHistory hist = new RlmSessionCaseHistory(data.RlmName);

            return hist.GetSessionCaseHistory(data.SessionId, data.Skip, data.Take);
        }

        public RlmCaseIOHistory GetCaseInputOutputDetails(RlmGetCaseIOParams data)
        {
            RlmSessionCaseHistory hist = new RlmSessionCaseHistory(data.RlmName);

            return hist.GetCaseIOHistory(data.CaseId, data.RneuronId, data.SolutionId);
        }

        public long? GetNextPrevLearnedCaseId(RlmGetNextPrevLearnedCaseIdParams data)
        {
            RlmSessionCaseHistory hist = new RlmSessionCaseHistory(data.RlmName);
            return hist.GetNextPreviousLearnedCaseId(data.CaseId.Value, data.IsNext);
        }

        public IEnumerable<RlmLearnedSessionDetails> GetSessionIODetails(RlmGetSessionDetailsParams data)
        {
            RlmSessionCaseHistory hist = new RlmSessionCaseHistory(data.RlmName);
            return hist.GetSessionIODetails(data.SessionIds);
        }

        public long? GetRneuronIdFromInputs(RlmGetRneuronIdFromInputs data)
        {
            RlmSessionCaseHistory hist = new RlmSessionCaseHistory(data.RlmName);
            return hist.GetRneuronIdFromInputs(data.InputValuesPair);
        }

        public long? GetSolutionIdFromOutputs(RlmGetSolutionIdFromOutputs data)
        {
            RlmSessionCaseHistory hist = new RlmSessionCaseHistory(data.RlmName);
            return hist.GetSolutionIdFromOutputs(data.OutputValuesPair);
        }

        public IEnumerable<RlmLearnedCase> GetLearnedCases(RlmGetLearnedCasesParams data)
        {
            RlmSessionCaseHistory hist = new RlmSessionCaseHistory(data.RlmName);
            return hist.GetLearnedCases(data.RneuronId, data.SolutionId, data.Scale);
        }

        public IEnumerable<RlmLearnedCaseDetails> GetCaseDetails(RlmGetCaseDetailsParams data)
        {
            RlmSessionCaseHistory hist = new RlmSessionCaseHistory(data.RlmName);
            return hist.GetCaseDetails(data.CaseId);
        }

        public IEnumerable<RlmIODetails>[] GetCaseIODetails(RlmGetCaseIODetailsParams data)
        {
            RlmSessionCaseHistory hist = new RlmSessionCaseHistory(data.RlmName);
            return hist.GetCaseIODetails(data.CaseId);
        }

        private RlmNetworkWebAPI LoadNetworkFromCache(RlmParams data)
        {
            RlmNetworkWebAPI retVal = null;

            if (RLM_NETWORKS.TryGetValue(data.RlmName, out retVal))
            {
                if (retVal.CurrentNetworkName != data.NetworkName)
                {
                    throw new ArgumentException($"Network '{data.NetworkName}' not found or has not been loaded yet. Please check the network name or make sure to call the CreateOrLoad API method first.");
                }
            }
            else
            {
                throw new ArgumentException($"RLM '{data.RlmName}' not found or has not been loaded yet. Please check the network name or make sure to call the CreateOrLoad API method first.");
            }

            retVal.ResetExpiry();

            return retVal;
        }
    }
}
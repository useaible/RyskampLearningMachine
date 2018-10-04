using RLM.Enums;
using RLM.Models;
using RLM.Models.Interfaces;
using RLM.Models.Optimizer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RLM
{   
    public class RlmOptimizer : IRlmTraceLog, IDisposable
    {
        private RlmNetwork network;
        private RlmFormulaCompiler compiler = null;
        private bool stopTraining = false;
        private ManualResetEvent dataPersistEvent = new ManualResetEvent(false);
        public double LastScore { get; set; } = double.MinValue;
        public double HighScore { get; set; } = double.MinValue;
        public bool AllowDuplicates { get; set; } = false;
        //public List<string> SessionData { get; set; } = new List<string>();
        //public List<string> PredictData { get; set; } = new List<string>();
        public IRlmDbData RlmDbData { get; set; }
        public IRlmRneuronProcessor Gpu { get; set; }


        public RlmOptimizer(IRlmDbData rlmDbData, IRlmRneuronProcessor gpu = null)
        {
            Resources = new Dictionary<string, Resource>();
            ResourceAttributes = new Dictionary<string, ResourceAttribute>();
            Constraints = new Dictionary<string, Constraint>();
            CycleOutputs = new Dictionary<string, object>();
            SessionOutputs = new Dictionary<string, List<object>>();
            CycleInputs = new Dictionary<string, object>();

            TrainingVariables.Add("CycleScore", new TrainingVariable() { Name = "CycleScore" });
            TrainingVariables.Add("SessionScore", new TrainingVariable() { Name = "SessionScore" });

            //if (!string.IsNullOrEmpty(databaseName))
            //{ 
            //    DatabaseName = databaseName;
            //}            

            RlmDbData = rlmDbData;
            DatabaseName = rlmDbData.DatabaseName;

            if (gpu != null)
            {
                Gpu = gpu;
            }
        }

        public event DataPersistenceCompleteDelegate DataPersistenceDone;
        public event RlmTraceLogDelegate OnLog;

        public IDictionary<string, TrainingVariable> TrainingVariables = new Dictionary<string, TrainingVariable>();

        public IDictionary<string, Resource> Resources { get; set; }
        public IDictionary<string, ResourceAttribute> ResourceAttributes { get; set; }
        public IDictionary<string, Constraint> Constraints { get; set; }
        public ScoringPhase CyclePhase { get; set; }
        public ScoringPhase SessionPhase { get; set; }
        public IDictionary<string, object> CycleInputs { get; set; }
        public IDictionary<string, object> CycleOutputs { get; set; }
        public IDictionary<string, List<object>> SessionOutputs { get; set; }
        public void ParseFormula() { }
        public void UploadResources() { }
        public RlmSettings Settings { get; set; }
        public string DatabaseName { get; private set; }
        

        public void RemoveSolutionCascade(IDictionary<string, IEnumerable<string>> outputs)
        {
            if (network != null)
            {
                TraceLog("Waiting for Data Persistence to finish...");
                dataPersistEvent.WaitOne();

                // find actual output id based on resources name (key)
                var outputList = new Dictionary<long, IEnumerable<string>>();
                foreach(var pair in outputs)
                {
                    Resource res = Resources[pair.Key];
                    var rlmOutput = network.Outputs.FirstOrDefault(a => a.Name == res.Name);
                    outputList[rlmOutput.ID] = pair.Value;
                }

                network.RemoveSolutionsCascade(outputList);
                TraceLog("Cascade removal done.");
            }
        }

        public void StopTraining()
        {            
            stopTraining = true;
        }

        public IDictionary<int, RlmCyclecompleteArgs> StartTraining(RlmSettings settings, CancellationToken? token = null)
        {
            dataPersistEvent.Reset();
            //stopTraining = false;
            //settings = this.Settings;
            this.Settings = settings;

            //Get RLM Settings from user
            int startRandomness = settings.StartRandomness;
            int endRandomness = settings.EndRandomness;
            int maxBracket = settings.MaxLinearBracket;
            int minBracket = settings.MinLinearBracket;
            RlmSimulationType simType = settings.SimulationType;
            TimeSpan time = settings.Time;
            double target = settings.SimulationTarget;

            // clear previous network, if any
            ClearNetwork();

            IDictionary<int, RlmCyclecompleteArgs> cycleOutputDic = null;
            network = (string.IsNullOrEmpty(DatabaseName) ? new RlmNetwork() : new RlmNetwork(RlmDbData, true));

            network.DataPersistenceComplete += Network_DataPersistenceComplete;

            network.StartRandomness = startRandomness;
            network.EndRandomness = endRandomness;
            network.MaxLinearBracket = maxBracket;
            network.MinLinearBracket = minBracket;

            List<RlmIO> inputs = new List<RlmIO>();
            List<RlmIO> outputs = new List<RlmIO>();

            //get inputs/outputs from resources
            var resourceInputs = Resources.Where(a => a.Value.RLMObject == RLMObject.Input);
            var resourceOutputs = Resources.Where(a => a.Value.RLMObject == RLMObject.Output);

            //set rlm inputs/outputs
            inputs = GetRlmIOFromResource(resourceInputs);
            outputs = GetRlmIOFromResource(resourceOutputs);

            //create network
            if (!network.LoadNetwork())
            {
                network.NewNetwork("RlmOptimizer", inputs, outputs);
            }

            //start training
            int sessions = settings.SimulationType == RlmSimulationType.Sessions ? Convert.ToInt32(settings.SimulationTarget) : settings.NumOfSessionsReset;
            double hours = settings.SimulationType == RlmSimulationType.Time ? settings.Time.TotalHours : 0;
            DateTime endsOn = DateTime.Now.AddHours(hours);

            TraceLog($"RLM Settings: \n\n" +
                $"Randomness: Max = {startRandomness}, End = {endRandomness}\nLinear Bracket: Max = {maxBracket}, Min = {minBracket}\n" +
                $"Simulation Type: {simType}\nTarget: {target}\nTime: {hours}\n" +
                $"Cycle Score Formula: {string.Join(" | ", CyclePhase.Formula)}\n" +
                $"Session Score Formula: {string.Join(" | ", SessionPhase.Formula)}");
            TraceLog();

            int scoreHits = 0;
            bool timesUp = false;
            //for (var trial = 1; trial <= 5; trial++)
            //{
            //TrainingLog($"\nTrial #{trial}");

            do
            {
                //settings
                network.NumSessions = sessions;
                network.StartRandomness = startRandomness;
                network.EndRandomness = endRandomness;
                network.MaxLinearBracket = maxBracket;
                network.MinLinearBracket = minBracket;

                network.ResetRandomizationCounter();

                if ((simType == RlmSimulationType.Time && DateTime.Compare(DateTime.Now, endsOn) == 1) || token?.IsCancellationRequested == true)
                {
                    timesUp = true;
                }

                if (!timesUp)
                {
                    TraceLog("\nTraining...\n");
                    for (int i = 1; i <= sessions; i++)
                    {
                        cycleOutputDic = RunOneSession(network, resourceInputs, true);
                        if (settings.SimulationType == RlmSimulationType.Score)
                        {
                            if (UpdateScoreHit(ref scoreHits, settings) || token?.IsCancellationRequested == true)
                            {
                                break;
                            }
                        }

                        if ((simType == RlmSimulationType.Time && DateTime.Compare(DateTime.Now, endsOn) == 1) || token?.IsCancellationRequested == true)
                        {
                            timesUp = true;
                            break;
                        }

                        if (stopTraining || token?.IsCancellationRequested == true)
                        {
                            break;
                        }
                    }
                }

                //predict
                TraceLog("\nPredicting...\n");
                if (settings.SimulationType == RlmSimulationType.Score)
                {
                    for (int i = 1; i <= settings.NumScoreHits; i++)
                    {
                        cycleOutputDic = RunOneSession(network, resourceInputs, false, (i == settings.NumScoreHits));
                        if (UpdateScoreHit(ref scoreHits, settings) || token?.IsCancellationRequested == true)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    cycleOutputDic = RunOneSession(network, resourceInputs, false, true);
                }

                if (stopTraining || token?.IsCancellationRequested == true)
                {
                    break;
                }

            } while ((!timesUp && simType == RlmSimulationType.Time) ||
                (simType == RlmSimulationType.Score && settings.NumScoreHits > scoreHits));
            //}

            //end training
            network.TrainingDone();

            if (simType == RlmSimulationType.Score)
            {
                TraceLog($"Score Hits: {scoreHits}");
            }

            TraceLog("Optimization Done");

            return cycleOutputDic;
        }

        private void Network_DataPersistenceComplete()
        {
            TraceLog("Data Persistence done.");
            dataPersistEvent.Set();
            DataPersistenceDone?.Invoke();
        }

        private bool UpdateScoreHit(ref int currentHits, RlmSettings settings)
        {
            bool retVal = false;

            if (TrainingVariables["SessionScore"].Value >= settings.SimulationTarget && settings.SimulationType == RlmSimulationType.Score)
            {
                currentHits++;
                if (currentHits >= settings.NumScoreHits)
                {
                    retVal = true;
                }
            }
            else
            {
                currentHits = 0;
            }

            return retVal;
        }

        private IDictionary<int, RlmCyclecompleteArgs> RunOneSession(RlmNetwork network, IEnumerable<KeyValuePair<string, Resource>> resourceInputs, bool learn = true, bool showCycleOutput = false)
        {
            CycleOutputs.Clear();
            SessionOutputs.Clear();

            IDictionary<int, RlmCyclecompleteArgs> cycleOutputDic = new Dictionary<int, RlmCyclecompleteArgs>();
            TrainingVariables["SessionScore"].Value = 0;

            long sessId = network.SessionStart();
            List<object> outputList = new List<object>();

            //TODO: check later for multiple inputs
            int min = 0;
            int max = 0;
            var resIn = resourceInputs.First();
            Resource res = resIn.Value;
            Dictionary<string, int> inputRange = GetInputRange(res);
            min = inputRange["Min"];
            max = inputRange["Max"];

            bool usedExcludeSolutions = false;
            for (int j = min; j <= max; j++)
            {
                RlmCyclecompleteArgs result;
                List<long> excludeSolutions = null;

                while (true)
                {
                    //Populate input values
                    var invs = new List<RlmIOWithValue>();
                    foreach (var a in network.Inputs)
                    {
                        invs.Add(new RlmIOWithValue(a, j.ToString()));
                        CycleInputs[a.Name] = j;
                    }

                    //Build and run a new RlmCycle
                    var Cycle = new RlmCycle();
                    result = Cycle.RunCycle(network, sessId, invs, learn, excludeSolutions: excludeSolutions);

                    //TODO: check later for multiple outputs
                    var rlmOut = network.Outputs.First();
                    var value = result.CycleOutput.Outputs.First(b => b.Name == rlmOut.Name).Value;
                                        
                    if (!AllowDuplicates && !learn)
                    {
                        bool hasDup = false;
                        foreach (var rlmOutputs in SessionOutputs)
                        {
                            if (rlmOutputs.Value.Any(a => a.ToString() == value))
                            {
                                if (excludeSolutions == null)
                                    excludeSolutions = new List<long>();

                                excludeSolutions.Add(result.CycleOutput.SolutionID);
                                hasDup = true;
                                System.Diagnostics.Debug.WriteLine("Duplicate found!");
                                break;
                            }
                        }

                        if (hasDup)
                        {
                            usedExcludeSolutions = true;
                            continue;
                        }
                    }

                    //set current rn and sl
                    cycleOutputDic[j] = result;

                    CycleOutputs[rlmOut.Name] = value;

                    outputList.Add(value);
                    SessionOutputs[rlmOut.Name] = outputList;

                    break;
                }

                double cycleScore = ScoreCycle();


                if (showCycleOutput)
                {
                    //if (j == min && PredictData.Count() > min)
                    //{
                    //    PredictData.Clear();
                    //}
                    //// exposed cyclescore on predict
                    //PredictData.Add(cycleScore.ToString());
                }

                network.ScoreCycle(result.CycleOutput.CycleID, cycleScore);

                TrainingVariables["CycleScore"].Value = 0;
            }

            double sessionScore = ScoreSession();
            LastScore = sessionScore;
            if (LastScore > HighScore)
            {
                HighScore = LastScore;

            }
            network.SessionEnd(sessionScore);

            TraceLogObjectsIf(!learn && !usedExcludeSolutions && sessionScore < HighScore, $"{network.DatabaseName}_BestSolutions_InMemory", network.MemoryManager.BestSolutions.SelectMany(a => a.Value.Values));
            TraceLog($"Score: {string.Format("{0:n}", sessionScore)}");

            // exposed session data
            //SessionData.Add(sessionScore.ToString());

            return cycleOutputDic;
        }

        private Dictionary<string, int> GetInputRange(Resource res)
        {
            int min = 0;
            int max = 0;
            switch (res.Type)
            {
                case Category.Constant:
                    min = 0;
                    max = 1;
                    break;
                case Category.Range:
                    min = Convert.ToInt32(res.Min);
                    max = Convert.ToInt32(res.Max);
                    break;
                case Category.Data:
                    min = 0;
                    max = res.DataObjDictionary.Count;
                    break;
            }

            Dictionary<string, int> retVal = new Dictionary<string, int>();
            retVal.Add("Min", min);
            retVal.Add("Max", max);

            return retVal;
        }

        private double ScoreCycle()
        {
            double retVal = 0;

            compiler = new RlmFormulaCompiler(this);
            //foreach (var cons in CyclePhase.Constraints)
            //{
            //    if (Convert.ToBoolean(compiler.Parse(cons)))
            //    {
            //        TrainingVariables["CycleScore"].Value += cons.SuccessScore;
            //    }
            //    else
            //    {
            //        TrainingVariables["CycleScore"].Value += cons.FailScore;
            //    }
            //}
            
            //retVal = TrainingVariables["CycleScore"].Value = TrainingVariables["CycleScore"].Value + cycleScoreFormula;

            retVal = Convert.ToDouble(compiler.Parse(CyclePhase));

            return retVal;
        }

        private double ScoreSession()
        {
            double retVal = 0;

            compiler = new RlmFormulaCompiler(this);
            retVal = Convert.ToDouble(compiler.Parse(SessionPhase));

            return retVal;
        }

        private List<RlmIO> GetRlmIOFromResource(IEnumerable<KeyValuePair<string, Resource>> resourceInputs)
        {
            List<RlmIO> retVal = new List<RlmIO>();
            foreach (var input in resourceInputs)
            {
                Resource res = input.Value;

                RlmIO rlmio = null;
                switch (res.Type)
                {
                    case Category.Constant:
                    case Category.Variable:

                        rlmio = new RlmIO(res.Name, res.DataType, 0, 0);
                        if (res.RlmInputType != null)
                        {
                            rlmio = new RlmIO(res.Name, res.DataType, 0, 0, res.RlmInputType.Value);
                        }

                        break;
                    case Category.Range:

                        rlmio = new RlmIO(res.Name, res.DataType, res.Min, res.Max);
                        if(res.RlmInputType != null)
                        {
                            rlmio = new RlmIO(res.Name, res.DataType, res.Min, res.Max, res.RlmInputType.Value);
                        }

                        break;
                    case Category.Data:

                        var data = res.DataObjDictionary;
                        rlmio = new RlmIO(res.Name, res.DataType, 0, data.Count - 1);
                        if(res.RlmInputType != null)
                        {
                            rlmio = new RlmIO(res.Name, res.DataType, 0, data.Count - 1, res.RlmInputType.Value);
                        }

                        break;
                }

                retVal.Add(rlmio);
            }

            return retVal;
        }

        private void TraceLogObjectsIf<T>(bool condition, string name, IEnumerable<T> objects)
        {
            if (condition)
            {
                string location = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string dirName = Path.GetDirectoryName(location);
                string filepath = Path.Combine(dirName, $@"AppLogs\{name}_{DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss_fff")}.csv");
                FileInfo fileInfo = new FileInfo(filepath);
                fileInfo.Directory.Create();

                using (var writer = new StreamWriter(filepath))
                {
                    var type = typeof(T);
                    var properties = type.GetProperties();

                    // headers
                    var lastProp = properties.Last();
                    foreach(var prop in properties)
                    {
                        writer.Write(prop.Name);
                        if (!lastProp.Equals(prop))
                        {
                            writer.Write(",");
                        }
                    }
                    writer.WriteLine();

                    foreach (var item in objects)
                    {
                        foreach (var prop in properties)
                        {
                            string value = prop.GetValue(item).ToString();
                            writer.Write(value);
                            if (!lastProp.Equals(prop))
                            {
                                writer.Write(",");
                            }
                        }
                        writer.WriteLine();
                    }
                }
            }
        }

        public void TraceLog(string data = "")
        {
            OnLog?.Invoke(data);
        }

        public void Dispose()
        {
            ClearNetwork();

            if (compiler != null)
            {
                compiler.Dispose();
                compiler = null;
            }

            dataPersistEvent?.Close();
        }

        private void ClearNetwork()
        {
            if (network != null)
            {
                network.DataPersistenceComplete -= Network_DataPersistenceComplete;
                network.Dispose();
                network = null;
            }
        }
    }
}

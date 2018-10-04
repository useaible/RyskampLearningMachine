using RLM.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RLM.Models;
using System.Collections.Concurrent;
using RLM.Database;
using System.Threading;
using RLM.Database.Utility;
using System.Diagnostics;
using RLM.Memory.CPU;

namespace RLM.Memory
{
    public class Manager : IManager
    {
        private const int MAX_ALLOC = 100000;

        private BlockingCollection<Session> bcSessionsToCreate;
        private BlockingCollection<Session> bcSessionsToUpdate;
        private BlockingCollection<Case> bcCasesQueue;
        private RlmDbMgr rlmDb;
        private RlmObjectEnqueuer rlmDbEnqueuer;
        
        private bool trainingDone = false;
        private bool sessionsDone = false;
        private int totalSessionsCount = 0;
        private Task sessionCreateTask;
        private CancellationTokenSource workerTokenSrc = new CancellationTokenSource();
        private Task sessionUpdateTask;
        private Task caseTask;
        private int iConcurrencyLevel = Environment.ProcessorCount;
        private int rneuronsBoundedCapacity;
        private int solutionsBoundedCapacity;

        private System.Diagnostics.Stopwatch dbSavingTime = new System.Diagnostics.Stopwatch();
        private System.Timers.Timer progressUpdater = new System.Timers.Timer();
        private double lastProgress = -1;

        const int ARRAY_SIZE = 1000;
        private RlmArray<long> rneuronIds = new RlmArray<long>(ARRAY_SIZE);
        private RlmArray<double>[] doubleInputs;
        private RlmArray<bool>[] results;
        private double[][] doubleInputsArray = new double[3][];
        private bool[][] resultsArray = new bool[3][];
        private double[] fromArray = new double[3];
        private double[] toArray = new double[3];
        private bool[] rneuronsCacheArray;
        public IRlmRneuronProcessor rneuronProcessor { get; private set; }


        public bool GPUMode { get; private set; } = false;

        // temp for benchmarks
        public uint CacheBoxCount { get; set; } = 0;
        public List<TimeSpan> GetRneuronTimes { get; set; }
        public List<TimeSpan> RebuildCacheboxTimes { get; set; }
        public System.Diagnostics.Stopwatch SwGetRneuron { get; set; }
        public System.Diagnostics.Stopwatch SwRebuildCache { get; set; }
        // temp for benchmarks


        public event DataPersistenceCompleteDelegate DataPersistenceComplete;
        public event DataPersistenceProgressDelegate DataPersistenceProgress;
        
        public IRlmNetwork Network { get; private set; }
        public SortedList<RlmInputKey, RlmInputValue> DynamicInputs { get; set; }
        public ConcurrentDictionary<long, HashSet<SolutionOutputSet>> DynamicOutputs { get; set; } = new ConcurrentDictionary<long, HashSet<SolutionOutputSet>>();
        public ConcurrentDictionary<long, Rneuron> Rneurons { get; set; } = new ConcurrentDictionary<long, Rneuron>();
        public ConcurrentDictionary<long, Session> Sessions { get; set; } = new ConcurrentDictionary<long, Session>();
        public ConcurrentQueue<Session> SessionsQueueToCreate { get; set; } = new ConcurrentQueue<Session>();
        public ConcurrentQueue<Session> SessionsQueueToUpdate { get; set; } = new ConcurrentQueue<Session>();
        public ConcurrentDictionary<long, Solution> Solutions { get; set; } = new ConcurrentDictionary<long, Solution>();
        public ConcurrentDictionary<long, Dictionary<long, BestSolution>> BestSolutions { get; set; } = new ConcurrentDictionary<long, Dictionary<long, BestSolution>>();
        public HashSet<BestSolution> BestSolutionStaging { get; set; } = new HashSet<BestSolution>();
        public BestSolutionCacheBox CacheBox { get; set; } = new BestSolutionCacheBox();

        public double MomentumAdjustment { get; set; } = 25;
        public double CacheBoxMargin { get; set; } = 0;
        public bool UseMomentumAvgValue { get; set; } = false;

        // benchmark stats storage
        //private Stopwatch globalExecutionStopWatch = new Stopwatch();
        //private Stopwatch getBest_getRneuron_StopWatch = new Stopwatch();
        //public List<long> GetRNeuronsFromInputsTime { get; set; } = new List<long>();
        //public List<long> GetBestSolutionTime { get; set; } = new List<long>();
        //public List<long> RangeInfoTime { get; set; } = new List<long>();
        //public List<long> RneuronExecuteTime { get; set; } = new List<long>();
        //public List<long> FindBestTime { get; set; } = new List<long>();
        //public List<long> GetRandomSolTime { get; set; } = new List<long>();

        private readonly ConcurrentQueue<Queue<Case>> MASTER_CASE_QUEUE = new ConcurrentQueue<Queue<Case>>();
        private Queue<Case> caseQueue = null;

        //Thread lock objects
        private object caseQueue_lock = new object();
        private object lockDynamicInputs = new object();

        // Get best solution variables
        private BestSolution currBS = null;
        private bool predict = false;
        private IEnumerable<long> excludeSolutions;
        private GetRneuronResult retValGetRneuronFromInputs = new GetRneuronResult();
        private IDictionary<int, InputRangeInfo> rangeInfos = new Dictionary<int, InputRangeInfo>();

        /// <summary>
        /// Initializes memory manager
        /// </summary>
        /// <param name="databaseName">datbase name</param>
        public Manager(IRlmNetwork network, bool trackStats = false)
        {
            Network = network;

            bcSessionsToCreate = new BlockingCollection<Session>();
            bcSessionsToUpdate = new BlockingCollection<Session>();
            bcCasesQueue = new BlockingCollection<Case>();


            rlmDb = new RlmDbMgr(network.RlmDBData, network.PersistData);
            rlmDbEnqueuer = new RlmObjectEnqueuer();
            
            progressUpdater.Interval = 1000;
            progressUpdater.Elapsed += ProgressUpdater_Elapsed;

            if (trackStats)
            {
                GetRneuronTimes = new List<TimeSpan>();
                RebuildCacheboxTimes = new List<TimeSpan>();
                SwGetRneuron = new System.Diagnostics.Stopwatch();
                SwRebuildCache = new System.Diagnostics.Stopwatch();
            }

            MASTER_CASE_QUEUE.Enqueue(caseQueue = new Queue<Case>());

            if (network.GPURneuronProcessor == null)
            {
                rneuronProcessor = new RlmRneuronGetter(this);
            }
            else
            {
                GPUMode = true;
                rneuronProcessor = network.GPURneuronProcessor;
            }
        }

        public void SetArrays(int length)
        {
            doubleInputs = new RlmArray<double>[length];
            results = new RlmArray<bool>[length];
            doubleInputsArray = new double[length][];
            resultsArray = new bool[length][];
            fromArray = new double[length];
            toArray = new double[length];

            for (var d = 0; d < length; d++)
            {
                doubleInputs[d] = new RlmArray<double>(ARRAY_SIZE, double.MinValue);
            }

            for (var d = 0; d < length; d++)
            {
                results[d] = new RlmArray<bool>(ARRAY_SIZE);
            }

            for (int i = 0; i < length; i++)
            {
                doubleInputsArray[i] = doubleInputs[i].DataArray;
                resultsArray[i] = results[i].DataArray;
            }
        }
        
        /// <summary>
        /// Save created network
        /// </summary>
        /// <param name="rnetwork">current rnetwork</param>
        /// <param name="io_type">type of input and output</param>
        /// <param name="inputs">List of inputs</param>
        /// <param name="outputs">List of outputs</param>
        public void NewNetwork(Rnetwork rnetwork, Input_Output_Type io_type, List<Input> inputs, List<Output> outputs)
        {
            //todo: rnn dbmanager and save
            rlmDb.SaveNetwork(rnetwork, io_type, inputs, outputs);
            double r = 1.0;
            double s = 1.0;

            foreach(var input in inputs)
            {
                r *= input.Max;
            }

            rneuronsBoundedCapacity = Convert.ToInt32(r);

            foreach(var output in outputs)
            {
                s *= output.Max;
            }

            solutionsBoundedCapacity = Convert.ToInt32(s);
        }
        /// <summary>
        /// Save the new network and send it to a task. It also starts the database workers
        /// </summary>
        /// <param name="rnetwork"></param>
        /// <param name="io_types"></param>
        /// <param name="inputs"></param>
        /// <param name="outputs"></param>
        /// <param name="rnn_net"></param>
        public void NewNetwork(Rnetwork rnetwork, List<Input_Output_Type> io_types, List<Input> inputs, List<Output> outputs, IRlmNetwork rnn_net)
        {
            //todo: rnn dbmanager and save
            dbSavingTime.Start();

            RlmDbLogger.Info("\n" + string.Format("[{0:G}]: Started saving data for {1}...", DateTime.Now, Network.DatabaseName), Network.DatabaseName);

            Task t1 = Task.Run(() =>
            {
                rlmDb.SaveNetwork(rnetwork, io_types, inputs, outputs, rnn_net);
            });

            t1.Wait();
            StartRlmDbWorkers();
        }

        /// <summary>
        /// Add the session to queue
        /// </summary>
        /// <param name="key">session Id</param>
        /// <param name="session">current session</param>
        /// <returns></returns>
        public bool AddSessionToCreateToQueue(long key, Session session)
        {
            bool retVal = false;

            sessionsDone = false;
            if (!dbSavingTime.IsRunning)
            {
                dbSavingTime.Restart();
            }

            SessionsQueueToCreate.Enqueue(session);

            return retVal;
        }
        /// <summary>
        /// Add session to be updated to queue
        /// </summary>
        /// <param name="session">current session</param>
        /// <returns></returns>
        public bool AddSessionToUpdateToQueue(Session session)
        {
            bool retVal = false;

            SessionsQueueToUpdate.Enqueue(session);

            return retVal;
        }
        /// <summary>
        /// Add case to queue
        /// </summary>
        /// <param name="key">cycle Id</param>
        /// <param name="c_case">current case</param>
        public void AddCaseToQueue(long key, Case c_case)
        {
            if (caseQueue.Count() >= MAX_ALLOC)
            {
                MASTER_CASE_QUEUE.Enqueue(caseQueue = new Queue<Case>(MAX_ALLOC));
            }

            lock (caseQueue_lock)
            {
                caseQueue.Enqueue(c_case);
            }
        }

        /// <summary>
        /// Gets existing Rneuron and creates a new one if not existing
        /// </summary>
        /// <param name="inputs">Inputs with value</param>
        /// <param name="rnetworkID">Current NetworkId</param>
        /// <returns></returns>
        public GetRneuronResult GetRneuronFromInputs(IEnumerable<RlmIOWithValue> inputs, long rnetworkID)
        {
            

            // generate key based on input values
            long rneuronId = Util.GenerateHashKey(inputs.Select(a => a.Value).ToArray());


            // create new rneuron if not exists
            if (!Rneurons.ContainsKey(rneuronId))
            {
                var rneuron = new Rneuron() { ID = rneuronId, Rnetwork_ID = rnetworkID };

                int cnt = 0;

                foreach(var i in inputs)
                {
                    // create IVR instance
                    var ivr = new Input_Values_Rneuron()
                    {
                        ID = Util.GenerateHashKey(rneuronId, i.ID),
                        Value = i.Value,
                        Input_ID = i.ID,
                        Rneuron_ID = rneuronId,
                        DotNetType = i.DotNetType,
                        InputType = i.Type
                    };
                    rneuron.Input_Values_Rneurons.Add(ivr);

                    double value;
                    if (i.DotNetType == typeof(bool).ToString())
                    {
                        bool boolVal = Convert.ToBoolean(i.Value);
                        value = (boolVal) ? 1D : 0D;
                    }
                    else
                    {
                        value = Convert.ToDouble(ivr.Value);
                    }

                    doubleInputs[cnt].Add(value);
                    results[cnt].Resize(doubleInputs[cnt].DataArray.Length);

                    cnt++;
                }

                Rneurons.TryAdd(rneuronId, rneuron);
                rneuronIds.Add(rneuronId);
                
                retValGetRneuronFromInputs.Rneuron = rneuron;
                retValGetRneuronFromInputs.ExistsInCache = false;
            }
            else
            {
                retValGetRneuronFromInputs.Rneuron = Rneurons[rneuronId]; 
                retValGetRneuronFromInputs.ExistsInCache = true;
            }

            return retValGetRneuronFromInputs;
        }
        /// <summary>
        /// Sets the Rneuron
        /// </summary>
        /// <param name="rneuron"></param>
        public void SetRneuronWithInputs(Rneuron rneuron)
        {
            var rneuronId = rneuron.ID;
            int cnt = 0;

            IComparer<RlmInputKey> distinctComparer = new RlmInputKeyDistinctComparer();
            IComparer<RlmInputKey> linearComparer = new RlmInputKeyLinearComparer();

            // TODO must implement repopulation of data arrays for loading network
            // build dynamic inputs
            foreach (var i in rneuron.Input_Values_Rneurons)
            {
                double inputDoubleValue = 0;
                if (i.DotNetType == typeof(bool).ToString())
                {
                    bool boolVal = Convert.ToBoolean(i.Value);
                    inputDoubleValue = (boolVal) ? 1 : 0; ;
                }
                else
                {
                    inputDoubleValue = Convert.ToDouble(i.Value);
                }

                doubleInputs[cnt].Add(inputDoubleValue);
                results[cnt].Resize(doubleInputs[cnt].DataArray.Length);

                cnt++;
            }

            Rneurons.TryAdd(rneuronId, rneuron);
            rneuronIds.Add(rneuronId);
        }

        /// <summary>
        /// Gets best Solution
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="linearTolerance"></param>
        /// <param name="predict"></param>
        /// <returns></returns>
        public Solution GetBestSolution(IEnumerable<RlmIOWithValue> inputs, double trainingLinearTolerance = 0, bool predict = false, double predictLinearTolerance = 0, IEnumerable<long> excludeSolutions = null)
        {

            this.predict = predict;
            this.excludeSolutions = excludeSolutions;

            bool useLinearTolerance = ((predict && predictLinearTolerance > 0) || !predict) ? true : false;
            double linearTolerance = (predict) ? predictLinearTolerance : trainingLinearTolerance;

            Solution retVal = null;

            int cnt = 0;
            foreach (var item in inputs)
            {
                InputRangeInfo rangeInfo = null;
                double val;

                if (item.DotNetType == typeof(bool).ToString())
                {
                    bool boolVal = Convert.ToBoolean(item.Value);
                    val = (boolVal) ? 1 : 0;
                }
                else
                {
                    val = Convert.ToDouble(item.Value);
                }

                if (item.Type == Enums.RlmInputType.Linear)
                {
                    double off = (item.Max - item.Min) * ((linearTolerance == 0) ? 0 : (linearTolerance / 100D));
                    rangeInfo = new InputRangeInfo() { InputId = item.ID, InputType = item.Type, FromValue = val - off, ToValue = val + off };                    
                }
                else
                {
                    rangeInfo = new InputRangeInfo() { InputId = item.ID, InputType = item.Type, Value = item.Value, FromValue = val, ToValue = val };
                }
                                
                rangeInfos[cnt] = rangeInfo;

                doubleInputsArray[cnt] = doubleInputs[cnt].DataArray;
                resultsArray[cnt] = results[cnt].DataArray;
                fromArray[cnt] = rangeInfo.FromValue;
                toArray[cnt] = rangeInfo.ToValue;
                cnt++;
            }

            currBS = null;

            if (useLinearTolerance && predict == false)
            {
                if (CacheBox.IsWithinRange(rangeInfos, linearTolerance))
                {
                    rneuronProcessor.Execute(CacheBox.CachedRneurons, CacheBox.CachedInputs, fromArray, toArray, false);
                }
                else
                {
                    double[][] cacheBoxRangeInfos = RebuildCacheBoxRangesGPU(inputs, linearTolerance);

                    rneuronsCacheArray = new bool[rneuronIds.DataArray.Length];
                    var cachedDataArray = rneuronProcessor.Execute(rneuronIds.DataArray, doubleInputsArray, fromArray, toArray, rneuronsCacheArray, cacheBoxRangeInfos[0], cacheBoxRangeInfos[1]);

                    CacheBox.CachedRneurons = cachedDataArray.Rneurons.ToArray();
                    CacheBox.CachedInputs = cachedDataArray.Inputs.Select(a => a.ToArray()).ToArray();
                }
            }
            else
            {
                rneuronProcessor.Execute(rneuronIds.DataArray, doubleInputsArray, fromArray, toArray, true);
            }

            if (currBS != null)
            {
                retVal = Solutions[currBS.SolutionId];
            }

            return retVal;
        }

        public void FindBestSolution(long rneuronId)
        {
            Dictionary<long, BestSolution> bsDict;
            if (BestSolutions.TryGetValue(rneuronId, out bsDict))
            {
                IEnumerable<BestSolution> solutionDic;
                if (excludeSolutions != null && excludeSolutions.Count() > 0)
                {
                    solutionDic = bsDict
                        .Where(a => !excludeSolutions.Any(b => b == a.Key))
                        .Select(a => a.Value);
                }
                else
                {
                    solutionDic = bsDict.Values;
                }

                foreach (var bs in solutionDic)
                {
                    if (currBS == null)
                    {
                        Interlocked.Exchange(ref currBS, bs);
                        continue;
                    }

                    if (!predict)
                    {
                        if ((bs.CycleScore > currBS.CycleScore) ||
                            (bs.CycleScore == currBS.CycleScore && bs.SessionScore > currBS.SessionScore) ||
                            (bs.CycleScore == currBS.CycleScore && bs.SessionScore == currBS.SessionScore && bs.CycleOrder > currBS.CycleOrder))
                        {
                            Interlocked.Exchange(ref currBS, bs);
                        }
                    }
                    else
                    {
                        if ((bs.SessionScore > currBS.SessionScore) ||
                            (bs.SessionScore == currBS.SessionScore && bs.CycleScore > currBS.CycleScore) ||
                            (bs.SessionScore == currBS.SessionScore && bs.CycleScore == currBS.CycleScore && bs.CycleOrder > currBS.CycleOrder))
                        {
                            Interlocked.Exchange(ref currBS, bs);
                        }
                    }
                }
            }
        }

        private Dictionary<int, InputRangeInfo> RebuildCacheBoxRanges(IEnumerable<RlmIOWithValue> inputs, double linearTolerance)
        {
            CacheBoxCount++;
            CacheBox.Clear();

            var cacheBoxRangeInfos = new Dictionary<int, InputRangeInfo>();
            int cacheRangeCnt = 0;
            foreach (var item in inputs)
            {
                if (item.Type == Enums.RlmInputType.Linear)
                {
                    double val = Convert.ToDouble(item.Value);
                    double dataOff = (item.Max - item.Min) * ((linearTolerance == 0) ? 0 : (linearTolerance / 100D));
                    //double cacheMargin = (CacheBoxMargin == 0) ? 0 : ((item.Max - item.Min) * (CacheBoxMargin / 100));
                    double momentum = item.InputMomentum.MomentumDirection;
                    double toOff = 0;
                    double fromOff = 0;
                    double cacheOff = 0;

                    if (UseMomentumAvgValue)
                        cacheOff = item.InputMomentum.MomentumValue * MomentumAdjustment;
                    else
                        cacheOff = (item.Max - item.Min) * ((linearTolerance == 0) ? 0 : (MomentumAdjustment / 100D));


                    if (momentum > 0)
                    {
                        var offset = momentum * cacheOff;
                        toOff = val + dataOff + (cacheOff + offset);
                        fromOff = val - dataOff - (cacheOff - offset);
                    }
                    else if (momentum < 0)
                    {
                        var offset = Math.Abs(momentum) * cacheOff;
                        toOff = val + dataOff + (cacheOff - offset);
                        fromOff = val - dataOff - (cacheOff + offset);
                    }
                    else
                    {
                        toOff = val + dataOff + cacheOff;
                        fromOff = val - dataOff - cacheOff;
                    }

                    double cacheMargin = (CacheBoxMargin == 0) ? 0 : (cacheOff) * (CacheBoxMargin / 100D);

                    toOff += cacheMargin;
                    fromOff -= cacheMargin;

                    cacheBoxRangeInfos.Add(cacheRangeCnt, new InputRangeInfo() { InputId = item.ID, FromValue = Math.Ceiling(fromOff), ToValue = Math.Ceiling(toOff) });
                }
                else
                {
                    cacheBoxRangeInfos.Add(cacheRangeCnt, new InputRangeInfo() { InputId = item.ID, Value = item.Value });
                }
                cacheRangeCnt++;
            }

            CacheBox.SetRanges(cacheBoxRangeInfos.Values);
            return cacheBoxRangeInfos;
        }

        private double[][] RebuildCacheBoxRangesGPU(IEnumerable<RlmIOWithValue> inputs, double linearTolerance)
        {
            var retVal = new double[][] { new double[inputs.Count()], new double[inputs.Count()] };
            var ranges = RebuildCacheBoxRanges(inputs, linearTolerance);

            for (int i = 0; i < ranges.Count; i++)
            {
                var item = ranges.ElementAt(i);
                retVal[0][i] = item.Value.FromValue;
                retVal[1][i] = item.Value.ToValue;
            }

            return retVal;
        }

        /// <summary>
        /// Sets best solution
        /// </summary>
        /// <param name="bestSolution"></param>
        public void SetBestSolution(BestSolution bestSolution)
        {
            Dictionary<long, BestSolution> innerBestSolutions;
            if (BestSolutions.TryGetValue(bestSolution.RneuronId, out innerBestSolutions))
            {
                innerBestSolutions.Add(bestSolution.SolutionId, bestSolution);
            }
            else
            {
                innerBestSolutions = new Dictionary<long, BestSolution>();
                innerBestSolutions.Add(bestSolution.SolutionId, bestSolution);
                BestSolutions.TryAdd(bestSolution.RneuronId, innerBestSolutions);
            }
        }
        /// <summary>
        /// Gets a random solution from outputs or randomize 
        /// </summary>
        /// <param name="randomnessCurrVal"></param>
        /// <param name="outputs"></param>
        /// <param name="bestSolutionId"></param>
        /// <returns></returns>
        public GetSolutionResult GetRandomSolutionFromOutput(double randomnessCurrVal, IEnumerable<RlmIO> outputs, long? bestSolutionId = null, IEnumerable<RlmIdea> ideas = null)
        {
            GetSolutionResult retVal = null;

            if (outputs.Count() == 1)
            {
                IEnumerable<RlmIOWithValue> outputWithValues = GetRandomOutputValues(outputs, ideas);
                retVal = GetSolutionFromOutputs(outputWithValues);
            }
            else
            {
                var outputsWithVal = new List<RlmIOWithValue>();

                // check if best solution was passed in as parameter or not
                if (bestSolutionId.HasValue)
                {
                    IEnumerable<Output_Values_Solution> bestOutputs = null;
                    int cntRandomValues = 0;

                    foreach (var item in outputs)
                    {
                        // use best solution value if randomness outside threshold
                        int randomnessValue = Util.Randomizer.Next(1, 101);
                        if (randomnessValue > randomnessCurrVal)
                        {
                            if (bestOutputs == null)
                            {
                                //bestOutputs = db.Output_Values_Solutions.Where(a => a.Solution_ID == bestSolutionId.Value);
                                Solution solution = Solutions[bestSolutionId.Value];
                                bestOutputs = solution.Output_Values_Solutions;
                            }

                            var bestOutput = bestOutputs.FirstOrDefault(a => a.Output_ID == item.ID);
                            outputsWithVal.Add(new RlmIOWithValue(item, bestOutput.Value));
                        }
                        else // get random value
                        {
                            RlmIdea idea = null;
                            if (ideas != null)
                                idea = ideas.FirstOrDefault(a => a.RlmIOId == item.ID);

                            string value = GetRandomValue(item, idea);
                            outputsWithVal.Add(new RlmIOWithValue(item, value));
                            cntRandomValues++;
                        }
                    }

                    // if no random values were assigned then we randomly select one to randomize
                    // this is to ensure we have at least one random output value
                    if (cntRandomValues == 0)
                    {
                        var index = Util.Randomizer.Next(0, outputsWithVal.Count);
                        var output = outputsWithVal.ElementAt(index);

                        RlmIdea idea = null;
                        if (ideas != null)
                            idea = ideas.FirstOrDefault(a => a.RlmIOId == output.ID);

                        string value = GetRandomValue(output, idea);
                        output.Value = value;
                    }
                }
                else // no best solution, so we give out all random values
                {
                    outputsWithVal.AddRange(GetRandomOutputValues(outputs, ideas));
                }

                retVal = GetSolutionFromOutputs(outputsWithVal);
            }

            return retVal;
        }
        /// <summary>
        /// Gets solution and record ideal score
        /// </summary>
        /// <param name="outputs"></param>
        /// <returns></returns>
        public GetSolutionResult GetSolutionFromOutputs(IEnumerable<RlmIOWithValue> outputs)
        {
            GetSolutionResult retVal = new GetSolutionResult();
            Solution solution = null;

            // generate key based on output values 
            long solutionId = Util.GenerateHashKey(outputs.Select(a => a.Value).ToArray());

            // create new solution if not exists
            if (!Solutions.TryGetValue(solutionId, out solution))
            {
                solution = new Solution() { ID = solutionId };

                foreach(var o in outputs)
                {
                    // create OVS instance
                    var ovs = new Output_Values_Solution()
                    {
                        ID = Util.GenerateHashKey(solutionId, o.ID),
                        Value = o.Value,
                        Output_ID = o.ID,
                        Solution_ID = solutionId
                    };
                    solution.Output_Values_Solutions.Add(ovs);

                    // insert into dynamic output collection
                    HashSet<SolutionOutputSet> outputSet = DynamicOutputs[o.ID];
                    outputSet.Add(new SolutionOutputSet()
                    {
                        SolutionId = solutionId,
                        Value = o.Value
                    });
                }

                Solutions.TryAdd(solution.ID, solution);

                retVal.Solution = solution;
                retVal.ExistsInCache = false;
            }
            else
            {
                retVal.Solution = solution;
                retVal.ExistsInCache = true;
            }

            return retVal; 
        }
        /// <summary>
        /// Sets solution and cache
        /// </summary>
        /// <param name="solution">solution</param>
        public void SetSolutionWithOutputs(Solution solution)
        {
            long solutionId = solution.ID;

            // add to Solutions cache
            Solutions.TryAdd(solution.ID, solution);

            // build dynamic outputs
            foreach (var o in solution.Output_Values_Solutions)
            {
                // insert into dynamic output collection
                HashSet<SolutionOutputSet> outputSet = DynamicOutputs[o.Output_ID];
                lock (outputSet)
                {
                    outputSet.Add(new SolutionOutputSet()
                    {
                        SolutionId = solutionId,
                        Value = o.Value
                    });
                }
            }              
        }


        /// <summary>
        /// starts database workers that handle queue's
        /// </summary>
        public void StartRlmDbWorkers()
        {
            //note: we can start multiple workers later
            rlmDb.StartSessionWorkerForCreate(bcSessionsToCreate, workerTokenSrc.Token); //start session thread for create
            Task.Factory.StartNew(() => { rlmDbEnqueuer.QueueObjects<Session>(SessionsQueueToCreate, bcSessionsToCreate, workerTokenSrc.Token); }, TaskCreationOptions.LongRunning); //queue sessions for create to blocking collections

            rlmDb.StartSessionWorkerForUpdate(bcSessionsToUpdate, workerTokenSrc.Token); //start session thread for update
            Task.Factory.StartNew(() => { rlmDbEnqueuer.QueueObjects<Session>(SessionsQueueToUpdate, bcSessionsToUpdate, workerTokenSrc.Token); }, TaskCreationOptions.LongRunning); //queue sessions for update to blocking collections

            rlmDb.StartCaseWorker(bcCasesQueue, workerTokenSrc.Token); //start case thread to save (Rneuron, Solution, Case) to db
            Task.Factory.StartNew(() => { rlmDbEnqueuer.QueueObjects(MASTER_CASE_QUEUE, bcCasesQueue, caseQueue_lock, workerTokenSrc.Token); }, TaskCreationOptions.LongRunning);

            progressUpdater.Start();
        }

        /// <summary>
        /// stops the session queue worker
        /// </summary>
        public void StopRlmDbWorkersSessions()
        {
            bcSessionsToCreate?.CompleteAdding();
            bcSessionsToUpdate?.CompleteAdding();
            
            sessionsDone = true;
            totalSessionsCount = Sessions.Count;
        }
        /// <summary>
        /// stops the cases queue worker
        /// </summary>
        public void StopRlmDbWorkersCases()
        {
            bcCasesQueue?.CompleteAdding();

            if (ConfigFile.DropDb)
            {
                Thread.Sleep(5000);

                rlmDb.DropDB();

                RlmDbLogger.Info("\n" + string.Format("[{0:G}]: {1} database successfully dropped...\n*** END ***\n", DateTime.Now, Network.DatabaseName), Network.DatabaseName);
            }

            progressUpdater.Stop();

            if (rlmDb.CaseWorkerQueues != null)
            {
                foreach (var item in rlmDb.CaseWorkerQueues)
                {
                    item.WorkerQueues.CompleteAdding();
                }
            }
        }
        /// <summary>
        /// Loads the network result
        /// </summary>
        /// <param name="network"></param>
        /// <returns></returns>
        public LoadRnetworkResult LoadNetwork(string networkName)
        {
            var result = rlmDb.LoadNetwork(networkName, Network);
            if (result.Loaded && Network.PersistData)
            {
                StartRlmDbWorkers();
            }
            return result;
        }

        /// <summary>
        /// signals that training is done
        /// </summary>
        public void TrainingDone()
        {          
            trainingDone = true;

            //background thread to stop session db workers when done
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    bool processing = Sessions.Any(a => a.Value.CreatedToDb == false || a.Value.UpdatedToDb == false);
                    if (!processing && /*Sessions.Count > 0 &&*/ trainingDone)
                    {
                        //StopRlmDbWorkersSessions();
                        sessionsDone = true;
                        System.Diagnostics.Debug.WriteLine("Worker Session done");
                        break;
                    }

                    //Task.Delay(5 * 1000).Wait();
                    Thread.Sleep(5000);
                }

            }, TaskCreationOptions.LongRunning);

            //background thread to stop case db workers when done
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (sessionsDone && 
                        //rlmDb.DistinctCaseSessionsCount() == totalSessionsCount && 
                        MASTER_CASE_QUEUE.Count == 1 && 
                        MASTER_CASE_QUEUE.ElementAt(0).Count == 0 &&
                        rlmDb.CaseWorkerQueues.All(a=> a.WorkerQueues.Count == 0 && !a.IsBusy))
                    {
                        // notify parent network that db background workers are done
                        DataPersistenceComplete?.Invoke();
                        dbSavingTime.Stop();
                        RlmDbLogger.Info("\n" + string.Format("[{0:G}]: Data successfully saved to the database in {1}", DateTime.Now, dbSavingTime.Elapsed), Network.DatabaseName);

                        System.Diagnostics.Debug.WriteLine("Worker Cases done");
                        break;
                    }

                    Thread.Sleep(5000);
                }

            }, TaskCreationOptions.LongRunning);
        }

        public void SetProgressInterval(int interval)
        {
            progressUpdater.Interval = interval;
        }

        private void ProgressUpdater_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (lastProgress != rlmDb.TotalTaskCompleted)
            {
                lastProgress = rlmDb.TotalTaskCompleted;
                DataPersistenceProgress?.Invoke(rlmDb.TotalTaskCompleted, rlmDbEnqueuer.TotalTaskEnqueued);
            }
        }

        private IEnumerable<RlmIOWithValue> GetRandomOutputValues(IEnumerable<RlmIO> outputs, IEnumerable<RlmIdea> ideas = null)
        {
            var retVal = new List<RlmIOWithValue>();

            foreach (var item in outputs)
            {
                RlmIdea idea = null;
                if (ideas != null)
                    idea = ideas.First(a => a.RlmIOId == item.ID);

                string value = GetRandomValue(item, idea);

                var output_with_value = new RlmIOWithValue(item, value);
                retVal.Add(output_with_value);
            }

            return retVal;
        }

        private string GetRandomValue(RlmIO item, RlmIdea idea = null)
        {
            string value = string.Empty;

            // TODO add checking on the types and their max and min values
            switch (item.DotNetType)
            {
                case "System.Boolean":
                    int boolMin = Convert.ToInt32(item.Min);
                    int boolMax = Convert.ToInt32(item.Max + 1);
                    int boolIntValue = Util.Randomizer.Next(boolMin, boolMax);
                    if (boolIntValue > 1 && boolIntValue < 0)
                    {
                        throw new Exception("Boolean value can only be 1 or 0");
                    }
                    value = Convert.ToBoolean(boolIntValue).ToString();
                    break;

                case "System.Double":
                case "System.Decimal":
                    double doubleMin = Convert.ToDouble(item.Min);
                    double doubleMax = Convert.ToDouble(item.Max);
                    value = Util.Randomizer.NextDouble(doubleMin, doubleMax).ToString();
                    break;

                default:
                    int min = Convert.ToInt32(item.Min);
                    int max = Convert.ToInt32(item.Max + 1);
                    if (idea != null && idea is RlmOutputLimiter)
                    {
                        var itemIdea = idea as RlmOutputLimiter;
                        max = itemIdea.IndexMax + 1;
                        int ideaIndex = Util.Randomizer.Next(min, max);
                        if (itemIdea.GetIndexEquivalent != null)
                        {
                            value = itemIdea.GetIndexEquivalent(ideaIndex).ToString();
                        }
                        else
                        {
                            value = ideaIndex.ToString();
                        }
                    }
                    else
                    {
                        value = Util.Randomizer.Next(min, max).ToString();
                    }
                    break;
            }

            return value;
        }

        public void Dispose()
        {
            workerTokenSrc.Cancel();
            StopRlmDbWorkersSessions();
            StopRlmDbWorkersCases();

            BestSolutions?.Clear();
            BestSolutionStaging?.Clear();
            Sessions?.Clear();
            Rneurons?.Clear();
            Solutions?.Clear();

            if (SessionsQueueToCreate != null && SessionsQueueToCreate.Count > 0)
            {
                Session removedSess;
                foreach (var item in SessionsQueueToCreate)
                {
                    SessionsQueueToCreate.TryDequeue(out removedSess);
                }
            }

            if (SessionsQueueToUpdate != null && SessionsQueueToUpdate.Count > 0)
            {
                Session removedSess;
                foreach(var item in SessionsQueueToUpdate)
                {
                    SessionsQueueToUpdate.TryDequeue(out removedSess);
                }
            }

            DynamicInputs?.Clear();
            DynamicOutputs?.Clear();

            if (GPUMode)
            {
                if (rneuronProcessor is IDisposable)
                {
                    (rneuronProcessor as IDisposable).Dispose();
                }
            }
        }

        public IEnumerable<long> GetSolutionIdsForOutputs(IDictionary<long, IEnumerable<string>> outputs)
        {
            IEnumerable<long> retVal = new List<long>();
            if (outputs != null && outputs.Values.All(a => a.Count() > 0))
            {
                retVal = rlmDb.GetSolutionIdsForOutputs(outputs);
            }
            return retVal;
        }

        public void RemoveSolutionCascade(long solutionId)
        {
            rlmDb.CascadeDelete(solutionId);
        }
    }
}

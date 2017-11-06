using RLM.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data.Entity;
using RLM.Models.Interfaces;
using PagedList;
using System.Diagnostics;

namespace RLM.Database
{
    public class RlmDbMgr
    {
        private const int CASES_TASKS = 5;
        
        private int currCaseQueue = 0;
        public BlockingCollection<Case[]>[] CaseWorkerQueues { get; private set; }

        private bool networkLoaded = true;
        private string databaseName;
        private long networkID;
        private Rnetwork rlm;
        private RlmDbBatchProcessor batchProcessor;
        private bool persistData;
        public RlmDbMgr(string databaseName, bool persistData = true)
        {
            this.persistData = persistData;
            this.databaseName = databaseName;

            if (persistData)
            {
                this.batchProcessor = new RlmDbBatchProcessor(databaseName);

                CaseWorkerQueues = new BlockingCollection<Case[]>[CASES_TASKS];
                for (int i = 0; i < CASES_TASKS; i++)
                {
                    CaseWorkerQueues[i] = new BlockingCollection<Case[]>();
                    int caseIndex = i;
                    Task.Run(async () => { await saveCase(CaseWorkerQueues[caseIndex]); });
                }
            }
        }

        public int TaskDelay { get; private set; } = 50;
        public int TaskRetryDelay { get; private set; } = 1000;

        private long taskCompletedCounter = 0;
        public long TotalTaskCompleted
        {
            get
            {
                return Interlocked.Read(ref taskCompletedCounter);
            }
        }


        public Task StartSessionWorkerForCreate(BlockingCollection<Session> sessionqueue, CancellationToken ct)
        {
            Task T1 = Task.Run(async () =>
            {
                foreach (Session session in sessionqueue.GetConsumingEnumerable())
                {
                    await createSession(session);
                    Interlocked.Increment(ref taskCompletedCounter);
                    //Task.Delay(TaskDelay).Wait();
                }

            }, ct);

            return T1;
        }

        public Task StartSessionWorkerForUpdate(BlockingCollection<Session> sessionqueue, CancellationToken ct)
        {
            Task T1 = Task.Run(async () =>
            {
                foreach (Session session in sessionqueue.GetConsumingEnumerable())
                {
                    await updateSession(session);
                    Interlocked.Increment(ref taskCompletedCounter);
                }
            }, ct);

            return T1;
        }

        private ConcurrentQueue<Case> caseBox = new ConcurrentQueue<Case>();
        public Task StartCaseWorker(BlockingCollection<Case> casesQueue, CancellationToken ct)
        {
            Task T1 = Task.Run(() =>
            {
                Stopwatch st = new Stopwatch();
                st.Start();

                List<Case> cases = new List<Case>();
                int cnt = 0;
                foreach (Case theCase in casesQueue.GetConsumingEnumerable())
                {
                    cnt++;
                    cases.Add(theCase);

                    Interlocked.Increment(ref taskCompletedCounter);
                    if (cnt % 10 == 0)
                    {
                        CaseWorkerQueues[currCaseQueue].Add(cases.ToArray());
                        cases.Clear();
                        currCaseQueue++;
                        if (currCaseQueue >= CASES_TASKS)
                        {
                            currCaseQueue = 0;
                        }
                        //Task.Delay(TaskDelay).Wait();
                    }
                }

                if (cases.Count > 0)
                {
                    CaseWorkerQueues[currCaseQueue].Add(cases.ToArray());
                }

                st.Stop();
                System.Diagnostics.Debug.WriteLine($"Case time: {st.Elapsed}");
            }, ct);

            return T1;
        }

        public Task StartRneuronWorker(BlockingCollection<Rneuron> rneuronsQueue, CancellationToken ct)
        {
            Task T1 = Task.Run(() =>
            {
                foreach (Rneuron rneuron in rneuronsQueue.GetConsumingEnumerable())
                {
                    saveRneuron(rneuron);
                    Task.Delay(TaskDelay).Wait();
                }
            }, ct);

            return T1;
        }

        public Task StartSolutionWorker(BlockingCollection<Solution> solutionsQueue, CancellationToken ct)
        {
            Task T1 = Task.Run(() =>
            {

                foreach (Solution solution in solutionsQueue.GetConsumingEnumerable())
                {
                    saveSolution(solution);
                    Task.Delay(TaskDelay).Wait();
                }

            }, ct);

            return T1;
        }

        #region DATA PERSISTENCE FUNCTIONS
        //create new network
        public async void SaveNetwork(Rnetwork rnetwork, Input_Output_Type io_type, List<Input> inputs, List<Output> outputs)
        {
            using (RlmDbEntities db = new RlmDbEntities(databaseName))
            {
                try
                {
                    //ready network for saving
                    Rnetwork _newNetwork = db.Rnetworks.Add(rnetwork);

                    //add inputs to io_type
                    foreach (Input input in inputs)
                    {
                        io_type.Inputs.Add(input);
                    }

                    //add outputs to io_type
                    foreach (Output output in outputs)
                    {
                        io_type.Outputs.Add(output);
                    }

                    //ready io_type for saving
                    Input_Output_Type _ioType = db.Input_Output_Types.Add(io_type);

                    //save to database
                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    RlmDbLogger.Error(ex, databaseName, "SaveNetwork");
                }
            }
        }

        public void SaveNetwork(Rnetwork rnetwork, List<Input_Output_Type> io_types, List<Input> inputs, List<Output> outputs, IRlmNetwork rnn_net)
        {
            using (RlmDbEntities db = new RlmDbEntities(databaseName))
            {
                try
                {
                    //ready network for saving
                    Rnetwork _newNetwork = db.Rnetworks.Add(rnetwork);

                    foreach (var _in in inputs)
                    {
                        Input_Output_Type _iotype = io_types.FirstOrDefault(a => a.DotNetTypeName == _in.Input_Output_Type.DotNetTypeName);

                        _in.Input_Output_Type = _iotype;

                        db.Inputs.Add(_in);
                    }

                    foreach (var _out in outputs)
                    {
                        Input_Output_Type _iotype = io_types.FirstOrDefault(a => a.DotNetTypeName == _out.Input_Output_Type.DotNetTypeName);

                        _out.Input_Output_Type = _iotype;

                        db.Outputs.Add(_out);
                    }

                    //save to database
                    db.SaveChanges();

                    rlm = _newNetwork;
                    networkID = _newNetwork.ID;

                    networkLoaded = true;

                    rnn_net.MemoryManager.InitStorage(inputs, outputs);
                }
                catch (Exception ex)
                {
                    networkLoaded = false;
                    RlmDbLogger.Error(ex, databaseName, "SaveNetwork");
                }
            }
        }

        //save session (create and update)
        private async Task createSession(Session session)
        {
            using (RlmDbEntities db = new RlmDbEntities(databaseName))
            {
                try
                {
                    if (networkLoaded)
                    {
                        db.Sessions.Add(session);
                        int a = await db.SaveChangesAsync();
                        if (a > 0)
                        {
                            session.CreatedToDb = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    RlmDbLogger.Error(ex, databaseName, "createSession");
                }
            }
        }

        private async Task updateSession(Session session)
        {
            using (RlmDbEntities db = new RlmDbEntities(databaseName))
            {
                try
                {
                    if (networkLoaded)
                    {
                        Session existing = null;
                        while (existing == null)
                        {
                            existing = db.Sessions.FirstOrDefault(a => a.ID == session.ID);

                            if (existing != null)
                            {
                                existing.Hidden = session.Hidden;
                                existing.SessionScore = session.SessionScore;
                                existing.DateTimeStop = session.DateTimeStop;

                                int a = await db.SaveChangesAsync();
                                if (a > 0)
                                {
                                    session.UpdatedToDb = true;
                                }
                                break;
                            }

                            //Task.Delay(50).Wait();
                        }
                    }
                }
                catch (Exception ex)
                {
                    RlmDbLogger.Error(ex, databaseName, "updateSession");
                }
            }
        }

        //save case
        private async void saveCase(Case theCase)
        {
            using (RlmDbEntities db = new RlmDbEntities(databaseName))
            {
                db.Cases.Add(theCase);
                start:

                try
                {
                    await db.SaveChangesAsync();

                    if (theCase.Rneuron != null)
                    {
                        theCase.Rneuron.SavedToDb = true;
                    }

                    if (theCase.Solution != null)
                    {
                        theCase.Solution.SavedToDb = true;
                    }

                    theCase.SavedToDb = true;

                    //RlmDbLogger.Info(string.Format("\n[{0:d/M/yyyy HH:mm:ss:ms}]: Saving case...", DateTime.Now), databaseName);
                }
                catch (Exception ex)
                {
                    RlmDbLogger.Error(ex, databaseName, "saveCase");
                    Task.Delay(TaskDelay).Wait();
                    goto start;
                }
            }
        }

        //save case
        private async Task saveCase(BlockingCollection<Case[]> cases)
        {
            Exception error = null;
            int retryCnt = 0;
            foreach (var item in cases.GetConsumingEnumerable())
            {
                do
                {
                    try
                    {
                        error = null;
                        using (RlmDbEntities db = new RlmDbEntities(databaseName))
                        {
                            db.Cases.AddRange(item);

                            await db.SaveChangesAsync();

                            //if (theCase.Rneuron != null)
                            //{
                            //    theCase.Rneuron.SavedToDb = true;
                            //}

                            //if (theCase.Solution != null)
                            //{
                            //    theCase.Solution.SavedToDb = true;
                            //}

                            //theCase.SavedToDb = true;

                            //RlmDbLogger.Info(string.Format("\n[{0:d/M/yyyy HH:mm:ss:ms}]: Saving case...", DateTime.Now), databaseName);
                        }

                        if (retryCnt > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"Cases saved after {retryCnt}");
                        }
                    }
                    catch (Exception ex)
                    {
                        RlmDbLogger.Error(ex, databaseName, "saveCase");
                        error = ex;
                        retryCnt++;
                        Task.Delay(TaskRetryDelay).Wait();
                    }
                } while (error != null);

                retryCnt = 0;
            }
          

            // TODO needs more testing and fix for Sessions concurrency bug
            //await batchProcessor.InsertCases(cases);
        }

        private double caseCount = 0;
        public void StartCaseWorker(CancellationToken tc)
        {
            Task.Run(async ()=> {

                while (true)
                {
                    using (RlmDbEntities db = new RlmDbEntities(databaseName))
                    {
                        try
                        {
                            var cases = caseBox.Where(a => a.SavedToDb == false);
                            var count = cases.Count();

                            if (count > 0)
                            {
                                foreach (var theCase in cases)
                                {
                                    db.Cases.Add(theCase);

                                    if (theCase.Rneuron != null)
                                    {
                                        theCase.Rneuron.SavedToDb = true;
                                    }

                                    if (theCase.Solution != null)
                                    {
                                        theCase.Solution.SavedToDb = true;
                                    }

                                    theCase.SavedToDb = true;
                                }

                                caseCount += count;
                                RlmDbLogger.Info(string.Format("[{0:d/M/yyyy HH:mm:ss:ms}]: Saving {1} cases [Total: {2}]", DateTime.Now, count, caseCount), databaseName);

                                await db.SaveChangesAsync();
                            }

                            Task.Delay(50).Wait();
                        }
                        catch (Exception ex)
                        {
                            RlmDbLogger.Error(ex, databaseName, "StartCaseWorker");
                        }
                    }
                }
            });

        }

        //save rneuron
        public async void saveRneuron(Rneuron rneuron)
        {
            using (RlmDbEntities db = new RlmDbEntities(databaseName))
            {
                try
                {
                    if (networkLoaded)
                    {
                        Task.Delay(5000).Wait();

                        Rneuron _rneuron = db.Rneurons.Add(rneuron);

                        _rneuron.Rnetwork_ID = networkID;

                        foreach (var rn in _rneuron.Input_Values_Reneurons)
                        {
                            db.Input_Values_Reneurons.Add(rn);
                        }

                        await db.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    RlmDbLogger.Error(ex, databaseName, "saveRneuron");
                }
            }
        }

        //save solution
        private async void saveSolution(Solution solution)
        {
            using (RlmDbEntities db = new RlmDbEntities(databaseName))
            {
                try
                {
                    if (networkLoaded)
                    {
                        Solution _solution = db.Solutions.Add(solution);

                        foreach (var sol in _solution.Output_Values_Solutions)
                        {
                            db.Output_Values_Solutions.Add(sol);
                        }

                        await db.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    RlmDbLogger.Error(ex, databaseName, "saveSolution");
                }
            }
        }

        //load network
        public LoadRnetworkResult LoadNetwork(string name, IRlmNetwork rnetwork)
        {
            var retVal = new LoadRnetworkResult();
            IRlmNetwork rnn = rnetwork;
            List<Input> inputs = new List<Input>();
            List<Output> outputs = new List<Output>();

            var watch = new Stopwatch();
            watch.Start();

            Rnetwork network;
            try
            {
                using (RlmDbEntities db = new RlmDbEntities(databaseName))
                {
                    //find network by name
                    network = db.Rnetworks.FirstOrDefault(a => a.Name == name);
                }

                if (network == null)
                {
                    //Throw an error
                    Console.WriteLine("Network name '" + name + "' does not exist in the database:" + databaseName);
                }
                else
                {
                    this.rlm = network;

                    // set rnetwork details
                    retVal.CurrentNetworkId = networkID = this.rlm.ID;
                    retVal.CurrentNetworkName = this.rlm.Name;

                    List<Input> rnnInputs = null;
                    List<Output> rnnOutputs = null;
                        
                    var tasks = new List<Task>();


                    int taskCnt = Environment.ProcessorCount / 2;
                    int batchSizeRn = 0;
                    int batchSizeIvr = 0;
                    var rnCountTask = Task.Run(() =>
                    {
                        Stopwatch cntWatch = new Stopwatch();
                        cntWatch.Start();

                        var subTasks = new Task[3];
                        int totalInputs = 0;
                        int totalRneurons = 0;
                        int totalIvrs = 0;

                        subTasks[0] = Task.Run(() =>
                        {
                            using (var ctx = new RlmDbEntities(databaseName))
                            {
                                totalInputs = ctx.Inputs.Count();
                            }
                        });

                        subTasks[1] = Task.Run(() =>
                        {
                            using (var ctx = new RlmDbEntities(databaseName))
                            {
                                totalRneurons = ctx.Rneurons.Count();
                            }
                        });

                        subTasks[2] = Task.Run(() =>
                        {
                            using (var ctx = new RlmDbEntities(databaseName))
                            {
                                totalIvrs = ctx.Input_Values_Reneurons.Count();
                            }
                        });

                        Task.WaitAll(subTasks);

                        batchSizeRn = Convert.ToInt32(Math.Ceiling(totalRneurons / (double)taskCnt));
                        batchSizeIvr = Convert.ToInt32(Math.Ceiling(totalIvrs / (double)taskCnt));

                        while (batchSizeIvr % totalInputs != 0)
                        {
                            batchSizeIvr++;
                        }
                            
                        cntWatch.Stop();
                        System.Diagnostics.Debug.WriteLine($"Count elapsed: {cntWatch.Elapsed}");
                    });
                    tasks.Add(rnCountTask);

                    var bestSolTask = Task.Run(() =>
                    {
                        IEnumerable<BestSolution> bestSolutions = null;
                        using (var ctx = new RlmDbEntities(databaseName))
                        {
                            bestSolutions = ctx.Database.SqlQuery<BestSolution>(@"
                                SELECT 
	                                main.[Rneuron_ID] as [RneuronId],
	                                main.[Solution_ID] as [SolutionId],
	                                MAX(main.[CycleScore]) as [CycleScore],
	                                MAX(sess.[SessionScore]) as [SessionScore],
	                                MAX(main.[CycleEndTime]) as [CycleEndTime]
                                FROM [Cases] main
                                INNER JOIN [Sessions] sess ON main.[Session_ID] = sess.[ID]
                                GROUP BY main.[Rneuron_ID], main.[Solution_ID]").ToList();
                        }

                        if (bestSolutions != null)
                        {
                            foreach (var item in bestSolutions)
                            {
                                rnetwork.MemoryManager.SetBestSolution(item);
                            }
                        }
                    });
                    tasks.Add(bestSolTask);

                    var inputTask = Task.Run(() =>
                    {
                        using (var ctx = new RlmDbEntities(databaseName))
                        {
                            // get inputs
                            rnnInputs = ctx.Inputs
                                .Include(a => a.Input_Output_Type)
                                .OrderBy(a => a.Order)
                                .ToList();
                        }

                        retVal.Inputs = rnetwork.Inputs = rnnInputs.Select(a => new RlmIO()
                        {
                            ID = a.ID,
                            Name = a.Name,
                            DotNetType = a.Input_Output_Type.DotNetTypeName,
                            Min = a.Min,
                            Max = a.Max,
                            Type = a.Type
                        });

                        rnetwork.InputMomentums.Clear();
                        foreach (var item in rnetwork.Inputs)
                        {
                            if (item.Type == Enums.RlmInputType.Linear)
                            {
                                rnetwork.InputMomentums.Add(item.ID, new RlmInputMomentum() { InputID = item.ID });
                            }
                        }
                        retVal.InputMomentums = rnetwork.InputMomentums;
                        
                    });
                    tasks.Add(inputTask);

                    var outputTask = Task.Run(() =>
                    {
                        using (var ctx = new RlmDbEntities(databaseName))
                        {
                            // get outputs
                            rnnOutputs = ctx.Outputs
                                .Include(a => a.Input_Output_Type)
                                .OrderBy(a => a.Order)
                                .ToList();
                        }

                        retVal.Outputs = rnetwork.Outputs = rnnOutputs.Select(a => new RlmIO()
                        {
                            ID = a.ID,
                            Name = a.Name,
                            DotNetType = a.Input_Output_Type.DotNetTypeName,
                            Min = a.Min,
                            Max = a.Max
                        });

                        foreach (var output in rnetwork.Outputs)
                        {
                            // add dynamic output collection
                            rnetwork.MemoryManager.DynamicOutputs.TryAdd(output.ID, new HashSet<SolutionOutputSet>());
                        }
                    });
                    tasks.Add(outputTask);

                    var sessionsTask = Task.Run(() =>
                    {
                        IEnumerable<Session> sessions = null;
                        using (var ctx = new RlmDbEntities(databaseName))
                        {
                            // get sessions and save to MemoryManager cache
                            sessions = ctx.Sessions.OrderBy(a => a.DateTimeStart).ToList();
                            
                            // set CaseOrder to the last case saved in db
                            retVal.CaseOrder = ctx.Cases.OrderByDescending(a => a.Order).Select(a => a.Order).FirstOrDefault();
                        }

                        if (sessions != null)
                        {
                            //set sessions count
                            retVal.SessionCount = sessions.Count();
                            foreach (var item in sessions)
                            {
                                rnetwork.MemoryManager.Sessions.TryAdd(item.ID, item);
                            }
                        }
                    });
                    tasks.Add(sessionsTask);
                        
                    var rneuronsTask = Task.WhenAll(inputTask, rnCountTask).ContinueWith((t) =>
                    {
                        Stopwatch rn_sw = new Stopwatch();
                        rn_sw.Start();
                       
                        var subTasks = new List<Task>();

                        // made input dictionary to help in setting the Dynamic inputs
                        var inputDic = rnnInputs.ToDictionary(a => a.ID, a => a);

                        var rneurons = new ConcurrentBag<Rneuron>();
                        var ivrs = new ConcurrentDictionary<long, List<Input_Values_Rneuron>>();
                        //IEnumerable<Rneuron> rneurons = null;
                        
                        Stopwatch subProcessWatch = new Stopwatch();
                        subProcessWatch.Start();

                        for (int i = 0; i < taskCnt; i++)
                        {
                            int batchNum = i;
                            subTasks.Add(Task.Run(() =>
                            {
                                Rneuron[] results;
                                using (var ctx = new RlmDbEntities(databaseName))
                                {
                                    results = ctx.Rneurons.OrderBy(a => a.ID).Skip(batchNum * batchSizeRn).Take(batchSizeRn).ToArray();
                                }
                                foreach(var item in results)
                                {
                                    rneurons.Add(item);
                                }
                            }));

                            subTasks.Add(Task.Run(() => 
                            {
                                Input_Values_Rneuron[] results;
                                using (var ctx = new RlmDbEntities(databaseName))
                                {
                                    results = ctx.Input_Values_Reneurons.OrderBy(a => a.Rneuron_ID).Skip(batchNum * batchSizeIvr).Take(batchSizeIvr).ToArray();
                                }
                                foreach(var item in results.GroupBy(a => a.Rneuron_ID))
                                {
                                    ivrs.TryAdd(item.Key, item.ToList());
                                }
                            }));
                        }

                        Task.WaitAll(subTasks.ToArray());
                        subProcessWatch.Stop();
                        System.Diagnostics.Debug.WriteLine($"Get data: {subProcessWatch.Elapsed}");
                        
                        subTasks.Clear();

                        subProcessWatch.Restart();

                        for (int i = 0; i < taskCnt; i++)
                        {
                            int batchNum = i;
                            subTasks.Add(Task.Run(() =>
                            {
                                //Stopwatch sw = new Stopwatch();
                                //sw.Start();

                                //IEnumerable<Rneuron> rneurons = null;
                                //using (var ctx = new RlmDbEntities(databaseName))
                                //{
                                //    rneurons = ctx.Rneurons
                                //        .Include(a => a.Input_Values_Reneurons)
                                //        .OrderBy(a => a.ID)
                                //        .Skip(batchNum * batchSizeRn)
                                //        .Take(batchSizeRn)
                                //        .ToList();
                                //}

                                //sw.Stop();
                                //System.Diagnostics.Debug.WriteLine($"Task: {Task.CurrentId}, Get Data: {sw.Elapsed}");

                                //sw.Restart();

                                //sw.Start();

                                foreach (var item in rneurons.Skip(batchNum * batchSizeRn).Take(batchSizeRn))
                                {
                                    // set input type and dotnettype
                                    item.Input_Values_Reneurons = ivrs[item.ID];
                                    foreach (var ivr in item.Input_Values_Reneurons)
                                    {
                                        var input = inputDic[ivr.Input_ID];
                                        ivr.InputType = input.Type;
                                        ivr.DotNetType = input.Input_Output_Type.DotNetTypeName;
                                        ivr.Input = input;
                                    }

                                    item.Input_Values_Reneurons = item.Input_Values_Reneurons.OrderBy(a => a.Input.Order).ToList();

                                    rnetwork.MemoryManager.SetRneuronWithInputs(item);
                                }

                                //sw.Stop();
                                //System.Diagnostics.Debug.WriteLine($"Task: {Task.CurrentId}, Process Data: {sw.Elapsed}");
                            }));
                        }

                        /** old batching code
                        int totalRneurons = db.Rneurons.Count();
                        int pageCount = 100;
                        var helper = new StaticPagedList<Rneuron>(
                                        Enumerable.Empty<Rneuron>(), 1, pageCount, totalRneurons);
                        Parallel.For(1, helper.PageCount + 1, i =>
                        {

                            using (var rdb = new RlmDbEntities(databaseName))
                            {
                                var data = rdb.Rneurons.Include(a => a.Input_Values_Reneurons).OrderBy(u => u.ID).ToPagedList(i, helper.PageSize).ToList();

                                foreach (var item in data)
                                {
                                    // set input type and dotnettype
                                    foreach (var ivr in item.Input_Values_Reneurons)
                                    {
                                        var input = inputDic[ivr.Input_ID];
                                        ivr.InputType = input.Type;
                                        ivr.DotNetType = input.Input_Output_Type.DotNetTypeName;
                                        ivr.Input = input;
                                    }

                                    item.Input_Values_Reneurons = item.Input_Values_Reneurons.OrderBy(a => a.Input.Order).ToList();

                                    rnetwork.MemoryManager.SetRneuronWithInputs(item);
                                }
                            }
                        }); 

                        if (rneurons != null)
                        {
                            foreach (var item in rneurons)
                            {
                                // set input type and dotnettype
                                foreach (var ivr in item.Input_Values_Reneurons)
                                {
                                    var input = inputDic[ivr.Input_ID];
                                    ivr.InputType = input.Type;
                                    ivr.DotNetType = input.Input_Output_Type.DotNetTypeName;
                                    ivr.Input = input;
                                }

                                item.Input_Values_Reneurons = item.Input_Values_Reneurons.OrderBy(a => a.Input.Order).ToList();

                                rnetwork.MemoryManager.SetRneuronWithInputs(item);
                            }
                        } */

                        Task.WaitAll(subTasks.ToArray());                        
                        subProcessWatch.Stop();
                        System.Diagnostics.Debug.WriteLine($"Process data: {subProcessWatch.Elapsed}");
                        
                        rn_sw.Stop();
                        System.Diagnostics.Debug.WriteLine($"Total rneuron task elapsed: {rn_sw.Elapsed}");
                        ;
                    });
                    tasks.Add(rneuronsTask);

                    var solutionTask = outputTask.ContinueWith((t) => 
                    {
                        IEnumerable<Solution> solutions = null;
                        using (var ctx = new RlmDbEntities(databaseName))
                        {
                            // get solutions and save to MemoryManager cache
                            solutions = ctx.Solutions.Include(a => a.Output_Values_Solutions).ToList();
                        }

                        if (solutions != null)
                        {
                            foreach (var item in solutions)
                            {
                                rnetwork.MemoryManager.SetSolutionWithOutputs(item);
                            }
                        }
                    });
                    tasks.Add(solutionTask);
                        
                    Task.WaitAll(tasks.ToArray());
                        
                    retVal.Loaded = true;
                }
            }
            catch (Exception ex)
            {
                RlmDbLogger.Error(ex, databaseName, "LoadNetwork");
            }

            watch.Stop();
            System.Diagnostics.Debug.WriteLine($"Load Network Elapsed: {watch.Elapsed}");

            return retVal;
        }

        public void DropDB()
        {
            using (RlmDbEntities ctx = new RlmDbEntities(RlmDbEntities.MASTER_DB))
            {
                if (ctx.DBExists(databaseName))
                {
                    DropDB(ctx, databaseName);
                }
            }
        }

        public int DistinctCaseSessionsCount()
        {
            int ret = 0;

            using (RlmDbEntities db = new RlmDbEntities(databaseName))
            {
                var count = db.Database.SqlQuery<Int32>(@"SELECT COUNT(*) FROM(SELECT DISTINCT Session_ID FROM[Cases] main) AS a").First();

                ret = count;
            }

            return ret;
        }

        private static void DropDB(RlmDbEntities ctx, string databaseName)
        {
            if (ctx != null && !string.IsNullOrEmpty(databaseName))
            {
                ctx.DropDB(databaseName);
                System.Diagnostics.Debug.WriteLine("Db dropped...");
            }
        }
        #endregion
    }
}


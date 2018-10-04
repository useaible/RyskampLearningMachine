using RLM.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RLM.Models.Interfaces;
using System.Diagnostics;
using System.IO;
using System.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace RLM.Database
{
    public class RlmDbMgr
    {
        //private const int CASES_TASKS = 5;
        private int CASES_TASKS = 5;

        private int currCaseQueue = 0;
        public RlmBCQueues<Case>[] CaseWorkerQueues { get; private set; }

        private bool networkLoaded = true;
        private string databaseName;
        private long networkID;
        //private Rnetwork rlm;
        //private IRlmNetwork rnetwork;
        //private RlmDbBatchProcessor batchProcessor;
        private bool persistData;

        private long currLoadProgress = 0;
        private long totalLoadProgress = 0;

        private IRlmDbData rlmDbData;

        public RlmDbMgr(IRlmDbData rlmDbData, bool persistData = true)
        {
            this.rlmDbData = rlmDbData;
            this.persistData = persistData;

            if (rlmDbData != null)
                this.databaseName = rlmDbData.DatabaseName;

            if (persistData)
            {
                //this.batchProcessor = new RlmDbBatchProcessor(this.databaseName);

                //int coreCount = 0;
                //foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
                //{
                //    coreCount += int.Parse(item["NumberOfCores"].ToString());
                //}

                CASES_TASKS = Environment.ProcessorCount; //coreCount;
                CaseWorkerQueues = new RlmBCQueues<Case>[CASES_TASKS];
                for (int i = 0; i < CASES_TASKS; i++)
                {
                    CaseWorkerQueues[i] = new RlmBCQueues<Case>(i);
                    int caseIndex = i;
                    Task.Factory.StartNew(async () => { await saveCase(CaseWorkerQueues[caseIndex]); }, TaskCreationOptions.LongRunning);
                }
            }
        }

        public int TaskDelay { get; private set; } = 50;
        public int TaskRetryDelay { get; private set; } = 5000;

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
            Task T1 = Task.Factory.StartNew(async () =>
            {
                foreach (Session session in sessionqueue.GetConsumingEnumerable())
                {
                    await createSession(session);
                    Interlocked.Increment(ref taskCompletedCounter);
                    //Task.Delay(TaskDelay).Wait();
                }

            }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            return T1;
        }

        public Task StartSessionWorkerForUpdate(BlockingCollection<Session> sessionqueue, CancellationToken ct)
        {
            Task T1 = Task.Factory.StartNew(async () =>
            {
                foreach (Session session in sessionqueue.GetConsumingEnumerable())
                {
                    await updateSession(session);
                    Interlocked.Increment(ref taskCompletedCounter);
                }
            }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            return T1;
        }

        private ConcurrentQueue<Case> caseBox = new ConcurrentQueue<Case>();
        public Task StartCaseWorker(BlockingCollection<Case> casesQueue, CancellationToken ct)
        {
            Task T1 = Task.Factory.StartNew(() =>
            {
                Stopwatch st = new Stopwatch();
                st.Start();

                List<Case> cases = new List<Case>();
                foreach (Case theCase in casesQueue.GetConsumingEnumerable())
                {
                    cases.Add(theCase);

                    //Interlocked.Increment(ref taskCompletedCounter);
                    CaseWorkerQueues[currCaseQueue].WorkerQueues.Add(theCase);
                    cases.Clear();
                    currCaseQueue++;
                    if (currCaseQueue >= CASES_TASKS)
                    {
                        currCaseQueue = 0;
                    }
                }
                
                st.Stop();
                System.Diagnostics.Debug.WriteLine($"Case time: {st.Elapsed}");
            }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);

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
            #region LegacyCode
            //using (RlmDbEntities db = new RlmDbEntities(databaseName))
            //{
            //    try
            //    {
            //        //ready network for saving
            //        var _newNetwork = db.Rnetworks.Add(rnetwork);

            //        //add inputs to io_type
            //        foreach (Input input in inputs)
            //        {
            //            io_type.Inputs.Add(input);
            //        }

            //        //add outputs to io_type
            //        foreach (Output output in outputs)
            //        {
            //            io_type.Outputs.Add(output);
            //        }

            //        //ready io_type for saving
            //        var _ioType = db.Input_Output_Types.Add(io_type);

            //        //save to database
            //        await db.SaveChangesAsync();
            //    }
            //    catch (Exception ex)
            //    {
            //        RlmDbLogger.Error(ex, databaseName, "SaveNetwork");
            //    }
            //} 
            #endregion

            try
            {
                //ready network for saving
                rlmDbData.Create<Rnetwork>(rnetwork);


                //add inputs to io_type
                foreach (Input input in inputs)
                {
                    rlmDbData.Create<Input>(input);
                }

                //add outputs to io_type
                foreach (Output output in outputs)
                {
                    rlmDbData.Create<Output>(output);
                }

                //ready io_type for saving
                rlmDbData.Create<Input_Output_Type>(io_type);

            }
            catch (Exception ex)
            {
                RlmDbLogger.Error(ex, databaseName, "SaveNetwork");
            }
        }

        public void SaveNetwork(Rnetwork rnetwork, List<Input_Output_Type> io_types, List<Input> inputs, List<Output> outputs, IRlmNetwork rnn_net)
        {
            #region LegacyCode
            //using (RlmDbEntities db = new RlmDbEntities(databaseName))
            //{
            //    try
            //    {
            //        //ready network for saving
            //        var _newNetwork = db.Rnetworks.Add(rnetwork);

            //        foreach (var _in in inputs)
            //        {
            //            Input_Output_Type _iotype = io_types.FirstOrDefault(a => a.DotNetTypeName == _in.Input_Output_Type.DotNetTypeName);

            //            _in.Input_Output_Type = _iotype;

            //            db.Inputs.Add(_in);
            //        }

            //        foreach (var _out in outputs)
            //        {
            //            Input_Output_Type _iotype = io_types.FirstOrDefault(a => a.DotNetTypeName == _out.Input_Output_Type.DotNetTypeName);

            //            _out.Input_Output_Type = _iotype;

            //            db.Outputs.Add(_out);
            //        }

            //        //save to database
            //        db.SaveChanges();

            //        //rlm = _newNetwork;
            //        networkID = _newNetwork.Entity.ID;

            //        networkLoaded = true;
            //    }
            //    catch (Exception ex)
            //    {
            //        networkLoaded = false;
            //        RlmDbLogger.Error(ex, databaseName, "SaveNetwork");
            //    }
            //} 
            #endregion

            try
            {
                //ready network for saving
                rlmDbData.Create<_Rnetwork>(rnetwork);

                foreach(var item in io_types)
                {
                    rlmDbData.Create<_Input_Output_Type>(item);
                }

                foreach (var _in in inputs)
                {
                    Input_Output_Type _iotype = io_types.FirstOrDefault(a => a.DotNetTypeName == _in.Input_Output_Type.DotNetTypeName);
                    _in.Input_Output_Type_ID = _iotype.ID;
                    rlmDbData.Create<_Input>(_in);
                }

                foreach (var _out in outputs)
                {
                    Input_Output_Type _iotype = io_types.FirstOrDefault(a => a.DotNetTypeName == _out.Input_Output_Type.DotNetTypeName);
                    _out.Input_Output_Type_ID = _iotype.ID;
                    rlmDbData.Create<_Output>(_out);
                }

                //save to database
                //db.SaveChanges();

                //rlm = rnetwork;
                networkID = rnetwork.ID;

                networkLoaded = true;
            }
            catch (Exception ex)
            {
                networkLoaded = false;
                RlmDbLogger.Error(ex, databaseName, "SaveNetwork");
            }
        }

        //save session (create and update)
        private async Task createSession(Session session)
        {           
            int retryCnt = 0;
            Exception error = null;

            do
            {
                error = null;
                try
                {
                    if (networkLoaded)
                    {
                        #region LegacyCode
                        //using (RlmDbEntities db = new RlmDbEntities(databaseName))
                        //{
                        //    db.Sessions.Add(session);
                        //    await db.SaveChangesAsync();
                        //    session.CreatedToDb = true;

                        //    db.Sessions.Remove(session);
                        //    session.Cases = null;
                        //} 
                        #endregion

                        rlmDbData.Create<_Session>(session);
                        session.CreatedToDb = true;

                        //db.Sessions.Remove(session); TODO Removal
                        session.Cases = null;
                    }

                    if (retryCnt > 10)
                    {
                        System.Diagnostics.Debug.WriteLine($"Create session done after {retryCnt}");
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                    RlmDbLogger.Error(ex, databaseName, "createSession");
                    retryCnt++;
                    Thread.Sleep(TaskRetryDelay);
                }
            } while (error != null);            
        }

        private async Task updateSession(Session session)
        {           
            int retryCnt = 0;
            Exception error = null;

            do
            {
                error = null;
                try
                {
                    if (networkLoaded)
                    {
                        _Session existing = null;
                        while (existing == null)
                        {
                            #region LegacyCode
                            //using (RlmDbEntities db = new RlmDbEntities(databaseName))
                            //{
                            //    existing = await db.Sessions.FindAsync(session.ID);

                            //    if (existing != null)
                            //    {
                            //        existing.Hidden = session.Hidden;
                            //        existing.SessionScore = session.SessionScore;
                            //        existing.DateTimeStop = session.DateTimeStop;

                            //        int a = await db.SaveChangesAsync();
                            //        session.UpdatedToDb = true;

                            //        db.Sessions.Remove(existing);
                            //        existing.Cases = null;

                            //        if (retryCnt > 10)
                            //        {
                            //            System.Diagnostics.Debug.WriteLine($"Update session done after {retryCnt}");
                            //        }
                            //        break;
                            //    }
                            //} 
                            #endregion

                            existing = rlmDbData.FindByID<_Session>(session.ID); // null; /* await db.Sessions.FindAsync(session.ID); TODO find Existing Session*/

                            if (existing != null)
                            {
                                existing.Hidden = session.Hidden;
                                existing.SessionScore = session.SessionScore;
                                existing.DateTimeStop = session.DateTimeStop;

                                //int a = await db.SaveChangesAsync(); TODO save method
                                session.UpdatedToDb = true;

                                //db.Sessions.Remove(existing); TODO remove method
                                session.Cases = null;

                                rlmDbData.Update(existing);

                                if (retryCnt > 10)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Update session done after {retryCnt}");
                                }
                                break;
                            }

                            Thread.Sleep(TaskRetryDelay);
                            retryCnt++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                    RlmDbLogger.Error(ex, databaseName, "updateSession");
                    retryCnt++;
                    Thread.Sleep(TaskRetryDelay);
                }
            } while (error != null);            
        }

        //save case
        private async Task saveCase(RlmBCQueues<Case> bcCases)
        {
            Exception error = null;
            int retryCnt = 0;
            foreach (var item in bcCases.WorkerQueues.GetConsumingEnumerable())
            {
                bcCases.IsBusy = true;

                do
                {
                    try
                    {
                        error = null;
                        #region LegacyCode
                        //using (RlmDbEntities db = new RlmDbEntities(databaseName))
                        //{
                        //    db.Cases.Add(item);
                        //    await db.SaveChangesAsync();

                        //    item.Solution = null;
                        //    item.Rneuron = null;
                        //    item.Session = null;

                        //    #region CommentedOutPreDBOverhaul
                        //    //db.Cases.Local.Clear();
                        //    //db.Cases.Remove(item);

                        //    //if (theCase.Rneuron != null)
                        //    //{
                        //    //    theCase.Rneuron.SavedToDb = true;
                        //    //}

                        //    //if (theCase.Solution != null)
                        //    //{
                        //    //    theCase.Solution.SavedToDb = true;
                        //    //}

                        //    //theCase.SavedToDb = true;

                        //    //RlmDbLogger.Info(string.Format("\n[{0:d/M/yyyy HH:mm:ss:ms}]: Saving case...", DateTime.Now), databaseName); 
                        //    #endregion
                        //} 
                        #endregion

                        if (item.Rneuron != null)
                            saveRneuron(item.Rneuron);

                        if (item.Solution != null)
                            saveSolution(item.Solution);

                        rlmDbData.Create<_Case>(item);

                        //await db.SaveChangesAsync(); TODO save method

                        item.Solution = null;
                        item.Rneuron = null;
                        item.Session = null;
                        
                        if (retryCnt > 10)
                        {
                            System.Diagnostics.Debug.WriteLine($"Cases saved after {retryCnt}");
                        }

                        Interlocked.Increment(ref taskCompletedCounter);
                    }
                    catch (Exception ex)
                    {
                        RlmDbLogger.Error(ex, databaseName, "saveCase");

                        bcCases.WorkerQueues.Add(item);

                        //error = ex;
                        retryCnt++;
                        //Task.Delay(TaskRetryDelay).Wait();
                        Thread.Sleep(TaskRetryDelay);
                    }
                } while (error != null);

                retryCnt = 0;
                if (bcCases.WorkerQueues.Count == 0)
                {
                    bcCases.IsBusy = false;
                }
            }


            // TODO needs more testing and fix for Sessions concurrency bug
            //await batchProcessor.InsertCases(cases);
        }

        //save rneuron
        public void saveRneuron(Rneuron rneuron)
        {
            #region LegacyCode
            //using (RlmDbEntities db = new RlmDbEntities(databaseName))
            //{
            //    try
            //    {
            //        if (networkLoaded)
            //        {
            //            Task.Delay(5000).Wait();

            //            var _rneuron = db.Rneurons.Add(rneuron);

            //            _rneuron.Entity.Rnetwork_ID = networkID;

            //            foreach (var rn in _rneuron.Entity.Input_Values_Rneurons)
            //            {
            //                db.Input_Values_Reneurons.Add(rn);
            //            }

            //            await db.SaveChangesAsync();

            //            _rneuron.Entity.Cases = null;
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        RlmDbLogger.Error(ex, databaseName, "saveRneuron");
            //    }
            //} 
            #endregion

            try
            {
                if (networkLoaded)
                {
                    //Task.Delay(5000).Wait();

                    rlmDbData.Create<_Rneuron>(rneuron);

                    rneuron.Rnetwork_ID = networkID;

                    foreach (var rn in rneuron.Input_Values_Rneurons)
                    {
                        rlmDbData.Create<_Input_Values_Rneuron>(rn);
                    }

                    //await db.SaveChangesAsync(); //TODO Save method

                    rneuron.Cases = null;
                }
            }
            catch (Exception ex)
            {
                RlmDbLogger.Error(ex, databaseName, "saveRneuron");
            }
        }

        //save solution
        private void saveSolution(Solution solution)
        {
            #region LegacyCode
            //using (RlmDbEntities db = new RlmDbEntities(databaseName))
            //{
            //    try
            //    {
            //        if (networkLoaded)
            //        {
            //            var _solution = db.Solutions.Add(solution);

            //            foreach (var sol in _solution.Entity.Output_Values_Solutions)
            //            {
            //                db.Output_Values_Solutions.Add(sol);
            //            }

            //            await db.SaveChangesAsync();

            //            _solution.Entity.Cases = null;
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        RlmDbLogger.Error(ex, databaseName, "saveSolution");
            //    }
            //} 
            #endregion

            try
            {
                if (networkLoaded)
                {
                    rlmDbData.Create<_Solution>(solution);
                    
                    foreach (var sol in solution.Output_Values_Solutions)
                    {
                        rlmDbData.Create<_Output_Values_Solution>(sol);
                    }

                    //await db.SaveChangesAsync(); TODO Save

                    solution.Cases = null;
                }
            }
            catch (Exception ex)
            {
                RlmDbLogger.Error(ex, databaseName, "saveSolution");
            }
        }

        //load network
        public LoadRnetworkResult LoadNetwork(string name, IRlmNetwork rnetwork)
        {
            var retVal = new LoadRnetworkResult();
            IRlmNetwork rnn = rnetwork;
            List<Input> inputs = new List<Input>();
            List<Output> outputs = new List<Output>();
            totalLoadProgress = Int32.MaxValue;
            currLoadProgress = 0;
            //int tempBestSolProgress = 0;

            var watch = new Stopwatch();
            watch.Start();

            string dirPath = Path.Combine(RlmDbEntities.BCPLocation, databaseName);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            Rnetwork network;
            Task loadProgressUpdater;
            var loadProgressCancelTokenSrc = new CancellationTokenSource();
            try
            {
                //using (RlmDbEntities db = new RlmDbEntities(databaseName))
                //{
                //    //find network by name
                //    network = db.Rnetworks.FirstOrDefault(a => a.Name == name);
                //}

                network = rlmDbData.FindByName<_Rnetwork, Rnetwork>(name); // db.Rnetworks.FirstOrDefault(a => a.Name == name); TODO, find on table network with same name

                if (network == null)
                {
                    //Throw an error
                    Console.WriteLine("Network name '" + name + "' does not exist in the database:" + databaseName);
                }
                else
                {
                    CancellationToken cancelToken = loadProgressCancelTokenSrc.Token;
                    loadProgressUpdater = Task.Factory.StartNew(() =>
                    {
                        while (!cancelToken.IsCancellationRequested)
                        {
                            rnn.UpdateLoadNetworkProgress(currLoadProgress, totalLoadProgress);
                            Thread.Sleep(5000); 
                        }
                    }, cancelToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                    //this.rlm = network;
                    //this.rnetwork = rnetwork;

                    // set rnetwork details
                    retVal.CurrentNetworkId = networkID = network.ID;
                    retVal.CurrentNetworkName = network.Name;

                    IEnumerable<Input> rnnInputs = null;
                    IEnumerable<Output> rnnOutputs = null;

                    var tasks = new List<Task>();
                    #region Legacy code
                    //var bcpConnStr = Utility.ConfigFile.BcpConfig;
                    //var bcpPath = Utility.ConfigFile.BcpPath;

                    //if (string.IsNullOrWhiteSpace(bcpConnStr))
                    //{
                    //    string connStr = RlmDbEntities.ConnStr;
                    //    if (connStr.Contains(RlmDbEntities.DBNAME_PLACEHOLDER))
                    //    {
                    //        connStr = connStr.Replace(RlmDbEntities.DBNAME_PLACEHOLDER, databaseName);
                    //    }

                    //    var sqlConn = new SqlConnectionStringBuilder(connStr);
                    //    bcpConnStr = $"-S {sqlConn.DataSource} -d {sqlConn.InitialCatalog}";
                    //    if (sqlConn.IntegratedSecurity)
                    //        bcpConnStr += " -T";
                    //    else
                    //        bcpConnStr += $" -U {sqlConn.UserID} -P {sqlConn.Password}";
                    //}
                    //else
                    //{
                    //    if (bcpConnStr.Contains(RlmDbEntities.DBNAME_PLACEHOLDER))
                    //    {
                    //        bcpConnStr = bcpConnStr.Replace(RlmDbEntities.DBNAME_PLACEHOLDER, databaseName);
                    //    }
                    //} 
                    #endregion

                    //int taskCnt = Environment.ProcessorCount;

                    //TODO getting Data from DB
                    var countTask = Task.Run(() =>
                    {
                        Stopwatch cntWatch = new Stopwatch();
                        cntWatch.Start();

                        var subTasks = new List<Task>();
                        int totalInputs = 0;
                        int totalOutputs = 0;
                        int totalRneurons = 0;
                        //int totalIvrs = 0;
                        int totalSolutions = 0;
                        //int totalOvs = 0;
                        int totalSessions = 0;
                        int totalBestSols = 0;

                        subTasks.Add(Task.Run(() =>
                        {
                            #region Legacy code
                            //using (var ctx = new RlmDbEntities(databaseName))
                            //{
                            //    totalInputs = ctx.Inputs.Count();
                            //}
                            #endregion

                            totalInputs = rlmDbData.Count<_Input>();
                        }));

                        subTasks.Add(Task.Run(() =>
                        {
                            #region Legacy code
                            //using (var ctx = new RlmDbEntities(databaseName))
                            //{
                            //    totalRneurons = ctx.Rneurons.Count();
                            //} 
                            #endregion

                            totalRneurons = rlmDbData.Count<_Rneuron>();
                        }));

                        //subTasks[2] = Task.Run(() =>
                        //{
                        //    using (var ctx = new RlmDbEntities(databaseName))
                        //    {
                        //        totalIvrs = ctx.Input_Values_Reneurons.Count();
                        //    }
                        //});

                        subTasks.Add(Task.Run(() =>
                        {
                            #region Legacy code
                            //using (var ctx = new RlmDbEntities(databaseName))
                            //{
                            //    totalSolutions = ctx.Solutions.Count();
                            //} 
                            #endregion

                            totalSolutions = rlmDbData.Count<_Solution>();
                        }));

                        //subTasks[4] = Task.Run(() =>
                        //{
                        //    using (var ctx = new RlmDbEntities(databaseName))
                        //    {
                        //        totalOvs = ctx.Output_Values_Solutions.Count();
                        //    }
                        //});

                        subTasks.Add(Task.Run(() =>
                        {
                            #region Legacy code
                            //using (var ctx = new RlmDbEntities(databaseName))
                            //{
                            //    totalOutputs = ctx.Outputs.Count();
                            //} 
                            #endregion

                            totalOutputs = rlmDbData.Count<_Output>();
                        }));

                        subTasks.Add(Task.Run(() =>
                        {
                            #region Legacy code
                            //using (var ctx = new RlmDbEntities(databaseName))
                            //{
                            //    retVal.SessionCount = totalSessions = ctx.Sessions.Where(a => !a.Hidden).Count();
                            //} 
                            #endregion

                            retVal.SessionCount = totalSessions = rlmDbData.Count<_Session>();
                        }));

                        subTasks.Add(Task.Run(() =>
                        {
                            #region Legacy code
                            //string sql = @"SELECT
                            //                COUNT(*) [Cnt]
                            //            FROM
                            //            (
                            //                SELECT
                            //                    main.Rneuron_ID,
                            //                    main.Solution_ID
                            //                FROM [Cases] main
                            //                INNER JOIN [Sessions] sess ON main.[Session_ID] = sess.[ID]
                            //             WHERE sess.[Hidden] = 0
                            //                GROUP BY main.[Rneuron_ID], main.[Solution_ID]
                            //            ) a";
                            //using (var ctx = new RlmDbEntities(databaseName))
                            //{
                            //    totalBestSols = ctx.Database.SqlQuery<int>(sql).FirstOrDefault();
                            //} 
                            #endregion

                            totalBestSols = rlmDbData.CountBestSolutions();
                        }));

                        subTasks.Add(Task.Run(() =>
                        {
                            #region Legacy code
                            //using (var ctx = new RlmDbEntities(databaseName))
                            //{
                            //    try
                            //    {
                            //        retVal.CaseOrder = ctx.Cases.Max(a => a.Order);
                            //    }
                            //    catch (InvalidOperationException)
                            //    {
                            //        retVal.CaseOrder = 0;
                            //    }
                            //}
                            #endregion

                            retVal.CaseOrder = rlmDbData.Max((_Case c) => c.Order);
                        }));

                        Task.WaitAll(subTasks.ToArray());

                        var total = totalInputs + totalOutputs + totalRneurons + totalSolutions + totalSessions + totalBestSols;
                        // phantom count (this is to delay the final load progress as some task or threads delay in finishing but currLoadProgress already incremented)
                        total += 1; 

                        Interlocked.Exchange(ref totalLoadProgress, total);

                        //batchSizeRn = Convert.ToInt32(Math.Ceiling(totalRneurons / (double)taskCnt));
                        //batchSizeIvr = Convert.ToInt32(Math.Ceiling(totalIvrs / (double)taskCnt));
                        //batchSizeSl = Convert.ToInt32(Math.Ceiling(totalSolutions / (double)taskCnt));
                        //batchSizeOvs = Convert.ToInt32(Math.Ceiling(totalOvs / (double)taskCnt));

                        //const int MAX_BATCH = 100000;
                        //if (batchSizeRn > MAX_BATCH)
                        //    batchSizeRn = MAX_BATCH;
                        //if (batchSizeIvr > MAX_BATCH)
                        //    batchSizeIvr = MAX_BATCH;
                        //if (batchSizeSl > MAX_BATCH)
                        //    batchSizeSl = MAX_BATCH;
                        //if (batchSizeOvs > MAX_BATCH)
                        //    batchSizeOvs = MAX_BATCH;

                        //while (batchSizeIvr % totalInputs != 0)
                        //{
                        //    batchSizeIvr++;
                        //}

                        //while (batchSizeOvs % totalOutputs != 0)
                        //{
                        //    batchSizeOvs++;
                        //}

                        cntWatch.Stop();
                        System.Diagnostics.Debug.WriteLine($"Count elapsed: {cntWatch.Elapsed}");
                    });
                    tasks.Add(countTask);

                    var inputTask = Task.Run(() =>
                    {
                        #region Legacy code
                        //using (var ctx = new RlmDbEntities(databaseName))
                        //{
                        //    // get inputs
                        //    rnnInputs = ctx.Inputs
                        //        .Include(a => a.Input_Output_Type)
                        //        .OrderBy(a => a.Order)
                        //        .ToList();

                        //    Interlocked.Add(ref currLoadProgress, rnnInputs.Count);
                        //} 
                        #endregion

                        rnnInputs = rlmDbData.FindAll<_Input, Input>().OrderBy(a => a.Order).ToList();

                        foreach(var input in rnnInputs)
                        {
                            input.Input_Output_Type = rlmDbData.FindByID<_Input_Output_Type, Input_Output_Type>(input.Input_Output_Type_ID);
                        }

                        Interlocked.Add(ref currLoadProgress, rnnInputs.Count());

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
                        rnetwork.MemoryManager.SetArrays(rnetwork.Inputs.Count());
                    });
                    tasks.Add(inputTask);

                    var outputTask = Task.Run(() =>
                    {
                        #region Legacy code
                        //using (var ctx = new RlmDbEntities(databaseName))
                        //{
                        //    // get outputs
                        //    rnnOutputs = ctx.Outputs
                        //        .Include(a => a.Input_Output_Type)
                        //        .OrderBy(a => a.Order)
                        //        .ToList();

                        //    Interlocked.Add(ref currLoadProgress, rnnOutputs.Count);
                        //} 
                        #endregion

                        rnnOutputs = rlmDbData.FindAll<_Output, Output>().OrderBy(a => a.Order).ToList();

                        foreach (var output in rnnOutputs)
                        {
                            output.Input_Output_Type = rlmDbData.FindByID<_Input_Output_Type, Input_Output_Type>(output.Input_Output_Type_ID);
                        }

                        Interlocked.Add(ref currLoadProgress, rnnOutputs.Count());

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

                    var bestSolTask = Task.WhenAll(countTask).ContinueWith((t) =>
                    {
                        Stopwatch bs_watch = new Stopwatch();
                        bs_watch.Start();

                        //var filepath = $@"{dirPath}\bcp_bestSol.txt";
                        try
                        {
                            #region Legacy code
                            //var bestSolQuery = $@"SELECT
                            //    main.[Rneuron_ID] as [RneuronId],
                            //    main.[Solution_ID] as [SolutionId],
                            //    MAX(main.[CycleScore]) as [CycleScore],
                            //    MAX(sess.[SessionScore]) as [SessionScore],
                            //    MAX(main.[Order]) as [CycleOrder]
                            //FROM [{databaseName}].[dbo].[Cases] main with (nolock)
                            //INNER JOIN [{databaseName}].[dbo].[Sessions] sess ON main.[Session_ID] = sess.[ID]
                            //WHERE sess.[Hidden] = 0
                            //GROUP BY main.[Rneuron_ID], main.[Solution_ID]";

                            //var arg = $@"""{bestSolQuery}"" queryout {filepath} -c {bcpConnStr}";
                            //var startInfo = new ProcessStartInfo(bcpPath, arg) { WindowStyle = ProcessWindowStyle.Hidden };
                            //var process = System.Diagnostics.Process.Start(startInfo);
                            //process.WaitForExit();

                            //var bestSolBC = new BlockingCollection<BestSolution>();
                            //var consumer = Task.Run(() =>
                            //{
                            //    foreach(var item in bestSolBC.GetConsumingEnumerable())
                            //    {
                            //        rnetwork.MemoryManager.SetBestSolution(item);
                            //        Interlocked.Increment(ref currLoadProgress);
                            //    }
                            //});

                            //Parallel.ForEach(File.ReadLines(filepath), (line) =>
                            //{
                            //    string[] split = line.Split('\t');
                            //    var sol = new BestSolution()
                            //    {
                            //        RneuronId = Convert.ToInt64(split[0]),
                            //        SolutionId = Convert.ToInt64(split[1]),
                            //        CycleScore = Convert.ToDouble(split[2]),
                            //        SessionScore = Convert.ToDouble(split[3]),
                            //        CycleOrder = Convert.ToInt64(split[4])
                            //    };

                            //    bestSolBC.TryAdd(sol);
                            //});

                            //bestSolBC.CompleteAdding();
                            //consumer.Wait(); 
                            #endregion

                            foreach(var item in rlmDbData.LoadBestSolutions())
                            {
                                rnetwork.MemoryManager.SetBestSolution(item);
                                Interlocked.Increment(ref currLoadProgress);
                            }
                        }
                        finally
                        {
                            //File.Delete(filepath);

                            bs_watch.Stop();
                            System.Diagnostics.Debug.WriteLine($"Best solution task elapsed: {bs_watch.Elapsed}, Count: {rnetwork.MemoryManager.BestSolutions.Count.ToString("n")}");
                        }
                    });
                    tasks.Add(bestSolTask);

                    var sessionsTask = Task.WhenAll(countTask).ContinueWith((t) =>
                    {
                        Stopwatch sess_watch = new Stopwatch();
                        sess_watch.Start();

                        //var filepath = $@"{dirPath}\bcp_sessions.txt";
                        try
                        {
                            #region Legacy code
                            //var arg = $@"""select * from [{databaseName}].[dbo].[Sessions] with (nolock) where [Hidden] = 0"" queryout {filepath} -c {bcpConnStr}";
                            //var startInfo = new ProcessStartInfo(bcpPath, arg) { WindowStyle = ProcessWindowStyle.Hidden };
                            //var process = System.Diagnostics.Process.Start(startInfo);
                            //process.WaitForExit();

                            //var sessionsBC = new BlockingCollection<Session>();
                            //var consumer = Task.Run(() =>
                            //{
                            //    foreach (var item in sessionsBC.GetConsumingEnumerable())
                            //    {
                            //        rnetwork.MemoryManager.Sessions.TryAdd(item.ID, item);
                            //        Interlocked.Increment(ref currLoadProgress);
                            //    }
                            //});

                            //Parallel.ForEach(File.ReadLines(filepath), (line) =>
                            //{
                            //    string[] split = line.Split('\t');
                            //    var session = new Session()
                            //    {
                            //        ID = Convert.ToInt64(split[0]),
                            //        SessionGuid = split[1],
                            //        DateTimeStart = Convert.ToDateTime(split[2]),
                            //        DateTimeStop = Convert.ToDateTime(split[3]),
                            //        SessionScore = Convert.ToDouble(split[4]),
                            //        Hidden = split[5] == "1",
                            //        Rnetwork_ID = Convert.ToInt64(split[6]),
                            //        CreatedToDb = true,
                            //        UpdatedToDb = true
                            //    };

                            //    sessionsBC.TryAdd(session);
                            //});

                            //sessionsBC.CompleteAdding();
                            //consumer.Wait(); 
                            #endregion

                            foreach(var item in rlmDbData.FindAll<_Session, Session>())
                            {
                                item.CreatedToDb = true;
                                item.UpdatedToDb = true;
                                rnetwork.MemoryManager.Sessions.TryAdd(item.ID, item);
                                Interlocked.Increment(ref currLoadProgress);
                            }
                        }
                        finally
                        {
                            //File.Delete(filepath);

                            sess_watch.Stop();
                            System.Diagnostics.Debug.WriteLine($"Sessions task elapsed: {sess_watch.Elapsed}, Count: {rnetwork.MemoryManager.Sessions.Count.ToString("n")}");
                        }
                    });
                    tasks.Add(sessionsTask);

                    var rneuronsTask = Task.WhenAll(inputTask, countTask).ContinueWith((t) =>
                    {
                        Stopwatch rn_watch = new Stopwatch();
                        rn_watch.Start();

                        var rneurons = new ConcurrentDictionary<long, Rneuron>();
                        //var filepath = $@"{dirPath}\bcp_ivr.txt";

                        try
                        {
                            #region Legacy code
                            //var arg = $@"""select * from [{databaseName}].[dbo].[Input_Values_Rneuron] with (nolock)"" queryout {filepath} -c {bcpConnStr}";
                            //var startInfo = new ProcessStartInfo(bcpPath, arg) { WindowStyle = ProcessWindowStyle.Hidden };
                            //var process = System.Diagnostics.Process.Start(startInfo);
                            //process.WaitForExit(); 
                            #endregion

                            var inputDic = new ConcurrentDictionary<long, Input>(rnnInputs.ToDictionary(a => a.ID, a => a));
                            var rneuronsBC = new BlockingCollection<Rneuron>();
                            var consumer = Task.Run(() =>
                            {
                                foreach (var item in rneuronsBC.GetConsumingEnumerable())
                                {
                                    item.Input_Values_Rneurons = item.Input_Values_Rneurons.OrderBy(a => a.Input.Order).ToList();
                                    rnetwork.MemoryManager.SetRneuronWithInputs(item);
                                    Interlocked.Increment(ref currLoadProgress);
                                }
                            });

                            #region Legacy code
                            //Parallel.ForEach(File.ReadLines(filepath), line =>
                            //{
                            //    string[] split = line.Split('\t');
                            //    long rneuronId = Convert.ToInt64(split[3]);

                            //    Rneuron rn;
                            //    if (!rneurons.TryGetValue(rneuronId, out rn))
                            //    {
                            //        rn = new Rneuron() { ID = rneuronId };
                            //        if (!rneurons.TryAdd(rneuronId, rn))
                            //        {
                            //            rneurons.TryGetValue(rneuronId, out rn);
                            //        }
                            //    }

                            //    lock (rn.Input_Values_Rneurons)
                            //    {
                            //        long inputId = Convert.ToInt64(split[2]);
                            //        var input = inputDic[inputId];
                            //        rn.Input_Values_Rneurons.Add(new Input_Values_Rneuron()
                            //        {
                            //            ID = Convert.ToInt64(split[0]),
                            //            Value = split[1],
                            //            Input_ID = inputId,
                            //            Rneuron_ID = rneuronId,
                            //            Input = input,
                            //            InputType = input.Type,
                            //            DotNetType = input.Input_Output_Type.DotNetTypeName
                            //        });

                            //        if (rn.Input_Values_Rneurons.Count >= rnnInputs.Count)
                            //        {
                            //            rneuronsBC.Add(rn);
                            //        }
                            //    }
                            //}); 
                            #endregion

                            foreach(var item in rlmDbData.FindAll<_Input_Values_Rneuron, Input_Values_Rneuron>())
                            {
                                long rneuronId = item.Rneuron_ID;

                                Rneuron rn;
                                if (!rneurons.TryGetValue(rneuronId, out rn))
                                {
                                    rn = new Rneuron() { ID = rneuronId };
                                    if (!rneurons.TryAdd(rneuronId, rn))
                                    {
                                        rneurons.TryGetValue(rneuronId, out rn);
                                    }
                                }

                                long inputId = item.Input_ID;
                                var input = inputDic[inputId];

                                item.Input = input;
                                item.InputType = input.Type;
                                item.DotNetType = input.Input_Output_Type.DotNetTypeName;

                                rn.Input_Values_Rneurons.Add(item);

                                if (rn.Input_Values_Rneurons.Count >= rnnInputs.Count())
                                {
                                    rneuronsBC.Add(rn);
                                }
                            }

                            rneuronsBC.CompleteAdding();
                            consumer.Wait();
                        }
                        finally
                        {
                            //File.Delete(filepath);

                            rn_watch.Stop();
                            System.Diagnostics.Debug.WriteLine($"Total Rneurons task elapsed: {rn_watch.Elapsed}, Count: {rneurons.Count.ToString("n")}");
                        }
                    });
                    tasks.Add(rneuronsTask);

                    var solutionsTask = Task.WhenAll(outputTask, countTask).ContinueWith((t) =>
                    {
                        Stopwatch rn_watch = new Stopwatch();
                        rn_watch.Start();

                        var solutions = new ConcurrentDictionary<long, Solution>();
                        //var filepath = $@"{dirPath}\bcp_ovs.txt";

                        try
                        {
                            #region Legacy code
                            //var arg = $@"""select * from [{databaseName}].[dbo].[Output_Values_Solution] with (nolock)"" queryout {filepath} -c {bcpConnStr}";
                            //var startInfo = new ProcessStartInfo(bcpPath, arg) { WindowStyle = ProcessWindowStyle.Hidden };
                            //var process = System.Diagnostics.Process.Start(startInfo);
                            //process.WaitForExit(); 
                            #endregion

                            var outputDic = new ConcurrentDictionary<long, Output>(rnnOutputs.ToDictionary(a => a.ID, a => a));
                            var solutionsBC = new BlockingCollection<Solution>();
                            var consumer = Task.Run(() =>
                            {
                                foreach (var item in solutionsBC.GetConsumingEnumerable())
                                {
                                    item.Output_Values_Solutions = item.Output_Values_Solutions.OrderBy(a => a.Output.Order).ToList();
                                    rnetwork.MemoryManager.SetSolutionWithOutputs(item);
                                    Interlocked.Increment(ref currLoadProgress);
                                }
                            });

                            #region Legacy code
                            //Parallel.ForEach(File.ReadLines(filepath), line =>
                            //{
                            //    string[] split = line.Split('\t');
                            //    long solutionId = Convert.ToInt64(split[3]);

                            //    Solution sol;
                            //    if (!solutions.TryGetValue(solutionId, out sol))
                            //    {
                            //        sol = new Solution() { ID = solutionId };
                            //        if (!solutions.TryAdd(solutionId, sol))
                            //        {
                            //            solutions.TryGetValue(solutionId, out sol);
                            //        }
                            //    }

                            //    lock (sol.Output_Values_Solutions)
                            //    {
                            //        var outputId = Convert.ToInt64(split[2]);
                            //        var output = outputDic[outputId];
                            //        sol.Output_Values_Solutions.Add(new Output_Values_Solution()
                            //        {
                            //            ID = Convert.ToInt64(split[0]),
                            //            Value = split[1],
                            //            Output = output,
                            //            Output_ID = outputId,
                            //            Solution_ID = solutionId
                            //        });

                            //        if (sol.Output_Values_Solutions.Count >= rnnOutputs.Count)
                            //        {
                            //            solutionsBC.Add(sol);
                            //        }
                            //    }
                            //}); 
                            #endregion

                            foreach(var item in rlmDbData.FindAll<_Output_Values_Solution, Output_Values_Solution>())
                            {
                                long solutionId = item.Solution_ID;

                                Solution sol;
                                if (!solutions.TryGetValue(solutionId, out sol))
                                {
                                    sol = new Solution() { ID = solutionId };
                                    if (!solutions.TryAdd(solutionId, sol))
                                    {
                                        solutions.TryGetValue(solutionId, out sol);
                                    }
                                }

                                var outputId = item.Output_ID;
                                var output = outputDic[outputId];

                                item.Output = output;

                                sol.Output_Values_Solutions.Add(item);

                                if (sol.Output_Values_Solutions.Count >= rnnOutputs.Count())
                                {
                                    solutionsBC.Add(sol);
                                }
                            }

                            solutionsBC.CompleteAdding();
                            consumer.Wait();
                        }
                        finally
                        {
                            //File.Delete(filepath);

                            rn_watch.Stop();
                            System.Diagnostics.Debug.WriteLine($"Total Solutions task elapsed: {rn_watch.Elapsed}, Count: {solutions.Count.ToString("n")}");
                        }
                    });
                    tasks.Add(solutionsTask);

                    Task.WaitAll(tasks.ToArray());
                    retVal.Loaded = true;
                }
            }
            catch (Exception ex)
            {
                RlmDbLogger.Error(ex, databaseName, "LoadNetwork");
                throw;
            }
            finally
            {
                // for phantom count (see above)
                Interlocked.Increment(ref currLoadProgress);

                watch.Stop();
                loadProgressCancelTokenSrc.Cancel();
                rnn.UpdateLoadNetworkProgress(currLoadProgress, totalLoadProgress);
                System.Diagnostics.Debug.WriteLine($"Load Network Elapsed: {watch.Elapsed}");
            }

            return retVal;
        }

        public void CascadeDelete(long solutionId)
        {
            string guid = Guid.NewGuid().ToString("N");
            string sql = $@"DECLARE @tmp_cases_{guid} TABLE (Case_ID BIGINT, Session_ID BIGINT);

                        INSERT INTO @tmp_cases_{guid} SELECT ID [Case_ID], Session_ID FROM Cases WHERE Solution_ID = @p0;

                        DELETE FROM Cases WHERE Session_ID IN (SELECT Session_ID FROM @tmp_cases_{guid});
                        DELETE FROM Sessions WHERE ID IN (SELECT Session_ID FROM @tmp_cases_{guid});";

            //using (RlmDbEntities ctx = new RlmDbEntities(databaseName))
            //{
            //    ctx.Database.ExecuteSqlCommand(sql, solutionId);                
            //}

            //rlmDbData.CascadeDelete(sql, solutionId);
        }

        public IEnumerable<long> GetSolutionIdsForOutputs(IDictionary<long, IEnumerable<string>> outputs)
        {
            IEnumerable<long> retVal = null;

            string guid = Guid.NewGuid().ToString("N");

            StringBuilder sql = new StringBuilder();
            sql.AppendLine($@"DECLARE @tmp_outvals_{guid} TABLE(Output_ID BIGINT, VALUE NVARCHAR(MAX));
                INSERT INTO @tmp_outvals_{guid}
                VALUES ");

            int pCnt = 0;
            var parameters = new List<object>();
            foreach (var pair in outputs)
            {
                sql.AppendLine();
                foreach (var item in pair.Value)
                {
                    sql.Append("(");
                    parameters.Add(pair.Key);
                    sql.Append($"@p{pCnt++}");
                    sql.Append(",");
                    parameters.Add(item);
                    sql.Append($"@p{pCnt++}");
                    sql.Append(")");

                    if (pair.Value.Last() != item)
                    {
                        sql.Append(",");
                    }
                }
            }

            sql.AppendLine();
            sql.AppendLine($@"SELECT DISTINCT
                            a.Solution_ID
                        FROM Output_Values_Solution a
                        INNER JOIN @tmp_outvals_{guid} b ON a.Output_ID = b.Output_ID AND a.Value = b.VALUE");
            
            // TODO
            using (var ctx = new RlmDbEntities(databaseName))
            {
                //retVal = ctx.Database.SqlQuery<long>(sql.ToString(), parameters.ToArray()).ToList();
                // todo migrate to ef core
            }

            return retVal;
        }


        public void DropDB()
        {
            // TODO Drop DB
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

            // TODO
            using (RlmDbEntities db = new RlmDbEntities(databaseName))
            {
                //var count = db.Database.SqlQuery<Int32>(@"SELECT COUNT(*) FROM(SELECT DISTINCT Session_ID FROM[Cases] main) AS a").First();

                //ret = count;
                // TODO migrate to ef core
            }

            return ret;
        }

        private static void DropDB(RlmDbEntities ctx, string databaseName)
        {
            // TODO Drop DB
            if (ctx != null && !string.IsNullOrEmpty(databaseName))
            {
                ctx.DropDB(databaseName);
                System.Diagnostics.Debug.WriteLine("Db dropped...");
            }
        }
        
        #endregion
    }
}
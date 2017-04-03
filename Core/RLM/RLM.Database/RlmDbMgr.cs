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

namespace RLM.Database
{
    public class RlmDbMgr
    {
        private bool networkLoaded = true;
        private string databaseName;
        private long networkID;
        private Rnetwork rlm;

        public RlmDbMgr(string databaseName)
        {
            this.databaseName = databaseName;
        }

        public int TaskDelay { get; private set; } = 50;

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
            Task T1 = Task.Run(() =>
            {
                foreach (Session session in sessionqueue.GetConsumingEnumerable())
                {
                    createSession(session);
                    Interlocked.Increment(ref taskCompletedCounter);
                    Task.Delay(TaskDelay).Wait();
                }

            }, ct);

            return T1;
        }

        public Task StartSessionWorkerForUpdate(BlockingCollection<Session> sessionqueue, CancellationToken ct)
        {
            Task T1 = Task.Run(() =>
            {
                foreach (Session session in sessionqueue.GetConsumingEnumerable())
                {
                    updateSession(session);
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
                foreach (Case theCase in casesQueue.GetConsumingEnumerable())
                {
                    //caseBox.Enqueue(theCase);
                    saveCase(theCase);
                    Interlocked.Increment(ref taskCompletedCounter);
                    Task.Delay(TaskDelay).Wait();
                }

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
        private async void createSession(Session session)
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

        private async void updateSession(Session session)
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

                            Task.Delay(50).Wait();
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

            using (RlmDbEntities db = new RlmDbEntities(databaseName))
            {
                try
                {
                    //find network by name
                    var network = from net in db.Rnetworks
                                  where net.Name.ToLower() == name.ToLower()
                                  select net;

                    if (network.Count() == 0)
                    {
                        //Throw an error
                        Console.WriteLine("Network name '" + name + "' does not exist in the database:" + db.Database.Connection.ToString());
                    }
                    else
                    {
                        var rnetworkFromDb = network.First<Rnetwork>();
                        this.rlm = rnetworkFromDb;

                        // set rnetwork details
                        retVal.CurrentNetworkId = networkID = this.rlm.ID;
                        retVal.CurrentNetworkName = this.rlm.Name;

                        // get inputs
                        var rnnInputs = db.Inputs
                            .Include(a => a.Input_Output_Type)
                            .OrderBy(a=>a.Order)
                            .ToList();

                        rnetwork.Inputs = rnnInputs.Select(a => new RlmIO()
                        {
                            ID = a.ID,
                            Name = a.Name,
                            DotNetType = a.Input_Output_Type.DotNetTypeName,
                            Min = a.Min,
                            Max = a.Max,
                            Type = a.Type
                        });

                        rnetwork.InputMomentums.Clear();
                        foreach(var item in rnetwork.Inputs)
                        {
                            if (item.Type == Enums.RlmInputType.Linear)
                            {
                                rnetwork.InputMomentums.Add(item.ID, new RlmInputMomentum() { InputID = item.ID });
                            }
                        }

                        // get outputs
                        var rnnOutputs = db.Outputs
                            .Include(a => a.Input_Output_Type)
                            .OrderBy(a => a.Order)
                            .ToList();

                        rnetwork.Outputs = rnnOutputs.Select(a => new RlmIO()
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

                        // initialize MemoryManager dictionaries (Rneurons, Solutions, etc...)
                        //rnetwork.MemoryManager.InitStorage(rnetwork.Inputs.Select(a =>
                        //{
                        //    return new Input
                        //    {
                        //        ID = a.ID,
                        //        Max = a.Max,
                        //        Min = a.Min,
                        //        Name = a.Name,
                        //        Rnetwork_ID = rnetwork.CurrentNetworkID,
                        //        Type = a.Type
                        //    };
                        //}).ToList(), rnetwork.Outputs.Select(a => {
                        //    return new Output
                        //    {
                        //        ID = a.ID,
                        //        Max = a.Max,
                        //        Min = a.Min,
                        //        Name = a.Name,
                        //        Rnetwork_ID = rnetwork.CurrentNetworkID
                        //    };
                        //}).ToList());

                        // get sessions and save to MemoryManager cache
                        var sessions = db.Sessions.OrderBy(a=>a.DateTimeStart).ToList();
                        foreach (var item in sessions)
                        {
                            rnetwork.MemoryManager.Sessions.TryAdd(item.ID, item);
                        }

                        //set sessions count
                        retVal.SessionCount = sessions.Count;

                        // set CaseOrder to the last case saved in db
                        var lastCase = db.Cases.OrderByDescending(a => a.Order).FirstOrDefault();
                        if (lastCase != null)
                        {
                            retVal.CaseOrder = lastCase.Order;
                        }

                        // made input dictionary to help in setting the Dynamic inputs
                        var inputDic = rnnInputs.ToDictionary(a => a.ID, a => a);

                        // get Rneurons and save to MemoryManager cache
                        //var rneurons = db.Rneurons.Include(a => a.Input_Values_Reneurons).ToList();

                        //batching
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
                        //end batching

                        //foreach (var item in rneurons)
                        //{
                        //    // set input type and dotnettype
                        //    foreach (var ivr in item.Input_Values_Reneurons)
                        //    {
                        //        var input = inputDic[ivr.Input_ID];
                        //        ivr.InputType = input.Type;
                        //        ivr.DotNetType = input.Input_Output_Type.DotNetTypeName;
                        //        ivr.Input = input;
                        //    }

                        //    item.Input_Values_Reneurons = item.Input_Values_Reneurons.OrderBy(a => a.Input.Order).ToList();

                        //    rnetwork.MemoryManager.SetRneuronWithInputs(item);
                        //}

                        // get solutions and save to MemoryManager cache
                        var solutions = db.Solutions.Include(a => a.Output_Values_Solutions).ToList();
                        foreach (var item in solutions)
                        {
                            rnetwork.MemoryManager.SetSolutionWithOutputs(item);
                        }

                        // get best solutions and save to MemoryManager cache
                        //var solCount = db.Database.SqlQuery<Int32>(@"SELECT COUNT(*) FROM(SELECT 

                        //        main.[Rneuron_ID] as [RneuronId],
                        //        main.[Solution_ID] as [SolutionId],
                        //        MAX(main.[CycleScore]) as [CycleScore],
                        //        MAX(sess.[SessionScore]) as [SessionScore],
                        //        MAX(main.[CycleEndTime]) as [CycleEndTime]
                        //    FROM[Cases] main
                        //    INNER JOIN[Sessions] sess ON main.[Session_ID] = sess.[ID]
                        //    GROUP BY main.[Rneuron_ID], main.[Solution_ID]) AS a").First();


                        var bestSolutions = db.Database.SqlQuery<BestSolution>(@"
                            SELECT 
	                            main.[Rneuron_ID] as [RneuronId],
	                            main.[Solution_ID] as [SolutionId],
	                            MAX(main.[CycleScore]) as [CycleScore],
	                            MAX(sess.[SessionScore]) as [SessionScore],
	                            MAX(main.[CycleEndTime]) as [CycleEndTime]
                            FROM [Cases] main
                            INNER JOIN [Sessions] sess ON main.[Session_ID] = sess.[ID]
                            GROUP BY main.[Rneuron_ID], main.[Solution_ID]")
                            .ToList();
                        foreach (var item in bestSolutions)
                        {
                            rnetwork.MemoryManager.SetBestSolution(item);
                        }

                        retVal.Loaded = true;
                    }
                }
                catch (Exception ex)
                {
                    RlmDbLogger.Error(ex, databaseName, "LoadNetwork");
                }
            }

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


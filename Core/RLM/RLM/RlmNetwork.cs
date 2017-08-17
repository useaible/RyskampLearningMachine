// Copyright 2017 Ryskamp Innovations LLC
// License Available through the RLM License Agreement
// https://github.com/useaible/RyskampLearningMachine/blob/dev-branch/License.md

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using RLM.Database;
using RLM.Models;
using RLM.Enums;
using RLM.Memory;
using RLM.Models.Interfaces;

namespace RLM
{
    //Delegates
    public delegate void SessionCompleteDelegate(Int64 SessionID, RlmNetwork net);

    public delegate void CycleCompleteDelegate(RlmCyclecompleteArgs e);


    //Core Network Class, this is used by client programs to access all network functions
    public class RlmNetwork : IRlmNetwork
    {
        const int MOMENTUM_ADJUSTMENT = 21;
        const int CACHE_MARGIN = 0;
        const bool USE_MOM_AVG = false;

        protected bool consoleDisplay = false;

        //Events
        public event SessionCompleteDelegate SessionComplete;
        public event CycleCompleteDelegate CycleComplete;
        public event DataPersistenceCompleteDelegate DataPersistenceComplete;
        public event DataPersistenceProgressDelegate DataPersistenceProgress;
        
        public bool PersistData { get; private set; }
        public IManager MemoryManager { get; private set; }

        public Case CurrentCase { get; internal set; }
        public long CaseOrder { get; private set; }

        public Int64 CurrentNetworkID { get; private set; } = -1;
        public String CurrentNetworkName { get; private set; } = null;
        public Int64 CurrentSessionID { get; private set; } = -1;
        public Int64 LastSessionID { get; private set; } = -1;
                
        public IEnumerable<RlmIO> Inputs { get; set; }
        public IEnumerable<RlmIO> Outputs { get; set; }

        public int SessionCount { get; private set; }
        protected int? SessionCountInitial { get; set; }
        protected Int32 SessionCountDiff
        {
            get
            {
                if (SessionCountInitial.HasValue)
                {
                    return SessionCount - SessionCountInitial.Value;
                }
                else
                {
                    return SessionCount;
                }
            }
        }
        /// <summary>
        /// The set number of sessions
        /// </summary>
        public int NumSessions { get; set; } = 100;

        // RNN randomness settings

        /// <summary>
        /// The starting percentage of randomness to be used by the engine
        /// </summary>
        public int StartRandomness { get; set; } = 100;
        /// <summary>
        /// The last percentage of randomness where the engine halts
        /// </summary>
        public int EndRandomness { get; set; } = 0;
        public double? RandomnessInterval { get; set; } = null;

        // RNN linear barcket settings

        /// <summary>
        /// Maximum value set for the range of Linear Type Training
        /// </summary>
        public double MaxLinearBracket { get; set; } = 15;
        /// <summary>
        /// Minimum value set for the range of Linear Type Training
        /// </summary>
        public double MinLinearBracket { get; set; } = 3;
        public double PredictLinear { get; set; } = 0;
        public double? LinearCap { get; set; } = null; 
                
        private double RandomnessSessionIntervalValue
        {
            get
            {
                double retVal = NumSessions;
                if (RandomnessInterval.HasValue)
                {
                    retVal = Convert.ToDouble(NumSessions) * (RandomnessInterval.Value / 100D);
                }
                return retVal;
            }
        }        
        private double RandomnessPerStep
        {
            get
            {
                double retVal = 0;
                if (NumSessions > 0)
                {
                    if (RandomnessInterval.HasValue)
                    {
                        retVal = (Convert.ToDouble(StartRandomness) - Convert.ToDouble(EndRandomness)) / RandomnessSessionIntervalValue;
                    }
                    else
                    {
                        retVal = (NumSessions == 1) ? Convert.ToDouble(StartRandomness) : (Convert.ToDouble(StartRandomness) - Convert.ToDouble(EndRandomness)) / Convert.ToDouble(NumSessions - 1);
                    }
                }
                return retVal;
            }
        }
        public double RandomnessCurrentValue
        {
            get
            {
                double retVal = 0;

                var sessCntDiff = SessionCountDiff;
                if (RandomnessInterval.HasValue)
                {
                    sessCntDiff = SessionCountDiff % Convert.ToInt32(RandomnessSessionIntervalValue);
                }

                retVal = Math.Round(StartRandomness - (RandomnessPerStep * sessCntDiff), 2);                
                if (retVal < EndRandomness)
                {
                    retVal = EndRandomness;
                }
                return retVal;
            }
        }

        private double LinearSessionCapValue
        {
            get
            {
                double retVal = NumSessions;
                if (LinearCap.HasValue)
                {
                    retVal = Convert.ToDouble(NumSessions) * (LinearCap.Value / 100D);
                }
                return retVal;
            }
        }
        private double LinearTolerancePerStep
        {
            get
            {
                double retVal = 0;
                if (NumSessions > 0)
                {
                    if (LinearCap.HasValue)
                    {
                        retVal = (MaxLinearBracket - MinLinearBracket) / LinearSessionCapValue;
                    }
                    else
                    {
                        retVal = (NumSessions == 1) ? MaxLinearBracket : (MaxLinearBracket - MinLinearBracket) / Convert.ToDouble(NumSessions - 1);
                    }
                }
                return retVal;
            }
        }
        public double LinearToleranceCurrentValue
        {
            get
            {
                double retVal = 0;
                
                retVal = Math.Round(MaxLinearBracket - (LinearTolerancePerStep * SessionCountDiff), 2);
                if (retVal < MinLinearBracket)
                {
                    retVal = MinLinearBracket;
                }

                if (LinearCap.HasValue && SessionCountDiff >= LinearSessionCapValue)
                {
                    retVal = MinLinearBracket;
                }

                return retVal;
            }
        }


        public int CousinNodeSearchToleranceIncrement { get; set; } // TODO enforce min 1 max 100 

        public string DatabaseName { get; private set; }
        
        public IDictionary<long, RlmInputMomentum> InputMomentums { get; private set; } = new Dictionary<long, RlmInputMomentum>();
        public RlmSessionCaseHistory SessionCaseHistory { get; set; }

        #region Static

        public static void RestoreDB(string databaseName, bool simpleRecovery = false)
        {
            using (RlmDbEntities ctx = new RlmDbEntities(RlmDbEntities.MASTER_DB))
            {
                bool dbExists = ctx.DBExists(databaseName);
                bool fileBackupExists = ctx.FileBackupExists(databaseName);

                if (!dbExists && !fileBackupExists)
                {
                    ctx.RestoreDBFromTemplate(databaseName);
                    System.Diagnostics.Debug.WriteLine("Db restored from template...");
                }
                else if (!dbExists && fileBackupExists)
                {
                    ctx.RestoreDB(databaseName);
                    System.Diagnostics.Debug.WriteLine("Db restored...");
                }

                if (simpleRecovery)
                {
                    ctx.SetDBRecoveryMode(databaseName, DBRecoveryMode.Simple);
                }
            }
        }

        public static void BackupDB(string databaseName)
        {
            using (RlmDbEntities ctx = new RlmDbEntities(RlmDbEntities.MASTER_DB))
            {
                if (ctx.FileBackupExists(databaseName))
                {
                    ctx.DeleteFileBackup(databaseName);
                    System.Diagnostics.Debug.WriteLine("Deleted file backup...");
                }

                if (ctx.DBExists(databaseName))
                {
                    ctx.BackupDB(databaseName);
                    System.Diagnostics.Debug.WriteLine("Db backed up...");

                    DropDB(ctx, databaseName);
                }
            }
        }

        public static void DropDB(string databaseName)
        {
            using (RlmDbEntities ctx = new RlmDbEntities(RlmDbEntities.MASTER_DB))
            {
                if (ctx.DBExists(databaseName))
                {
                    DropDB(ctx, databaseName);
                }
            }
        }

        private static void DropDB(RlmDbEntities ctx, string databaseName)
        {
            if (ctx != null && !string.IsNullOrEmpty(databaseName))
            {
                ctx.DropDB(databaseName);
                System.Diagnostics.Debug.WriteLine("Db dropped...");
            }
        }

        public static bool Exists(string databaseName)
        {
            bool retVal = false;
            using (RlmDbEntities ctx = new RlmDbEntities(RlmDbEntities.MASTER_DB))
            {
                retVal = ctx.DBExists(databaseName);
            }
            return retVal;
        }

        #endregion

        #region Constructor
        /// <summary>
        /// default contstructor, creates "RyskampLearningMachine" database
        /// </summary>
        /// <param name="persistData">Allows you to turn on/off the data persistence feature of the RLM. Turned on by default.</param>
        public RlmNetwork(bool persistData = true)
        {
            PersistData = persistData;
            if (PersistData)
                DatabaseName = RlmDbEntities.DetermineDbName();
            Initialize();
        }
        /// <summary>
        /// sets your preferred database name
        /// </summary>
        /// <param name="databaseName">database name</param>
        public RlmNetwork(string databaseName)
        {
            PersistData = true;
            DatabaseName = databaseName;
            Initialize();
            //MemoryManager.StartRnnDbWorkers();

            //using (RnnDbEntities db = new RnnDbEntities(DatabaseName))
            //{
            //    db.SaveChanges(); //On the constructor we save changes no matter what to create DB if not exists
            //}
        }

        #endregion Constructor

        #region Core Methods
        /// <summary>
        /// Sets up a new network and sets the network as current network to use in training.
        /// </summary>
        /// <param name="name">Your preferred network name</param>
        /// <param name="inputs">List of input types for your created network</param>
        /// <param name="outputs">List of output types for your created network</param>
        public void NewNetwork(string name, IEnumerable<RlmIO> inputs, IEnumerable<RlmIO> outputs)
        {
            //Create new network
            Rnetwork newnet = new Rnetwork { ID = Util.GenerateHashKey(name), Name = name, DateTimeCreated=DateTime.Now };
                
            CurrentNetworkID = newnet.ID;
            CurrentNetworkName = newnet.Name;

            // initialize Case Order
            CaseOrder = 0;

            if (consoleDisplay)
                Console.WriteLine("Create new network: " + newnet.Name);

            //Inputs
            var inputsForDb = new List<Input>();
            var outputsForDb = new List<Output>();
            var ioTypesForDb = new List<Input_Output_Type>();
            int count = 0;
            foreach (RlmIO io in inputs)
            {
                count++;

                var newio = ProcessNewIO(newnet, io, false) as Input;
                Input newin = new Input() { HashedKey = newio.HashedKey, ID = newio.ID, Input_Output_Type = newio.Input_Output_Type, Input_Output_Type_ID = newio.Input_Output_Type_ID, Max = newio.Max, Min = newio.Min, Name = newio.Name, Input_Values_Reneurons = newio.Input_Values_Reneurons, Rnetwork = newio.Rnetwork, Rnetwork_ID = newio.Rnetwork_ID, Type = newio.Type, Order = count };
                inputsForDb.Add(newin);
                InputMomentums.Add(newin.ID, new RlmInputMomentum() { InputID = newin.ID });

                if (!ioTypesForDb.Any(a => a.DotNetTypeName == newio.Input_Output_Type.DotNetTypeName))
                {
                    ioTypesForDb.Add(newin.Input_Output_Type);
                    newio.Input_Output_Type = null;
                }                
            }
            Inputs = inputs;

            //Outputs                
            count = 0;
            foreach (RlmIO io in outputs)
            {
                count++;

                var newio = ProcessNewIO(newnet, io, true) as Output;
                Output newout = new Output() { HashedKey = newio.HashedKey, ID = newio.ID, Input_Output_Type = newio.Input_Output_Type, Input_Output_Type_ID = newio.Input_Output_Type_ID, Max = newio.Max, Min = newio.Min, Name = newio.Name, Output_Values_Solutions = newio.Output_Values_Solutions, Rnetwork = newio.Rnetwork, Rnetwork_ID = newio.Rnetwork_ID, Order =  count};
                outputsForDb.Add(newout);

                if (!ioTypesForDb.Any(a => a.DotNetTypeName == newio.Input_Output_Type.DotNetTypeName))
                {
                    ioTypesForDb.Add(newout.Input_Output_Type);
                    newio.Input_Output_Type = null;
                }

                // add dynamic output collection
                MemoryManager.DynamicOutputs.TryAdd(newio.ID, new HashSet<SolutionOutputSet>());
            }
            Outputs = outputs;
            
            SessionCountInitial = SessionCount;

            //create new network
            if (PersistData)
            {
                MemoryManager.NewNetwork(newnet, ioTypesForDb, inputsForDb, outputsForDb, this);
            }
        }

        /// <summary>
        /// Loads the first network in the database, sorted by ID
        /// </summary>
        /// <returns>Returns true if network is successfully loaded</returns>
        public bool LoadNetwork()
        {
            string networkName = null;
            using (RlmDbEntities db = new RlmDbEntities(DatabaseName))
            {
                networkName = db.Rnetworks.Select(a => a.Name).FirstOrDefault();
            }

            if (networkName == null)
            {
                return false;
            }
            else
            {
                return LoadNetwork(networkName);
            }
        }

        /// <summary>
        /// Loads selected network’s data (input types, output types, training data, network settings) from the Database into memory lists.
        /// </summary>
        /// <param name="name">the network you prefer to load</param>
        /// <remarks>
        /// Is used as an indicator if there’s a need to create a new network.
        /// </remarks>
        /// <returns>Returns true if network is successfully loaded</returns>
        public bool LoadNetwork(string name)
        {
            bool retVal = false;

            if (PersistData)
            {
                var result = MemoryManager.LoadNetwork(name);

                if (result.Loaded)
                {
                    CurrentNetworkID = result.CurrentNetworkId;
                    CurrentNetworkName = result.CurrentNetworkName;
                    CaseOrder = result.CaseOrder;
                    SessionCountInitial = SessionCount = result.SessionCount;
                }

                retVal = result.Loaded; 
            }

            return retVal;
        }

        public void ResetNetwork()
        {
            if (CurrentNetworkID < 0)
            {
                throw new Exception("Cannot reset a non existing network. You must create or load a network first.");
            }

            using (RlmDbEntities db = new RlmDbEntities(DatabaseName))
            {
                RlmUtils.ResetTrainingData(db, CurrentNetworkID);
            }

            // recreates network from scratch
            // TODO don't know if we need this later on when resetting the network. Just creating new one since we are dropping entire DB
            //NewNetwork(CurrentNetworkName, Inputs.ToList(), Outputs.ToList());
        }

        /// <summary>
        /// Sets the state of the session to started
        /// </summary>
        /// <remarks>Cannot be used again prior to SessionEnd()</remarks>
        /// <returns>Returns the Session ID of the current session</returns>
        public virtual long SessionStart()
        {
            //Is there an existing session open?
            if(CurrentSessionID != -1)
            {
                //throw new Exception("An active session is already open.  Please use SessionEnd prior to begining a new session.");
                return CurrentSessionID;
            }
            else
            {
                var dateNow = DateTime.Now;
                Session session = new Session()
                {
                    ID = Util.GenerateHashKey(Guid.NewGuid().ToString()),
                    Rnetwork_ID = CurrentNetworkID,
                    DateTimeStart = dateNow,
                    DateTimeStop = dateNow,
                    Hidden = true,
                    SessionScore = Int32.MinValue,
                };

                CurrentSessionID = session.ID;
                SessionCount = MemoryManager.Sessions.Count;

                if (MemoryManager.Sessions.TryAdd(session.ID, session))
                {
                    //save session to db
                    if (PersistData)
                    {
                        MemoryManager.AddSessionToQueue(CurrentSessionID, session);
                    }

                    // reset input momentums
                    foreach(var item in InputMomentums)
                    {
                        item.Value.Reset();
                    }
                }
                else
                {
                    throw new Exception("The session already exists");
                }

                return session.ID;
            }
        }

        /// <summary>
        /// Halts the current session
        /// </summary>
        /// <param name="finalSessionScore">the score of the current session</param>
        public void SessionEnd(double finalSessionScore)
        {
            try
            {
                //Is there an existing session open?
                if (CurrentSessionID == -1)
                {
                    throw new Exception("There is no session open.");
                }
                                
                // merge staging to BestSolutions cache
                foreach(var stageBsol in MemoryManager.BestSolutionStaging)
                {
                    // update sessions score
                    stageBsol.SessionScore = finalSessionScore;

                    Dictionary<long, BestSolution> bsDict;
                    if (MemoryManager.BestSolutions.TryGetValue(stageBsol.RneuronId, out bsDict))
                    {
                        BestSolution bSol;
                        if (bsDict.TryGetValue(stageBsol.SolutionId, out bSol))
                        {
                            if (stageBsol.SessionScore >= bSol.SessionScore && stageBsol.CycleScore >= bSol.CycleScore && stageBsol.CycleOrder >= bSol.CycleOrder)
                            {
                                bsDict[stageBsol.SolutionId] = stageBsol;
                            }
                        }
                        else
                        {
                            bsDict.Add(stageBsol.SolutionId, stageBsol);
                        }
                    }
                    else
                    {
                        var newDict = new Dictionary<long, BestSolution>();
                        newDict.Add(stageBsol.SolutionId, stageBsol);
                        MemoryManager.BestSolutions.TryAdd(stageBsol.RneuronId, newDict);
                    }
                }

                MemoryManager.BestSolutionStaging.Clear();
                
                // TODO need to change to actual update for the session
                var session = MemoryManager.Sessions[CurrentSessionID];
                session.DateTimeStop = DateTime.Now;
                session.SessionScore = finalSessionScore;
                session.Hidden = false;

                //update session to db
                if (PersistData)
                {
                    MemoryManager.AddSessionUpdateToQueue(session);
                }

                //Throw the endsession event
                if (SessionComplete != null) SessionComplete(CurrentSessionID, this);
                LastSessionID = CurrentSessionID;
                CurrentSessionID = -1;
            }
            catch (Exception e)
            {
                throw;
            }
        }
        
        //End Cycle
        internal RlmCyclecompleteArgs EndCycle(RlmCycleOutput cycleOutput, RlmNetworkType rnnType)
        {
            var retVal = new RlmCyclecompleteArgs(cycleOutput, this, rnnType);
            
            CurrentCase.Order = ++CaseOrder;

            //Fire CycleComplete Event
            if (CycleComplete != null) CycleComplete(retVal);

            return retVal;
        }
        /// <summary>
        /// Saves cycle information to database and updates with the score
        /// </summary>
        /// <param name="cycleId">Unique identifier of the Cycle</param>
        /// <param name="cycleScore">Score the engine attained this cycle</param>
        public void ScoreCycle(long cycleId, double cycleScore)
        {
            CurrentCase.CycleScore = cycleScore;

            // add to best solution staging
            var bsol = new BestSolution()
            {
                RneuronId = CurrentCase.Rneuron_ID,
                SolutionId = CurrentCase.Solution_ID,
                CycleOrder = CurrentCase.Order, //CurrentCase.CycleEndTime,
                CycleScore = CurrentCase.CycleScore,
                SessionScore = double.MinValue
            };
            MemoryManager.BestSolutionStaging.Add(bsol);

            //save case to db
            if (PersistData)
            {
                MemoryManager.AddCaseToQueue(cycleId, CurrentCase);
            }

            CurrentCase = null;
        }


        #endregion External Methods

        #region Supporting Functions
        /// <summary>
        /// Notifies the RLM that the current training/prediction sessions are finished and you will no longer use the RLM Network instance. 
        /// Also, it allows the DataPersistence events to work properly so this must be called at the very end.
        /// </summary>   
        public void TrainingDone()
        {
            if (PersistData)
            {
                MemoryManager.TrainingDone();
            }
        }

        /// <summary>
        /// Resets the internal randomization counter to the maximum that was set (StartRandomness). You must call this when you want to retrain the network
        /// after it has recently been trained to maintain the StartRandomness-EndRandomness range. Without calling this method, the randomization will stay 
        /// at the minimum value (EndRandomness) for all training sessions onwards.
        /// </summary>
        public void ResetRandomizationCounter()
        {
            SessionCountInitial = SessionCount;
        }

        /// <summary>
        /// Changes the interval time that the DataPersistenceProgress event is triggered. Default time is 1000ms (1 second)
        /// </summary>
        /// <param name="milliseconds">The amount of time for the interval in milliseconds (ms)</param>
        public void SetDataPersistenceProgressInterval(int milliseconds)
        {
            MemoryManager.SetProgressInterval(milliseconds);
        }

        //Process new inputs/outputs during create new network
        private object ProcessNewIO(Rnetwork net, RlmIO io, Boolean ProcessAsOutput = false)
        {
            object retVal = null;
            Input_Output_Type iot;

            //Create Type
            iot = new Input_Output_Type() { ID = Util.GenerateHashKey(io.DotNetType), Name = io.DotNetType, DotNetTypeName = io.DotNetType };
            if (consoleDisplay)
                Console.WriteLine("Creating new Input_Output_Type: " + io.DotNetType);

            if (!ProcessAsOutput)
            {
                //ToDo: Check for unique names

                //Create Input
                Input newio = new Input() { ID = Util.GenerateHashKey(io.Name), Name = io.Name, Rnetwork_ID = net.ID, Input_Output_Type_ID = iot.ID, Input_Output_Type = iot, Min = io.Min, Max = io.Max, Type = io.Type };
                io.ID = newio.ID;
                retVal = newio;
                if (consoleDisplay)
                    Console.WriteLine("Create new Input: " + newio.Name);
            }
            else
            {
                //ToDo: Check for unique names

                //Create Output
                Output newio = new Output() { ID = Util.GenerateHashKey(io.Name), Name = io.Name, Rnetwork_ID = net.ID, Input_Output_Type_ID = iot.ID, Input_Output_Type = iot, Min = io.Min, Max = io.Max };
                io.ID = newio.ID;
                retVal = newio;
                if (consoleDisplay)
                    Console.WriteLine("Create new Output: " + newio.Name);
            }

            return retVal;
        }

        private void Initialize()
        {
            MemoryManager = new Manager(this);
            MemoryManager.DataPersistenceComplete += MemoryManager_DataPersistenceComplete;
            MemoryManager.DataPersistenceProgress += MemoryManager_DataPersistenceProgress;

            // default 
            MemoryManager.UseMomentumAvgValue = USE_MOM_AVG;
            MemoryManager.MomentumAdjustment = MOMENTUM_ADJUSTMENT;
            MemoryManager.CacheBoxMargin = CACHE_MARGIN;
        }
        
        private void MemoryManager_DataPersistenceComplete()
        {
            DataPersistenceComplete?.Invoke();
        }
        private void MemoryManager_DataPersistenceProgress(long processing, long total)
        {
            DataPersistenceProgress?.Invoke(processing, total);
        }
        #endregion Supporting Functions
    }
}
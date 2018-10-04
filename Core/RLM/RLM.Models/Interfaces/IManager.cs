using RLM.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models.Interfaces
{
    public interface IManager : IDisposable
    {
        // temp for benchmark only
        uint CacheBoxCount { get; set; }
        List<TimeSpan> GetRneuronTimes { get; set; }
        List<TimeSpan> RebuildCacheboxTimes { get; set; }
        // temp for benchmar only

        event DataPersistenceCompleteDelegate DataPersistenceComplete;
        event DataPersistenceProgressDelegate DataPersistenceProgress;

        IRlmNetwork Network { get; }
        ConcurrentDictionary<long, Rneuron> Rneurons { get; set; }
        //key: Input.ID
        SortedList<RlmInputKey, RlmInputValue> DynamicInputs { get; set; }
        //ConcurrentDictionary<long, SortedList<double, HashSet<long>>> DynamicLinearInputs { get; set; }
        //ConcurrentDictionary<long, Dictionary<string, HashSet<long>>> DynamicDistinctInputs { get; set; }
        //key: Ouput.ID
        ConcurrentDictionary<long, HashSet<SolutionOutputSet>> DynamicOutputs { get; set; }
        ConcurrentDictionary<long, Session> Sessions { get; set; }
        ConcurrentDictionary<long, Solution> Solutions { get; set; }
        //ConcurrentBag<Case> Cases { get; set; }
        ConcurrentDictionary<long, Dictionary<long, BestSolution>> BestSolutions { get; set; }
        HashSet<BestSolution> BestSolutionStaging { get; set; }
        double MomentumAdjustment { get; set; }
        double CacheBoxMargin { get; set; }
        bool UseMomentumAvgValue { get; set; }
        void NewNetwork(Rnetwork rnetwork, Input_Output_Type io_type, List<Input> inputs, List<Output> outputs);
        void NewNetwork(Rnetwork rnetwork, List<Input_Output_Type> io_types, List<Input> inputs, List<Output> outputs, IRlmNetwork rnn_net);
        LoadRnetworkResult LoadNetwork(string networkName);
        bool AddSessionToCreateToQueue(long key, Session session);
        bool AddSessionToUpdateToQueue(Session session);
        void AddCaseToQueue(long key, Case c_case);
        Solution GetBestSolution(IEnumerable<RlmIOWithValue> inputs, double linearTolerance = 0, bool predict = false, double predictLinearTolerance = 0, IEnumerable<long> excludeSolutions = null);
        void SetBestSolution(BestSolution bestSolution);
        GetRneuronResult GetRneuronFromInputs(IEnumerable<RlmIOWithValue> inputs, long rnetworkID);
        void SetRneuronWithInputs(Rneuron rneuron);
        GetSolutionResult GetRandomSolutionFromOutput(double randomnessCurrVal, IEnumerable<RlmIO> outputs, long? bestSolutionId = null, IEnumerable<RlmIdea> ideas = null);
        GetSolutionResult GetSolutionFromOutputs(IEnumerable<RlmIOWithValue> outputs);
        void SetSolutionWithOutputs(Solution solution);
        void StartRlmDbWorkers();
        void StopRlmDbWorkersSessions();
        void StopRlmDbWorkersCases();
        void TrainingDone();
        void SetProgressInterval(int milliseconds);
        //todo: Manage  Collections, garbage collect?
        bool GPUMode { get; }
        void SetArrays(int length);
        IEnumerable<long> GetSolutionIdsForOutputs(IDictionary<long, IEnumerable<string>> outputs);
        void RemoveSolutionCascade(long solutionId);
        void FindBestSolution(long rneuronId);
        //List<long> GetRNeuronsFromInputsTime { get; set; }
        //List<long> GetBestSolutionTime { get; set; }
        //List<long> RangeInfoTime { get; set; }
        //List<long> RneuronExecuteTime { get; set; }
        //List<long> FindBestTime { get; set; }
        //List<long> GetRandomSolTime { get; set; }
    }
}

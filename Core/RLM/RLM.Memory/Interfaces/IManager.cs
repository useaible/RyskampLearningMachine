using RLM.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RNN.Memory.Interfaces
{
    public interface IManager
    {
        string DabaseName { get; set; }
        ConcurrentDictionary<long, Rneuron> Rneurons { get; set; }
        //key: Input.ID
        SortedList<RlmInputKey, RlmInputValue> DynamicLinearInputs { get; set; }
        //ConcurrentDictionary<long, SortedList<double, HashSet<long>>> DynamicLinearInputs { get; set; }
        ConcurrentDictionary<long, Dictionary<string, HashSet<long>>> DynamicDistinctInputs { get; set; }
        //key: Ouput.ID
        ConcurrentDictionary<long, HashSet<SolutionOutputSet>> DynamicOutputs { get; set; }
        ConcurrentDictionary<long, Session> Sessions { get; set; }
        ConcurrentDictionary<long, Solution> Solutions { get; set; }
        ConcurrentBag<Case> Cases { get; set; }
        ConcurrentDictionary<long, Dictionary<long, BestSolution>> BestSolutions { get; set; }
        HashSet<BestSolution> BestSolutionStaging { get; set; }        
        void NewNetwork(Rnetwork rnetwork, Input_Output_Type io_type, List<Input> inputs, List<Output> outputs);
        void NewNetwork(Rnetwork rnetwork, List<Input_Output_Type> io_types, List<Input> inputs, List<Output> outputs);
        bool AddSessionToQueue(long key, Session session);
        bool AddSessionUpdateToQueue(Session session);
        void AddCaseToQueue(long key, Case c_case);
        Solution GetBestSolution(IEnumerable<RlmIOWithValue> inputs, double linearTolerance = 0, bool predict = false);
        GetRneuronResult GetRneuronFromInputs(IEnumerable<RlmIOWithValue> inputs, long rnetworkID);
        GetSolutionResult GetRandomSolutionFromOutput(double randomnessCurrVal, IEnumerable<RlmIO> outputs, long? bestSolutionId = null);
        GetSolutionResult GetSolutionFromOutputs(IEnumerable<RlmIOWithValue> outputs);

        //todo: Manage  Collections, garbage collect?
    }
}

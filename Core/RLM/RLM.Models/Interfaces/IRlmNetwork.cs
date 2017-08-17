using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models.Interfaces
{
    public interface IRlmNetwork
    {
        event DataPersistenceCompleteDelegate DataPersistenceComplete;
        event DataPersistenceProgressDelegate DataPersistenceProgress;
        bool PersistData { get; }
        long CurrentNetworkID { get; }
        string CurrentNetworkName { get; }
        string DatabaseName { get; }
        IEnumerable<RlmIO> Inputs { get; set; }
        IEnumerable<RlmIO> Outputs { get; set; }
        IManager MemoryManager { get; }
        IDictionary<long, RlmInputMomentum> InputMomentums { get; }
        int SessionCount { get; }
        long CaseOrder { get; }


        void NewNetwork(string name, IEnumerable<RlmIO> inputs, IEnumerable<RlmIO> outputs);
        bool LoadNetwork();
        bool LoadNetwork(string name);
        void ResetNetwork();
        long SessionStart();
        void SessionEnd(double finalSessionScore);
        void ScoreCycle(long cycleId, double cycleScore);
        void SetDataPersistenceProgressInterval(int milliseconds);
    }
}

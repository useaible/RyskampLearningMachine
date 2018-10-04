// Copyright 2017 Ryskamp Innovations LLC
// License Available through the RLM License Agreement
// https://github.com/useaible/RyskampLearningMachine/blob/dev-branch/License.md

using RLM.Enums;
using RLM.Models;
using RLM.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Models
{
    public class RlmCycleOutput
    {
        public RlmCycleOutput()
        {
            Outputs = new List<RlmIOWithValue>();
        }
        /// <summary>
        /// object type that stores cycle output with cycle information
        /// </summary>
        /// <param name="cycleID">unique identifier for the cycle</param>
        /// <param name="solutionID">unique identifier for the solution</param>
        /// <param name="outputs"></param>
        /// <param name="outputsWithVal"></param>
        /// <param name="cycleGUID"></param>
        public RlmCycleOutput(long cycleID, long rneuronID, long solutionID, IEnumerable<RlmIO> outputs, IEnumerable<Output_Values_Solution> outputsWithVal)
        {
            CycleID = cycleID;
            SolutionID = solutionID;
            RneuronID = rneuronID;

            var outputList = new List<RlmIOWithValue>();
            foreach(var o in outputsWithVal)
            {
                //if (o.Output == null)
                //{
                //    throw new Exception("Output nav property cannot be null");
                //}

                //if (o.Output.Input_Output_Type == null)
                //{
                //    throw new Exception("Input_Output_Type nav property cannot be null");
                //}

                //outputList.Add(new RlmIOWithValue(new RlmIO(null, o.Output.Name, o.Output.Input_Output_Type.Name, o.Output.Min, o.Output.Max, o.Output.ID), o.Value));
                outputList.Add(new RlmIOWithValue(outputs.First(a => a.ID == o.Output_ID), o.Value));
            }
            Outputs = outputList;
        }

        public long CycleID { get; set; }
        public long SolutionID { get; set; }
        public long RneuronID { get; set; }
        public IEnumerable<RlmIOWithValue> Outputs { get; set; }
        public bool CompletelyRandom { get; set; } = false;
    }

    public class RlmCyclecompleteArgs
    {
        /// <summary>
        /// object type that stores cycle outputs with the rlm network
        /// </summary>
        /// <param name="cycleOutput"></param>
        /// <param name="network">current RLM Network</param>
        /// <param name="rnnType">current RLM Network Type</param>
        public RlmCyclecompleteArgs(RlmCycleOutput cycleOutput, IRlmNetwork network, RlmNetworkType rnnType)
        {
            CycleOutput = cycleOutput;
            RlmNetwork = network;
            RlmType = rnnType;
        }
        
        public RlmNetworkType RlmType { get; set; }
        public IRlmNetwork RlmNetwork { get; set; }
        //public Case CurrentCase { get; set; }
        public RlmCycleOutput CycleOutput { get; set; }
    }
}

// Copyright 2017 Ryskamp Innovations LLC
// License Available through the RLM License Agreement
// https://github.com/useaible/RyskampLearningMachine/blob/dev-branch/License.md

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RLM.Enums;
using RLM.Models;

namespace RLM
{
    public class RlmCycle
    {
        public RlmCycle()
        {
        }

        // this allows us to reference the case from db instead of getting it again later on inside the cerebral cortex
        public Case CaseReference { get; private set; }
        public RlmNetworkType RlmType { get; private set; }

        private Task cycletask;
        /// <summary>
        /// starts training
        /// </summary>
        /// <param name="rnnNet">current network being used</param>
        /// <param name="sessionID">unique identifier for the session being started</param>
        /// <param name="inputsValues">Inputs with stored values</param>
        /// <param name="learn">Indicator that if true, will start training, if false, will run prediction</param>
        /// <param name="outputValues">Outputs with stored values</param>
        /// <param name="cyclescore">Score of the current cycle</param>
        /// <param name="Parallel"></param>
        /// <param name="ideas">Gives bias to the RLM on what to output</param>
        /// <returns></returns>
        public RlmCyclecompleteArgs RunCycle(RlmNetwork rnnNet, Int64 sessionID, IEnumerable<RlmIOWithValue> inputsValues, Boolean learn, IEnumerable<RlmIOWithValue> outputValues=null, double cyclescore=0.000, Boolean Parallel = false, IEnumerable<RlmIdea> ideas = null, IEnumerable<long> excludeSolutions = null)
        {
            //Run Validity Checks -- includes local var assignment
            Int64 cyclecaseid = Init(rnnNet, sessionID, inputsValues, learn, outputValues);

            //if parallel, it is executed on another thread and returns null since the cycle complete arguments can only be taken via the Rnetwork cycle complete event
            if (Parallel)
            {
                cycletask = Task.Run(() => RunCycle(rnnNet, this, inputsValues, RlmType, outputValues, cyclescore, ideas, excludeSolutions));
                return null;
            }
            else //if not parallel, we run it on same thread and return the cycle complete arguments **did it this way for a better windowless training
            {
                return RunCycle(rnnNet, this, inputsValues, RlmType, outputValues, cyclescore, ideas, excludeSolutions);
            }

            #region Old code commented
            //cycletask = Task.Run(() =>
            //{
            //    rnn_net._AddCycleToCurrentCycles(this);
            //    rlm_cycle_output caseOutput = rnn_cerebralcortex.CoreCycleProcess(rnn_net, this, inputs_values, RlmType, output_values, cyclescore);
            //    rnn_net._EndCycle(caseOutput, RlmType);
            //});

            //Non-Parallel Waits for Thread to Complete
            //if (!Parallel) cycletask.Wait();             
            #endregion
        }

        private RlmCyclecompleteArgs RunCycle(RlmNetwork rnnNet, RlmCycle cycle, IEnumerable<RlmIOWithValue> inputValues, RlmNetworkType rnnType, IEnumerable<RlmIOWithValue> outputValues, double cycleScore, IEnumerable<RlmIdea> ideas = null, IEnumerable<long> excludeSolutions = null)
        {
            //rnn_net._AddCycleToCurrentCycles(this);
            RlmCycleOutput caseOutput = RlmCerebralCortex.CoreCycleProcess(rnnNet, this, inputValues, RlmType, outputValues, cycleScore, ideas, excludeSolutions);
            return rnnNet.EndCycle(caseOutput, RlmType);
        }
        
        private Int64 Init(RlmNetwork rnn_net, Int64 sessionID, IEnumerable<RlmIOWithValue> inputs_values, Boolean learn, IEnumerable<RlmIOWithValue> output_values = null)
        {
            if(this.CaseReference != null)
            {
                throw new Exception("You may only envoke Runxx once per cycle object.");
            }

            // set type of learning based on Inputs and Outputs
            RlmType = RlmNetworkType.Predict;
            if (learn)
            {
                if ((inputs_values != null && inputs_values.Count() > 0) &&
                    (output_values != null && output_values.Count() > 0)) // Inputs & Outputs
                {
                    RlmType = RlmNetworkType.Supervised;
                }
                else if ((inputs_values != null && inputs_values.Count() > 0) &&
                    (output_values == null || (output_values != null && output_values.Count() == 0))) // Inputs without Outputs
                {
                    RlmType = RlmNetworkType.Unsupervised;
                }
            }
            
            Case cyclecase = new Case() {Session_ID = sessionID /*db.Sessions.Where(item=>item.ID == sessionID).First()*/, CycleStartTime=DateTime.Now, CycleEndTime = DateTime.Now};
            this.CaseReference = cyclecase;
            return cyclecase.ID;
        }

    }
}

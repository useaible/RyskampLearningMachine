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
using System.Linq.Expressions;
using System.Data.Entity;
using RLM.Memory;

namespace RLM
{
    internal class RlmCerebralCortex
    {
        private const double IDEAL_SCORE = 100; 

        internal static RlmCycleOutput CoreCycleProcess(RlmNetwork rnn_net, RlmCycle rnn_cyc, List<Models.RlmIOWithValue> rnn_ins, RlmNetworkType rnnType, List<Models.RlmIOWithValue> rnn_outs, double cyclescore, IEnumerable<RlmIdea> ideas = null)
        {
            var memoryMgr = rnn_net.MemoryManager;

            // temp benchmark only
            //rnn_net.CurrentCycleCount++;
            // temp benhcmark only

            // Determine if any inputs are of Linear type
            bool hasLinearInputs = rnn_ins.Any(a => a.Type == RlmInputType.Linear);

            // update input momentums
            if (hasLinearInputs)
            {
                foreach (var item in rnn_ins)
                {
                    if (item.Type == RlmInputType.Linear)
                    {
                        var inputMomentumObj = rnn_net.InputMomentums[item.ID];
                        inputMomentumObj.SetInputValue(Convert.ToDouble(item.Value));
                        item.InputMomentum = inputMomentumObj;
                    }
                }
            }
            
            //Get rneuron
            GetRneuronResult rneuronFound = memoryMgr.GetRneuronFromInputs(rnn_ins, rnn_net.CurrentNetworkID);
            Rneuron neuron = rneuronFound.Rneuron;

            //Holds the solution instance
            GetSolutionResult solutionFound = new GetSolutionResult();
            Solution solution = null;
            
            // sets ideas, if any are passed in as parameters
            IEnumerable<Models.RlmIO> outputs = rnn_net.Outputs;
            if (ideas != null)
            {
                foreach (var output in outputs)
                {
                    foreach (var idea in ideas)
                    {
                        if (idea is RlmOutputLimiter)
                        {
                            var outputIdea = idea as RlmOutputLimiter;
                            if (outputIdea.RlmIOId == output.ID)
                            {
                                output.Idea = outputIdea;
                            }
                        }
                    }
                }
            }

            bool completelyRandom = false;
            double randomnessValue = rnn_net.RandomnessCurrentValue;

            if (rnnType == RlmNetworkType.Supervised)
            {
                //Supervised, get solution and record ideal score
                solutionFound = memoryMgr.GetSolutionFromOutputs(rnn_outs);
                solution = solutionFound.Solution;
                cyclescore = IDEAL_SCORE;
            }
            else if (rnnType == RlmNetworkType.Unsupervised && randomnessValue > 0)
            {
                //TODO:  This should be based upon the randomization factor
                int randomProbability = Util.Randomizer.Next(1, 101);
                bool random = randomProbability <= randomnessValue;
                
                //Idea 
                //ToDo: Implement Ideas
                //The idea implementation will not be added until core functionality works.  It is an "extra" and the network can learn without it.  In fact, since it reduces load, we need
                //to test without it in place first.  Otherwise networks that don't have an applicable "idea" may crash

                //System.Diagnostics.Debug.WriteLine("Threshold: " + randomnThreshold);

                long? bestSolutionId = null;
                if (!random)
                {
                    // get best solution
                    solution = memoryMgr.GetBestSolution(rnn_ins, (hasLinearInputs) ? rnn_net.LinearToleranceCurrentValue : 0); //db.GetBestSolution(rnn_net.CurrentNetworkID, rnn_ins, (hasLinearInputs) ? rnn_net.LinearToleranceCurrentValue : 0);
                    bestSolutionId = solution?.ID;
                    if (solution == null)
                    {
                        completelyRandom = true;
                        solutionFound = memoryMgr.GetRandomSolutionFromOutput(randomnessValue, outputs, bestSolutionId);
                    }
                    else
                    {
                        solutionFound.Solution = solution;
                        solutionFound.ExistsInCache = true;
                    }
                }
                else if (random && outputs.Count() > 1)
                {
                    solution = memoryMgr.GetBestSolution(rnn_ins, (hasLinearInputs) ? rnn_net.LinearToleranceCurrentValue : 0); //db.GetBestSolution(rnn_net.CurrentNetworkID, rnn_ins, (hasLinearInputs) ? rnn_net.LinearToleranceCurrentValue : 0);
                    bestSolutionId = solution?.ID;
                    completelyRandom = true;
                    solutionFound = memoryMgr.GetRandomSolutionFromOutput(randomnessValue, outputs, bestSolutionId);
                }
                else
                {
                    completelyRandom = true;
                    solutionFound = memoryMgr.GetRandomSolutionFromOutput(randomnessValue, outputs);
                }

                solution = solutionFound.Solution;
            }
            else // Predict
            {
                solution = memoryMgr.GetBestSolution(rnn_ins, predict: true); //db.GetBestSolution(rnn_net.CurrentNetworkID, new List<long>() { neuron.ID }, true);
                
                if (solution == null)
                {
                    completelyRandom = true;
                    solutionFound = memoryMgr.GetRandomSolutionFromOutput(randomnessValue, outputs);
                    solution = solutionFound.Solution;
                    #region TODO cousin node search
                    //// no solution found AND all inputs are Distinct
                    //if (!hasLinearInputs)
                    //{
                    //    completelyRandom = true;
                    //    //solution = GetRandomSolutionFromOutput(db, rnn_net.CurrentNetworkID, outputs, false);
                    //    solutionFound = memoryMgr.GetRandomSolutionFromOutput(randomnessValue, outputs); //GetRandomSolutionFromOutput(db, rnn_net, outputs, rnn_ins, (hasLinearInputs) ? rnn_net.LinearToleranceCurrentValue : 0);
                    //}
                    //else // has linear
                    //{
                    //    // TODO need to change the methods used below to MemoryManager
                    //    //// gets all the known inputs
                    //    //var knownInputs = DetermineKnownInputs(db, rnn_ins, rnn_net.CousinNodeSearchToleranceIncrement);
                    //    //if (knownInputs.Count > 0)
                    //    //{
                    //    //    // holds the top cases for each known input
                    //    //    var topCases = new List<Case>();
                    //    //    foreach (var item in knownInputs)
                    //    //    {
                    //    //        // gets the top solution for the current input with incremental checks based on the linear bracket
                    //    //        var topCase = GetBestKnownCase(db, item, rnn_net.CousinNodeSearchToleranceIncrement);
                    //    //        if (topCase != null)
                    //    //        {
                    //    //            topCases.Add(topCase);
                    //    //        }
                    //    //    }

                    //    //    // determine which Case has the highest score and get it's corresponding solution
                    //    //    solution = topCases.OrderByDescending(a => a.Session.DateTimeStop)
                    //    //        .ThenByDescending(a => a.CycleEndTime)
                    //    //        .ThenByDescending(a => a.CycleScore)
                    //    //        .ThenByDescending(a => a.Session.SessionScore)
                    //    //        .Take(1)
                    //    //        .Select(a => a.Solution)
                    //    //        .FirstOrDefault();
                    //    //}
                    //    //else // if no known inputs then we get solution randomly
                    //    //{
                    //    //    completelyRandom = true;
                    //    //    //solution = GetRandomSolutionFromOutput(db, rnn_net.CurrentNetworkID, outputs, false);
                    //    //    solution = GetRandomSolutionFromOutput(db, rnn_net, outputs, rnn_ins, (hasLinearInputs) ? rnn_net.LinearToleranceCurrentValue : 0);
                    //    //}
                    //}
                    #endregion

                    //solutionFound.Solution = solution;
                    //solutionFound.ExistsInCache = false;
                }
                else
                {
                    solutionFound.Solution = solution;
                    solutionFound.ExistsInCache = true;
                }
            }
                
            //Document score, solution in Case
            var newCase = RecordCase(rnn_cyc
                , rneuronFound
                , rnn_ins
                , rnn_outs
                , cyclescore
                , solutionFound
                , 0 //ToDo: Pass the current maturity factor setting
                , completelyRandom //ToDo: pass whether or not the result was completely randomly generated
                , 0 //ToDo: pass sequential count
                );
            
            // set Current case reference
            rnn_net.CurrentCase = newCase;
                
            var cycleOutput = new RlmCycleOutput(newCase.ID, newCase.Solution_ID, rnn_net.Outputs, solution.Output_Values_Solutions, rnn_cyc.CycleCaseGUID);
            return cycleOutput;
        }
   
        //Record Case
        private static Case RecordCase(RlmCycle cycle, GetRneuronResult rneuronFound, List<RlmIOWithValue> rnn_ins, List<RlmIOWithValue> runn_outs,
            double cyclescore, GetSolutionResult solutionFound, Int16 currentmfactor, bool resultcompletelyrandom, short sequentialmfactorsuccessescount)
        {
            Case casefromdb = cycle.CaseReference; //db.Cases.Find(cycle.CycleCaseID);
            //db.Sessions.Attach(casefromdb.Session);

            //db.Entry(casefromdb.Session).State = EntityState.Unchanged;

            //casefromdb.Session = db.Sessions.Find(casefromdb.Session.ID);
            
            //Check for none found
            if (casefromdb == null)
            {
                throw new Exception("An error occurred, the current case with the ID of" + cycle.CycleCaseGUID.ToString() + "could not be located in the database.");
            }

            //Check for none found
            if (rneuronFound.Rneuron == null)
            {
                throw new Exception("An error occurred, the current Rnueron could not be located in the database.");
            }

            //Check for none found
            if (solutionFound.Solution == null)
            {
                throw new Exception("An error occurred, the current Solution could not be located in the database.");
            }

            //Assign Values
            //casefromdb.Rneuron = rneuron;
            casefromdb.Rneuron_ID = rneuronFound.Rneuron.ID;
            casefromdb.Rneuron = (rneuronFound.ExistsInCache) ? null : rneuronFound.Rneuron;            
            //casefromdb.Idea_Implementations --> Later
            casefromdb.Solution_ID = solutionFound.Solution.ID;
            casefromdb.Solution = (solutionFound.ExistsInCache) ? null : solutionFound.Solution;
            casefromdb.CycleEndTime = DateTime.Now;
            casefromdb.CycleScore = cyclescore;
            casefromdb.CurrentRFactor = 0;// rneuron.RandomizationFactor;
            casefromdb.CurrentMFactor = currentmfactor;
            casefromdb.ResultCompletelyRandom = resultcompletelyrandom;
            casefromdb.SequentialMFactorSuccessesCount = sequentialmfactorsuccessescount;
          

            return casefromdb;
        }

        #region TODO implement to Memory Manager for RLM Prediction
        //private static Case GetBestKnownCase(RlmDbEntities db, RlmIOWithValue input, int cousinNodeSearchToleranceValue)
        //{
        //    Case retVal = null;

        //    if (input.Type == RlmInputType.Distinct)
        //    {
        //        retVal = db.Cases
        //            .Where(a => a.Rneuron.Input_Values_Reneurons.Any(b =>
        //                b.Value == input.Value &&
        //                b.Input.ID == input.ID))
        //            .Include(a => a.Session)
        //            .Include(a => a.Solution)
        //            .OrderByDescending(a => a.Session.DateTimeStop)
        //            .ThenByDescending(a => a.CycleEndTime)
        //            .ThenByDescending(a => a.CycleScore)
        //            .ThenByDescending(a => a.Session.SessionScore)
        //            .Take(1)
        //            .FirstOrDefault();
        //    }
        //    else // for linear
        //    {
        //        for (int i = 1; i <= cousinNodeSearchToleranceValue; i++)
        //        {
        //            // get offset value
        //            decimal offset = Convert.ToDecimal(input.Max) * (Convert.ToDecimal(i) / 100M);
        //            decimal valueUp = Convert.ToDecimal(input.Value) + offset;
        //            decimal valueDown = Convert.ToDecimal(input.Value) - offset;

        //            retVal = db.Cases
        //                .Where(a => a.Rneuron.Input_Values_Reneurons.Any(b =>
        //                      RlmFunctions.ConvertToDouble(b.Value) >= valueDown &&
        //                      RlmFunctions.ConvertToDouble(b.Value) <= valueUp &&
        //                      b.Input.ID == input.ID))
        //                .Include(a => a.Session)
        //                .Include(a => a.Solution)
        //                .OrderByDescending(a => a.Session.DateTimeStop)
        //                .ThenByDescending(a => a.CycleEndTime)
        //                .ThenByDescending(a => a.CycleScore)
        //                .ThenByDescending(a => a.Session.SessionScore)
        //                .Take(1)
        //                .FirstOrDefault();

        //            if (retVal != null) break;
        //        }
        //    }

        //    return retVal;
        //}

        //private static List<RlmIOWithValue> DetermineKnownInputs(RlmDbEntities db, List<RlmIOWithValue> inputs, int cousinNodeSearchToleranceValue)
        //{
        //    var retVal = new List<RlmIOWithValue>();

        //    foreach (var item in inputs)
        //    {
        //        bool hasInputs = false;

        //        if (item.Type == RlmInputType.Distinct)
        //        {
        //            hasInputs = db.Input_Values_Reneurons.Any(a =>
        //                a.Value == item.Value &&
        //                a.Input.ID == item.ID);
        //        }
        //        else
        //        {
        //            decimal offset = Convert.ToDecimal(item.Max) * (Convert.ToDecimal(cousinNodeSearchToleranceValue) / 100M);
        //            decimal valueUp = Convert.ToDecimal(item.Value) + offset;
        //            decimal valueDown = Convert.ToDecimal(item.Value) - offset;

        //            hasInputs = db.Input_Values_Reneurons.Any(a =>
        //                RlmFunctions.ConvertToDouble(a.Value) >= valueDown &&
        //                RlmFunctions.ConvertToDouble(a.Value) <= valueUp &&
        //                a.Input.ID == item.ID);
        //        }

        //        if (hasInputs)
        //        {
        //            retVal.Add(item);
        //        }
        //    }

        //    return retVal;
        //}
        #endregion
    }
}

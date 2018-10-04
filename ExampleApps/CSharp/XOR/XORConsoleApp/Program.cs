using RLM;
using RLM.SQLServer;
using RLM.Enums;
using RLM.Models;
using RLM.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools;
using RLM.PostgreSQLServer;

namespace XORConsoleApp
{
    class Program
    {
        // we will use this table to supply inputs and check for the RLM output
        static IEnumerable<dynamic> xorTable = new List<dynamic>
        {
            new { Input1 = "True", Input2 = "True", Answer = "False" },
            new { Input1 = "True", Input2 = "False", Answer = "True" },
            new { Input1 = "False", Input2 = "True", Answer = "True" },
            new { Input1 = "False", Input2 = "False", Answer = "False" },
        };
        private static bool showDataPersistProgress = false;

        static void Main(string[] args)
        {
            Console.WriteLine("XOR");            
            Console.WriteLine("\nRLM settings");

            // user inputs for the RLM settings
            int sessions = Util.GetInput("Number of sessions [default 50]: ", 50);
            int startRandomness = Util.GetInput("Start randomness [default 50]: ", 50);
            int endRandomness = Util.GetInput("End randomness [default 0]: ", 0);

            // use Sql Server as Rlm Db
            IRlmDbData rlmDBData = new RlmDbDataSQLServer($"RLM_XOR_SAMPLE_{Guid.NewGuid().ToString("N")}");
            //IRlmDbData rlmDBData = new RlmDbDataPostgreSqlServer($"RLM_XOR_SAMPLE_{Guid.NewGuid().ToString("N")}");

            // the appended Guid is just to have a unique RLM network every time we run this example.
            // you can remove this or simply change the name to something static to use the same network all the time
            var rlmNet = new RlmNetwork(rlmDBData);

            // subscribe to events to know the status of the Data Persistence that works in the background
            rlmNet.DataPersistenceComplete += RlmNet_DataPersistenceComplete;
            rlmNet.DataPersistenceProgress += RlmNet_DataPersistenceProgress;

            // checks to see if the network already exists and loads it to memory
            if (!rlmNet.LoadNetwork("XOR_SAMPLE"))
            {
                // declare our inputs
                var ins = new List<RlmIO>();
                ins.Add(new RlmIO("XORInput1", typeof(bool).ToString(), 0, 1, RlmInputType.Distinct));
                ins.Add(new RlmIO("XORInput2", typeof(bool).ToString(), 0, 1, RlmInputType.Distinct));

                // declare our outputs
                var outs = new List<RlmIO>();
                outs.Add(new RlmIO("XOROutput", typeof(bool).ToString(), 0, 1));

                // creates a new network
                rlmNet.NewNetwork("XOR_SAMPLE", ins, outs);
            }

            // execute it on another thread as not to block the RLM training
            Console.WriteLine("\nPress 'd' to show Data persistence progress\n");
            Task.Run(() =>
            {
                while (!Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.D)
                {
                    showDataPersistProgress = true;
                }
            });

            // set rlm training settings
            rlmNet.NumSessions = sessions;
            rlmNet.StartRandomness = startRandomness;
            rlmNet.EndRandomness = endRandomness;
            
            Console.WriteLine("Training Session started");
            double sumOfCycleScores = 0;
            
            // this is just good practice, usually you need to call this when you want to do multiple trainings on the same network over and over again.
            // it resets the randomization of the RLM back to the beginning (start randomness) after each training set 
            // i.e, training for 50 sessions, then training for another 50 sessions, and so on and so forth
            rlmNet.ResetRandomizationCounter();

            for (int i = 0; i < sessions; i++)
            {
                // start session
                long sessionId = rlmNet.SessionStart();
                sumOfCycleScores = 0;

                foreach (var xor in xorTable)
                {
                    //Populate input values
                    var invs = new List<RlmIOWithValue>();

                    // get value for Input1 and associate it with the Input instance
                    string input1Value = xor.Input1;
                    invs.Add(new RlmIOWithValue(rlmNet.Inputs.Where(item => item.Name == "XORInput1").First(), input1Value));

                    // get value for Input2 and associate it with the Input instance
                    string input2Value = xor.Input2;
                    invs.Add(new RlmIOWithValue(rlmNet.Inputs.Where(item => item.Name == "XORInput2").First(), input2Value));

                    //Build and run a new RlmCycle
                    var Cycle = new RlmCycle();
                    RlmCyclecompleteArgs result = Cycle.RunCycle(rlmNet, sessionId, invs, true);

                    // scores the RLM on how well it did for this cycle
                    // each cycle with a correct output is rewarded a 100 score, 0 otherwise
                    double score = ScoreCycle(result, xor);

                    sumOfCycleScores += score;

                    // sets the score
                    rlmNet.ScoreCycle(result.CycleOutput.CycleID, score);
                }

                Console.WriteLine($"Session #{i} - score: {sumOfCycleScores}");

                // end the session with the sum of the cycle scores
                // with the way the scoring is set, a perfect score would be 400 meaning all cycles got it right
                rlmNet.SessionEnd(sumOfCycleScores);
            }            


            Console.WriteLine("Training Session ended");
            
            Console.WriteLine();
            Console.WriteLine("Predict:");

            // PREDICT... see how well the RLM learned
            // NOTE that the only difference with Predict from Training is that we passed 'false' to the learn argument on the Cycle.RunCycle() method
            // and of course, the inputs which we used our xor table to check if the RLM has learned as expected
            long SessionID = rlmNet.SessionStart();
            sumOfCycleScores = 0;

            foreach (var xor in xorTable)
            {
                var invs = new List<RlmIOWithValue>();
                invs.Add(new RlmIOWithValue(rlmNet.Inputs.First(a => a.Name == "XORInput1"), xor.Input1));
                invs.Add(new RlmIOWithValue(rlmNet.Inputs.First(a => a.Name == "XORInput2"), xor.Input2));

                RlmCycle Cycle = new RlmCycle();
                RlmCyclecompleteArgs result = Cycle.RunCycle(rlmNet, SessionID, invs, false);

                double score = ScoreCycle(result, xor, true);

                sumOfCycleScores += score;

                // sets the score
                rlmNet.ScoreCycle(result.CycleOutput.CycleID, score);
            }

            rlmNet.SessionEnd(sumOfCycleScores);

            // must call this to let the Data persistence know we are done training/predicting
            rlmNet.TrainingDone();

            Console.ReadLine();
        }

        private static void RlmNet_DataPersistenceProgress(long processed, long total)
        {
            if (showDataPersistProgress)
            {
                Console.WriteLine($"Data persistence progress - {processed} / {total}");
            }
        }

        private static void RlmNet_DataPersistenceComplete()
        {
            Console.WriteLine("Data persistence done");
        }

        static double ScoreCycle(RlmCyclecompleteArgs cycleResult, dynamic xor, bool showOutput = false)
        {
            double score = 0;
            string output = null;

            // get output
            output = cycleResult.CycleOutput.Outputs.First().Value;

            // check if RLM output is correct based on our xor reference
            if (xor.Answer == output)
            {
                score = 100;
            }

            if (showOutput)
            {
                Console.WriteLine($"{ xor.Input1 } and { xor.Input2 } is { output }");
            }

            return score;
        }        
    }
}

using LogisticsGameLib;
using RLM;
using RLM.SQLServer;
using RLM.Enums;
using RLM.Models;
using RLM.Models.Exceptions;
using RLM.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools;
using RLM.PostgreSQLServer;

namespace LogisticsConsoleApp
{
    public class RlmLogistics
    {
        private bool showDataPersistProgress = false;

        public bool DataPersistenceDone { get; private set; } = false;

        public void LogisticTrain()
        {
            Console.WriteLine("\nRLM network settings:");
            int sessions = Util.GetInput("\nEnter Number of Session [default 100]: ", 100); //Gets user input for the number of tries the game will play
            int startRand = Util.GetInput("Enter Start Randomness [default 100]: ", 100); //Gets user input for start randomness
            int endRand = Util.GetInput("Enter End Randomness [default 0]: ", 0); //Gets user input for end randomness

            var dbName = $"RLM_logistic_" + Guid.NewGuid().ToString("N");
            var networkName = "Logicstics Network";
            LogisticSimulator simulator = null;

            IEnumerable<int> customerOrders = LogisticInitialValues.CustomerOrders;
            
            try
            {
                //IRlmDbData rlmDbData = new RlmDbDataPostgreSqlServer(dbName);
                IRlmDbData rlmDbData = new RlmDbDataSQLServer(dbName);
                RlmNetwork network = new RlmNetwork(rlmDbData); //Make an instance of rlm_network passing the database name as parameter
                network.DataPersistenceComplete += Network_DataPersistenceComplete;
                network.DataPersistenceProgress += Network_DataPersistenceProgress;

                if (!network.LoadNetwork(networkName))
                {
                    var inputs = new List<RlmIO>()
                    {
                        new RlmIO("X", typeof(Int32).ToString(), 1, 1, RlmInputType.Distinct),
                    };

                    double minFrom = LogisticInitialValues.PlayerMinRange[0];
                    double minTo = LogisticInitialValues.PlayerMinRange[1];
                    double maxFrom = LogisticInitialValues.PlayerMaxRange[0];
                    double maxTo = LogisticInitialValues.PlayerMaxRange[1];
                    var outputs = new List<RlmIO>()
                    {
                       new RlmIO("Retailer_Min", typeof(Int16).ToString(), minFrom, minTo),
                       new RlmIO("Retailer_Max", typeof(Int16).ToString(), maxFrom, maxTo),
                       new RlmIO("WholeSaler_Min", typeof(Int16).ToString(), minFrom, minTo),
                       new RlmIO("WholeSaler_Max", typeof(Int16).ToString(), maxFrom, maxTo),
                       new RlmIO("Distributor_Min", typeof(Int16).ToString(), minFrom, minTo),
                       new RlmIO("Distributor_Max", typeof(Int16).ToString(), maxFrom, maxTo),
                       new RlmIO("Factory_Min", typeof(Int16).ToString(), minFrom, minTo),
                       new RlmIO("Factory_Max", typeof(Int16).ToString(), maxFrom, maxTo),
                       new RlmIO("Factory_Units_Per_Day", typeof(Int16).ToString(), LogisticInitialValues.FactoryRange[0], LogisticInitialValues.FactoryRange[1]),
                    };

                    network.NewNetwork(networkName, inputs, outputs);
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

                network.NumSessions = sessions; // num of sessioins default 100
                network.StartRandomness = startRand;
                network.EndRandomness = endRand;
                
                simulator = new LogisticSimulator(LogisticInitialValues.StorageCost, LogisticInitialValues.BacklogCost, LogisticInitialValues.InitialInventory, LogisticInitialValues.InitialInventory, LogisticInitialValues.InitialInventory, LogisticInitialValues.InitialInventory);

                Stopwatch watch = new Stopwatch();
                watch.Start();
                Console.WriteLine("\n\nTraining:\n");
                IEnumerable<LogisticSimulatorOutput> predictedLogisticOutputs = null;
                
                network.ResetRandomizationCounter();

                for (int i = 0; i < sessions; i++)
                {
                    var sessId = network.SessionStart();

                    var inputs = new List<RlmIOWithValue>();
                    inputs.Add(new RlmIOWithValue(network.Inputs.First(), "1"));

                    var cycle = new RlmCycle();
                    var outputs = cycle.RunCycle(network, sessId, inputs, true);

                    var simOutputs = outputs.CycleOutput.Outputs
                            .Select(a => new LogisticSimulatorOutput() { Name = a.Name, Value = Convert.ToInt32(a.Value) })
                            .ToList();

                    simulator.ResetSimulationOutput();
                    simulator.Start(simOutputs, 50, customerOrders);

                    network.ScoreCycle(outputs.CycleOutput.CycleID, 0);
                    var totalCosts = simulator.SumAllCosts();
                    network.SessionEnd(totalCosts);

                    Console.WriteLine($"Session #{i + 1} \t Score: {Math.Abs(totalCosts).ToString("$#,##0"),10}");

                    if (i == sessions - 1)
                        predictedLogisticOutputs = simOutputs;
                }


                watch.Stop();

                Console.WriteLine("\nPredicted outputs:");
                string resultText = "";
                foreach (var item in predictedLogisticOutputs)
                {
                    resultText += "\n" + item.Name + ": " + item.Value;
                }

                Console.WriteLine(resultText);
                Console.WriteLine($"\nElapsed: {watch.Elapsed}");
                network.TrainingDone();                
            }
            catch (Exception e)
            {
                if (e.InnerException != null && e.InnerException is RlmDefaultConnectionStringException)
                {
                    Console.WriteLine($"Error: {e.InnerException.Message}");
                }
                else
                {
                    Console.WriteLine($"ERROR: {e.Message}");
                }
            }
            Console.ReadLine();
        }
        
        private void Network_DataPersistenceComplete()
        {
            DataPersistenceDone = true;
            Console.WriteLine("RLM data persistence done.");
        }
        private void Network_DataPersistenceProgress(long processed, long total)
        {
            if (showDataPersistProgress)
            {
                Console.WriteLine($"Data Persistence progress: {processed} / {total}");
            }
        }
    }
}

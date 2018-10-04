using CsvHelper;
using LanderGameLib;
using RLM.Models.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools;

namespace LunarLanderConsoleApp
{
    public class RLMLander
    {
        private RLMPilot pilot;
        private int sessions;
        private int startRand;
        private int endRand;
        private int maxLinearBracket;
        private int minLinearBracket;
        private bool inputsHasValue;
        
        public RLMLander()
        {
            inputsHasValue = false;
        }
        
        public RLMLander(int sess, int sr = 30, int er = 0, int maxlr = 15, int minlr = 3)
        {
            sessions = sess;
            startRand = sr;
            endRand = er;
            maxLinearBracket = maxlr;
            minLinearBracket = minlr;
            inputsHasValue = true;
        }

        public void LanderTrain()
        {
            const int MOMENTUM_ADJUSTMENT = 21;
            const int CACHE_MARGIN = 0;
            const bool USE_MOM_AVG = false;

            // settings
            Console.WriteLine("\nRLM network settings:");
            if (!inputsHasValue)
            {
                sessions = Util.GetInput("Enter Number of Sessions [default 100]: ", 100);
                startRand = Util.GetInput("Enter start randomness [default 30]: ", 30);
                endRand = Util.GetInput("Enter end randomness [default 0]: ", 0);
                maxLinearBracket = Util.GetInput("Max linear bracket value [default 15]: ", 15);
                minLinearBracket = Util.GetInput("Min linear bracket value [default 3]: ", 3);
            }
            Console.WriteLine();

            try
            {
                pilot = new RLMPilot(true, sessions, startRand, endRand, maxLinearBracket, minLinearBracket);

                // execute it on another thread as not to block the RLM training
                Console.WriteLine("\nPress 'd' to show Data persistence progress\n");
                Task.Run(() =>
                {
                    while (!Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.D)
                    {
                        pilot.ShowDataPersistenceProgress = true;
                    }
                });

                Stopwatch watch = new Stopwatch();
                watch.Start();
                Console.WriteLine();

                pilot.ResetRLMRandomization();

                for (int i = 0; i < sessions; i++)
                {
                    pilot.StartSimulation(i, true);
                }

                watch.Stop();

                Console.WriteLine($"\nTotal Elapsed Time: {watch.ElapsedMilliseconds}\n");

                int showPredictedOutput = 0;//Util.GetInput("Show landing simulation for AI prediction? [No - 0, Yes - 1 default]: ", 1);
                if (showPredictedOutput == 1)
                {
                    pilot.Learn = false;
                    pilot.StartSimulation(sessions, true);
                }
                
                pilot.TrainingDone(watch.Elapsed, sessions);
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
        }
    }

    public class SessionElapseAvg
    {
        public int Session { get; set; }
        public double Average { get; set; }
        public int Inputs { get; set; }
    }
}

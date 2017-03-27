using LanderGameLib;
using RLM.Models.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools;

namespace LunarLanderConsoleApp
{
    public class RLMLander
    {
        public static void LanderTrain()
        {
            const int MOMENTUM_ADJUSTMENT = 21;
            const int CACHE_MARGIN = 0;
            const bool USE_MOM_AVG = false;

            // setting
            Console.WriteLine("\n\nRLM network settings:");
            int sessions = Util.GetInput("Enter Number of Sessions [default 100]: ", 100);
            int startRand = Util.GetInput("Enter start randomness [default 30]: ", 30);
            int endRand = Util.GetInput("Enter end randomness [default 0]: ", 0);
            int maxLinearBracket = Util.GetInput("Max linear bracket value [default 15]: ", 15);
            int minLinearBracket = Util.GetInput("Min linear bracket value [default 3]: ", 3);
            Console.WriteLine();

            try
            {
                var pilot = new RLMPilot(true, sessions, startRand, endRand, maxLinearBracket, minLinearBracket);

                Stopwatch watch = new Stopwatch();
                watch.Start();
                Console.WriteLine();

                for (int i = 0; i < sessions; i++)
                {
                    pilot.StartSimulation(i, true);
                }

                pilot.TrainingDone();

                watch.Stop();

                Console.WriteLine($"\nElapsed: {watch.Elapsed}\n");

                int showPredictedOutput = Util.GetInput("Show landing simulation for AI prediction? [No - 0, Yes - 1 default]: ", 1);
                if (showPredictedOutput == 1)
                {
                    pilot.Learn = false;
                    pilot.StartSimulation(sessions, true);

                    Console.WriteLine("hit enter to continue...");
                    Console.ReadLine();
                }
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
}

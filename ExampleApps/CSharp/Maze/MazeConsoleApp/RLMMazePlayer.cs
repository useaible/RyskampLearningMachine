using MazeGameLib;
using RLM.Models.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools;

namespace MazeConsoleApp
{
    public class RlmMazePlayer
    {
        /// <summary>
        /// This is where RLM is being configured and trained. Call this method and pass the needed parameters to start the training.
        /// </summary>
        /// <param name="maze">Contains all the details of the maze that encog will be going to solve.</param>
        /// <param name="gameTimeout">The given time for solving the maze per session.</param>
        public static void MazeTrain(MazeInfo maze, int gameTimeout)
        {
            Console.WriteLine("RLM network settings:");

            int sessions = Util.GetInput("Number of sessions [default 50]: ", 50); //Gets user input for the number of sessions
            int startRandomness = Util.GetInput("Start randomness [default 100]: ", 100); //Gets user input for the start randomness
            int endRandomness = Util.GetInput("End randomness [default 0]: ", 0); //Gets user input for the end randomness
            int randomnessThrough = Util.GetInput("Number of sessions to enforce randomness [default 1]: ", 1);

            Console.WriteLine();

            try
            {
                RLMMazeTraveler traveler = new RLMMazeTraveler(maze, true, randomnessThrough, startRandomness, endRandomness); //Instantiate RlmMazeTraveler game lib to configure the network.
                traveler.SessionComplete += SesionComplete;

                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                //Start the training (Play the game)
                for (int i = 0; i < randomnessThrough; i++)
                {
                    traveler.Travel(gameTimeout);
                }

                // set to predict instead of learn for the remaining sessions
                traveler.Learn = false;

                for (int i = 0; i < sessions - randomnessThrough; i++)
                {
                    traveler.Travel(gameTimeout);
                }

                traveler.TrainingDone();
                watch.Stop();

                Console.WriteLine($"Elapsed: {watch.Elapsed}");
                Console.ReadLine();
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

        private static void SesionComplete(int cycleNum, double score, int movesCnt)
        {
            Console.WriteLine($"Session: {cycleNum} \t Score: {score} \t Moves: {movesCnt}");
        }
    }
}

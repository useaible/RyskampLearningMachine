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
        const int MIN_MAZE_SIZE = 8;
        const int MAZE_GAME_TIMEOUT = 5; // seconds
        private static Dictionary<int, MazeInfo> mazes = new Dictionary<int, MazeInfo>();

        /// <summary>
        /// This is where RLM is being configured and trained.
        /// </summary>
        public void MazeTrain()
        {
            int mazeSize = Util.GetInput($"Enter Maze size [must be {MIN_MAZE_SIZE} or greater]: ", MIN_MAZE_SIZE);
            var maze = GenerateOrGetExistingMaze(mazeSize);

            int gameTimeout = Util.GetInput($"Game timeouts after [{MAZE_GAME_TIMEOUT} seconds default]: ", MAZE_GAME_TIMEOUT);
            Console.WriteLine($"Perfect move count for this maze would be {maze.PerfectGameMovesCount}");
            Console.WriteLine();

            Console.WriteLine("RLM network settings:");

            int sessions = Util.GetInput("Number of sessions [default 50]: ", 50); //Gets user input for the number of sessions
            int startRandomness = Util.GetInput("Start randomness [default 100]: ", 100); //Gets user input for the start randomness
            int endRandomness = Util.GetInput("End randomness [default 0]: ", 0); //Gets user input for the end randomness
            int randomnessThrough = Util.GetInput("Number of sessions to enforce randomness [default 1]: ", 1);
            
            try
            {
                RLMMazeTraveler traveler = new RLMMazeTraveler(maze, true, randomnessThrough, startRandomness, endRandomness); //Instantiate RlmMazeTraveler game lib to configure the network.
                traveler.SessionComplete += SesionComplete;
                
                // execute it on another thread as not to block the RLM training
                Console.WriteLine("\nPress 'd' to show Data persistence progress\n");
                Task.Run(() =>
                {
                    while (!Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.D)
                    {
                        traveler.ShowDataPersistenceProgress = true;
                    }
                });

                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                traveler.ResetRLMRandomization();

                //Start the training (Play the game)
                for (int i = 0; i < randomnessThrough; i++)
                {
                    traveler.Travel(gameTimeout * 1000);
                }

                // set to predict instead of learn for the remaining sessions
                traveler.Learn = false;

                for (int i = 0; i < sessions - randomnessThrough; i++)
                {
                    traveler.Travel(gameTimeout * 1000);
                }

                watch.Stop();

                Console.WriteLine($"Elapsed: {watch.Elapsed}");
                traveler.TrainingDone();
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

        private void SesionComplete(int cycleNum, double score, int movesCnt)
        {
            Console.WriteLine($"Session: {cycleNum} \t Score: {score} \t Moves: {movesCnt}");
        }

        private static MazeInfo GenerateOrGetExistingMaze(int size)
        {
            MazeInfo retVal;
            if (!mazes.TryGetValue(size, out retVal))
            {
                var generator = new MazeGenerator();
                generator.Generate(size, size);
                generator.Solve();
                retVal = new MazeInfo()
                {
                    GoalPosition = generator.GoalLocation,
                    Grid = generator.TheMazeGrid,
                    Name = $"{size}x{size}",
                    PerfectGameMovesCount = generator.PerfectGameMovesCount,
                    StartingPosition = generator.StartingPosition,
                    Height = size,
                    Width = size
                };

                mazes.Add(size, retVal);
            }

            return retVal;
        }
    }
}

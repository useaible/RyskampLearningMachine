// Copyright 2017 Ryskamp Innovations LLC
// License Available through the RLM License Agreement
// https://github.com/useaible/RyskampLearningMachine/blob/dev-branch/License.md

using Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MazeGameLib;

namespace MazeConsoleApp
{
    class Program
    {
        const int MIN_MAZE_SIZE = 8;
        static readonly int MAZE_GAME_TIMEOUT = 5; // seconds
        static Dictionary<int, MazeInfo> mazes = new Dictionary<int, MazeInfo>();

        static void Main(string[] args)
        {
            bool isExit = false;
            do
            {
                Console.Clear();
                Console.WriteLine("Maze");
                int mazeSize = Util.GetInput($"Enter Maze size [must be {MIN_MAZE_SIZE} or greater]: ", 8);

                if (mazeSize >= MIN_MAZE_SIZE)
                {
                    var maze = GenerateOrGetExistingMaze(mazeSize);

                    int gameTimeout = Util.GetInput($"Game timeouts after [{MAZE_GAME_TIMEOUT} seconds default]: ", MAZE_GAME_TIMEOUT);
                    Console.WriteLine($"Perfect move count for this maze would be {maze.PerfectGameMovesCount}");
                    Console.WriteLine();

                    Console.WriteLine("Select an AI");
                    Console.WriteLine("1) Ryskamp Learning Machine \n2) Encog \n3) Exit ");
                    var choice = Console.ReadKey();

                    Console.WriteLine();
                    switch (choice.KeyChar)
                    {
                        case '1':
                            RlmMazePlayer.MazeTrain(maze, gameTimeout * 1000);
                            break;
                        case '2':
                            EncogMazePlayer.MazeTrain(maze, gameTimeout * 1000);
                            break;
                        case '3':
                            isExit = true;
                            break;
                        default:
                            Console.WriteLine("\nInvalid input try again...");
                            System.Threading.Thread.Sleep(1000);
                            break;
                    }
                }
            } while (!isExit);
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

using MazeGameLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools;

namespace MazeConsoleApp
{
    public class EncogMazePlayer
    {
        /// <summary>
        /// This is where encog is being configured and trained. Call this method and pass the needed parameters to start the training.
        /// </summary>
        /// <param name="maze">Contains all the details of the maze that encog will be going to solve.</param>
        /// <param name="gameTimeout">The given time for solving the maze per session.</param>
        public static void MazeTrain(MazeInfo maze, int gameTimeout)
        {
            Console.WriteLine("Encog network structure:");

            int defHiddenLayers = maze.Width * maze.Height; //This is the default number of hidden layer neurons if not specified.
            int hiddenLayers = Util.GetInput("Number of hidden layers [default 1]: ", 1); //Getting user input for the number of hidden layers, will be set to 1(default) if not specified.
            int hiddenLayersNeurons = Util.GetInput($"Hidden layers rneurons [default {defHiddenLayers}]:", defHiddenLayers); //Getting user input for the number of hidden layer neurons, will be set to "defHiddenLayers"(default) value if not specified.

            var em = new EncogMaze(maze, hiddenLayers, hiddenLayersNeurons); //Create an instance of encog maze game to configure the network.

            em.EncogCycleComplete += SesionComplete;
            em.TrainingIterationComplete += Em_DoneTrainingIteration;

            int mode = Util.GetInput("Select Encog learning method [Annealing - 0, Genetic - 1 default]: ", 1); //Gets user input for the type of encog training method, e.g. (Simulated Annealing, MLGeneticAlgorithm)
            int epochs = Util.GetInput("EPOCHS to execute [default 10]: ", 10); //Gets user input for the number of epochs
            int cycles = Util.GetInput((mode == 0) ? "Cycles per epoch [defualt 10]: " : "Population size [default 10]: ", 10); //Gets user input for the number of cycles
            int maxTemp = 0;
            int minTemp = 0;
            if (mode == 0)
            {
                maxTemp = Util.GetInput("Max temperature [default 10]: ", 10); //Gets user input for the starting temperature
                minTemp = Util.GetInput("Min temperature [default 2]: ", 2); //Gets user input for the ending temperature
            }
            Console.WriteLine();

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            em.Train(mode, epochs, maxTemp, minTemp, cycles, gameTimeout); //Call the Train() method from encog game lib to start the training.

            watch.Stop();

            Console.WriteLine($"Elapsed: {watch.Elapsed}");
            Console.ReadLine();
        }

        private static void SesionComplete(int cycleNum, double score, int movesCnt)
        {
            Console.WriteLine($"Session: {cycleNum} \t Score: {score} \t Moves: {movesCnt}");
        }

        private static void Em_MazeCycleComplete(int x, int y, bool bumpedIntoWall)
        {
            Console.WriteLine($"X: {x} Y: {y} Bump: {bumpedIntoWall}");
        }

        private static void Em_DoneTrainingIteration(int epoch, double score)
        {
            var output = $"EPOCH: {epoch} \t Score: {score}\n";
            Console.WriteLine(output);
            System.Diagnostics.Debug.WriteLine(output);
        }
    }
}

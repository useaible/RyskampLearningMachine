using Encog.Engine.Network.Activation;
using Encog.ML;
using Encog.ML.Genetic;
using Encog.ML.Train;
using Encog.Neural.Networks;
using Encog.Neural.Networks.Training.Anneal;
using LanderGameLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools;

namespace LunarLanderConsoleApp
{
    public class EncogLander
    {
        public static void LanderTrain()
        {
            Console.WriteLine("\n\nEncog network structure: \nNumber of Input neurons 3 \nNumber of output neurons 1 ");
            int hiddenLayers = Util.GetInput("Number of hidden layers [default 1]:", 1);
            int hiddenLayerNeurons = Util.GetInput("Number of hidden layer neurons [default 100]: ", 100);
            int numOfEpoch = Util.GetInput("Number of Epochs [default 10]:", 10);
            int type = Util.GetInput("\nSelect a training method [Annealing - 0][Genetic - 1 default]:", 1);

            BasicNetwork network = CreateNetwork(hiddenLayers, hiddenLayerNeurons);
            var pilotScore = new PilotScore();


            IMLTrain train;
            if (type == 0)
            {
                int startTemp = Util.GetInput("Start Temperature [default 10]:", 10);
                int endTemp = Util.GetInput("Stop Temperature [default 2]:", 2);
                int cycles = Util.GetInput("Number of Cycles [default 10]:", 10);
                train = new NeuralSimulatedAnnealing(
                    network, pilotScore, startTemp, endTemp, cycles);
            }
            else
            {
                int PopulationSize = Util.GetInput("Population Size [default 10]:", 10);
                train = new MLMethodGeneticAlgorithm(() => {
                    BasicNetwork result = CreateNetwork(hiddenLayers, hiddenLayerNeurons);
                    ((IMLResettable)result).Reset();
                    return result;
                }, pilotScore, PopulationSize); // population size
            }

            Console.WriteLine("\n\nTraining: \n");

            System.Diagnostics.Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int i = 1; i <= numOfEpoch; i++) // num of epochs
            {
                train.Iteration();
                Console.WriteLine($"Epoch#: {i} \t Score: {train.Error}");
            }
            stopwatch.Stop();


            Console.WriteLine("\nThe total number of times it tried the Lunar Lander for training: " + NeuralPilot.CycleCount);
            Console.WriteLine($"Elapsed: {stopwatch.Elapsed}\n");

            int showPredictedOutput = Util.GetInput("Show landing simulation for AI prediction? [No - 0, Yes - 1 default]: ", 1);
            if (showPredictedOutput == 1)
            {
                network = (BasicNetwork)train.Method;
                var pilot = new NeuralPilot(network, true);
                pilot.ScorePilot();

                Console.WriteLine("hit enter to continue...");
                Console.ReadLine();
            }
        }

        private static BasicNetwork CreateNetwork(int hiddenLayers, int hiddenLayerNeurons)
        {
            var pattern = new Encog.Neural.Pattern.FeedForwardPattern { InputNeurons = 3 };
            for (int i = 0; i < hiddenLayers; i++)
            {
                pattern.AddHiddenLayer(hiddenLayerNeurons);
            }
            pattern.OutputNeurons = 1;
            pattern.ActivationFunction = new ActivationTANH();
            var network = (BasicNetwork)pattern.Generate();
            network.Reset();
            return network;
        }
    }
}

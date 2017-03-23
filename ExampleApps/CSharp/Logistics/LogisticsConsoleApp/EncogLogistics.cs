using Encog.Engine.Network.Activation;
using Encog.ML;
using Encog.ML.Data;
using Encog.ML.Data.Basic;
using Encog.ML.Genetic;
using Encog.ML.Train;
using Encog.Neural.Networks;
using Encog.Neural.Networks.Training;
using Encog.Neural.Networks.Training.Anneal;
using Encog.Util.Arrayutil;
using LogisticsGameLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tools;

namespace LogisticsConsoleApp
{
    public class EncogLogistics
    {
        /// <summary>
        /// This is where encog network settings are configured and training is being processed.
        /// </summary>
        public static void LogisticTrain()
        {
            Console.WriteLine("\n\nEncog network structure: \nNumber of Input neurons 1 \nNumber of output neurons 9 ");

            int hiddenLayers = Util.GetInput("Number of hidden layers [default 1]: ", 1);
            int hiddenLayerNeurons = Util.GetInput("Hidden layer neurons [default 100]: ", 100);
            int type = Util.GetInput("\nSelect a training method [Annealing - 0][Genetic - 1 default]:", 1);
            int numOfEpoch = Util.GetInput("Number of Epochs [default 10]:", 10);

            BasicNetwork network = CreateNetwork(hiddenLayers, hiddenLayerNeurons);
            var pilotScore = new EncogLogisticScore();
            IMLTrain train;

            if (type == 0)
            {
                int startTemp = Util.GetInput("Start Temperature [default 10]:", 10);
                int endTemp = Util.GetInput("End Temperature [default 2]:", 2);
                int cycles = Util.GetInput("Cycles [default 10]:", 10);
                train = new NeuralSimulatedAnnealing(network, pilotScore, endTemp, startTemp, cycles);
            }
            else
            {
                int populationSize = Util.GetInput("Population Size [default 10]:", 10);
                train = new MLMethodGeneticAlgorithm(() => {
                    BasicNetwork result = CreateNetwork(hiddenLayers, hiddenLayerNeurons);
                    ((IMLResettable)result).Reset();
                    return result;
                }, pilotScore, populationSize); // population size
            }

            Stopwatch watch = new Stopwatch();
            watch.Start();
            Console.WriteLine("\n\nTraining: \n");

            for (int i = 0; i < numOfEpoch; i++) // num of epochs
            {
                train.Iteration();

                double totalCosts = train.Error;
                string currencyScore = totalCosts.ToString("$#,##0");
                Console.WriteLine($"Epoch # {i} \t Score: {currencyScore,10}");
            }

            watch.Stop();

            Console.WriteLine("\nPredicted outputs:");
            network = (BasicNetwork)train.Method;
            var pilot = new EncogLogisticSimulator(network, true);

            pilot.CalculateScore(LogisticSimulator.GenerateCustomerOrders(), true);

            Console.WriteLine($"\nElapsed: {watch.Elapsed}");
            Console.WriteLine("\nThe total number of times it tried the Logistic Simulation for training: " + pilotScore.SessionCnt);
            Console.ReadLine();
        }

        private static BasicNetwork CreateNetwork(int hiddenLayers, int hiddenLayerNeurons)
        {
            var pattern = new Encog.Neural.Pattern.FeedForwardPattern { InputNeurons = 1 };
            for (int i = 0; i < hiddenLayers; i++)
            {
                pattern.AddHiddenLayer(hiddenLayerNeurons);
            }
            pattern.OutputNeurons = 9;
            pattern.ActivationFunction = new ActivationTANH();
            var network = (BasicNetwork)pattern.Generate();
            network.Reset();
            return network;
        }
    }

    /// <summary>
    /// A class that handles the processing of encog logistics scoring
    /// </summary>
    public class EncogLogisticScore : ICalculateScore
    {
        private int sessionCnt = 0;
        static IEnumerable<int> CustomerOrders = null;
        public EncogLogisticScore()
        {
            CustomerOrders = LogisticInitialValues.CustomerOrders;
        }

        public int SessionCnt
        {
            get { return sessionCnt; }
        }

        public double CalculateScore(IMLMethod network)
        {
            int cnt = Interlocked.Increment(ref sessionCnt);

            EncogLogisticSimulator sim = new EncogLogisticSimulator((BasicNetwork)network, false);
            var score = sim.CalculateScore(CustomerOrders, false);

            Console.WriteLine($"Session #{cnt} \t Score: {Math.Abs(score).ToString("$#,##0")}");

            return score;
        }

        public bool ShouldMinimize
        {
            get { return false; }
        }

        /// <inheritdoc/>
        public bool RequireSingleThreaded
        {
            get { return false; }
        }
    }

    public class EncogLogisticSimulator
    {
        private LogisticSimulator sim = new LogisticSimulator(LogisticInitialValues.StorageCost, LogisticInitialValues.BacklogCost, LogisticInitialValues.InitialInventory, LogisticInitialValues.InitialInventory, LogisticInitialValues.InitialInventory, LogisticInitialValues.InitialInventory);

        private readonly BasicNetwork _network;
        private NormalizedField min = new NormalizedField(NormalizationAction.Normalize, "min", LogisticInitialValues.PlayerMinRange[1], LogisticInitialValues.PlayerMinRange[0], -1, 1);
        private NormalizedField max = new NormalizedField(NormalizationAction.Normalize, "max", LogisticInitialValues.PlayerMaxRange[1], LogisticInitialValues.PlayerMaxRange[0], -1, 1);
        private NormalizedField units = new NormalizedField(NormalizationAction.Normalize, "units", LogisticInitialValues.FactoryRange[1], LogisticInitialValues.FactoryRange[0], -1, 1);

        public static List<int> Outputs { get; set; } = new List<int>();

        public EncogLogisticSimulator(BasicNetwork network, bool track)
        {
            _network = network;
        }

        public int CalculateScore(IEnumerable<int> customerOrder, bool showResults)
        {
            var input = new BasicMLData(1);
            input[0] = 1;

            IMLData output = _network.Compute(input);
            var logOutput = new List<LogisticSimulatorOutput>();
            logOutput.Add(new LogisticSimulatorOutput() { Name = "Retailer_Min", Value = Convert.ToInt32(min.DeNormalize(output[0])) });
            logOutput.Add(new LogisticSimulatorOutput() { Name = "Retailer_Max", Value = Convert.ToInt32(max.DeNormalize(output[1])) });
            logOutput.Add(new LogisticSimulatorOutput() { Name = "WholeSaler_Min", Value = Convert.ToInt32(min.DeNormalize(output[2])) });
            logOutput.Add(new LogisticSimulatorOutput() { Name = "WholeSaler_Max", Value = Convert.ToInt32(max.DeNormalize(output[3])) });
            logOutput.Add(new LogisticSimulatorOutput() { Name = "Distributor_Min", Value = Convert.ToInt32(min.DeNormalize(output[4])) });
            logOutput.Add(new LogisticSimulatorOutput() { Name = "Distributor_Max", Value = Convert.ToInt32(max.DeNormalize(output[5])) });
            logOutput.Add(new LogisticSimulatorOutput() { Name = "Factory_Min", Value = Convert.ToInt32(min.DeNormalize(output[6])) });
            logOutput.Add(new LogisticSimulatorOutput() { Name = "Factory_Max", Value = Convert.ToInt32(max.DeNormalize(output[7])) });
            logOutput.Add(new LogisticSimulatorOutput() { Name = "Factory_Units_Per_Day", Value = Convert.ToInt32(units.DeNormalize(output[8])) });

            sim.Start(logOutput, customOrders: customerOrder);

            if (showResults)
            {
                ShowResults(logOutput);
            }

            int score = Convert.ToInt32(sim.SumAllCosts());

            return score;
        }

        private static void ShowResults(List<LogisticSimulatorOutput> logOutput)
        {
            string resultText = "";
            foreach (var item in logOutput)
            {
                resultText += "\n" + item.Name + ": " + item.Value;
            }

            Console.WriteLine(resultText);
        }

        public int GetScore(LogisticSimulator sim)
        {
            var score = sim.SumAllCosts() * -1;
            return Convert.ToInt32(20000 - score);
        }
    }
}

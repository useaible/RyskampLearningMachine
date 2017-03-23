using Encog.Engine.Network.Activation;
using Encog.ML;
using Encog.ML.Data;
using Encog.ML.Data.Basic;
using Encog.ML.Genetic;
using Encog.ML.Train;
using Encog.Neural.Networks;
using Encog.Neural.Networks.Training;
using Encog.Neural.Networks.Training.Anneal;
using Encog.Neural.Pattern;
using Encog.Util.Arrayutil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace MazeGameLib
{
    public delegate void SessionCompleteDelegate(int cycleNum, double score, int movesCnt);
    public delegate void TrainingIterationCompleteDelegate(int epoch, double score);

    public class EncogMazeTraveler : Traveler
    {
        private NormalizedField xInput;
        private NormalizedField yInput;
        private NormalizedField bumpedIntoWallInput;
        private NormalizedField directionOutput;
        private BasicNetwork network;
        private MazeInfo maze;

        public event MazeCycleCompleteDelegate MazeCycleComplete;

        public EncogMazeTraveler(BasicNetwork network, MazeInfo maze)
        {
            this.network = network;
            this.maze = maze;

            // set normalized fields
            xInput = new NormalizedField(NormalizationAction.Normalize, "X", maze.Width - 1, 0, -0.9, 0.9);
            yInput = new NormalizedField(NormalizationAction.Normalize, "Y", maze.Height - 1, 0, -0.9, 0.9);
            bumpedIntoWallInput = new NormalizedField(NormalizationAction.Normalize, "BumpedIntoWall", 1, 0, -0.9, 0.9);
            directionOutput = new NormalizedField(NormalizationAction.Normalize, "Direction", 3, 0, -0.9, 0.9);

            tmr.Elapsed += Tmr_Elapsed;
        }

        private int timeout = 0;
        private System.Timers.Timer tmr = new System.Timers.Timer();
        private void Tmr_Elapsed(object sender, ElapsedEventArgs e)
        {
            Interlocked.CompareExchange(ref timeout, 1, 0);
        }


        //public double Travel(ref int timeout, out int movesCnt)
        public double Travel(int timerTimeout, out int movesCnt)
        {
            MazeGame game = new MazeGame();
            game.InitGame(maze);
            game.traveler = this;
            game.traveler.location.X = maze.StartingPosition.X;
            game.traveler.location.Y = maze.StartingPosition.Y;

            var recentOutcome = new MazeCycleOutcome();
            tmr.Interval = timerTimeout;
            tmr.Start();
            timeout = 0;
            movesCnt = 0;

            while (!recentOutcome.GameOver && timeout == 0)
            {
                movesCnt++;
                var input = new BasicMLData(2);
                input[0] = xInput.Normalize(Convert.ToDouble(game.traveler.location.X));
                input[1] = yInput.Normalize(Convert.ToDouble(game.traveler.location.Y));

                IMLData output = network.Compute(input);

                double maxVal = double.MinValue;
                int direction = 0;
                for (int i = 0; i < output.Count; i++)
                {
                    if (output[i] > maxVal)
                    {
                        direction = i;
                        maxVal = output[i];
                    }
                }
                recentOutcome = game.CycleMaze(direction);
                MazeCycleComplete?.Invoke(game.traveler.location.X, game.traveler.location.Y, recentOutcome.BumpedIntoWall);
            }

            tmr.Stop();

            var score = game.CalculateFinalScore(movesCnt);
            return score;
        }

        public Int16 Play(double[] inputs)
        {
            Int16 retVal = -1;

            var input = new BasicMLData(3);
            input[0] = xInput.Normalize(inputs[0]);
            input[1] = yInput.Normalize(inputs[1]);
            input[2] = bumpedIntoWallInput.Normalize(inputs[2]);

            IMLData output = network.Compute(input);
            double denormValue = output[0];
            double normValue = Math.Round(directionOutput.DeNormalize(denormValue));
            retVal = Convert.ToInt16(normValue);

            return retVal;
        }
    }

    public class EncogScore : ICalculateScore
    {
        private static int EncogCycleCounter = 0;
        public event SessionCompleteDelegate EncogCycleComplete;
        public event MazeCycleCompleteDelegate MazeCycleComplete;

        private int time;

        private MazeInfo maze;
        public EncogScore(MazeInfo maze, int timerTimeout = 5)
        {
            this.maze = maze;
            this.time = timerTimeout;
        }

        public double CalculateScore(IMLMethod network)
        {
            var traveler = new EncogMazeTraveler((BasicNetwork)network, maze);
            traveler.MazeCycleComplete += Traveler_MazeCycleComplete;
            int movesCnt = 0;
            //var retVal = traveler.Travel(ref timeout, out moveCnt);
            var retVal = traveler.Travel(time, out movesCnt);

            if (EncogCycleComplete != null)
            {
                EncogCycleCounter++;
                EncogCycleComplete(EncogCycleCounter, retVal, movesCnt);
            }

            return retVal;
        }

        private void Traveler_MazeCycleComplete(int x, int y, bool bumpedIntoWall)
        {
            if (MazeCycleComplete != null) MazeCycleComplete(x, y, bumpedIntoWall);
        }

        public bool RequireSingleThreaded
        {
            get
            {
                return true;
            }
        }

        public bool ShouldMinimize
        {
            get
            {
                return false;
            }
        }
    }

    public class EncogMaze
    {
        private const string ENCOG_FILE_EXTENSION = "eg";
        private BasicNetwork network;
        private MazeInfo maze;
        private MazeGame gameRef;
        private string filePath;

        public const int INPUT_RNEURONS = 2;
        public const int OUTPUT_RNEURONS = 4;
        public event SessionCompleteDelegate EncogCycleComplete;
        public event MazeCycleCompleteDelegate MazeCycleComplete;
        public event TrainingIterationCompleteDelegate TrainingIterationComplete;

        public EncogMaze(MazeInfo maze, int hiddenLayers = 1, int? hiddenLayerNeurons = null)
        {
            this.maze = maze;
            filePath = Path.Combine(Environment.CurrentDirectory, maze.Name + "." + ENCOG_FILE_EXTENSION);

            if (!LoadNetwork())
            {
                // create new network
                var pattern = new FeedForwardPattern() { InputNeurons = INPUT_RNEURONS, OutputNeurons = OUTPUT_RNEURONS };
                for (int i = 0; i < hiddenLayers; i++)
                {
                    pattern.AddHiddenLayer(hiddenLayerNeurons == null ? maze.Width * maze.Height : hiddenLayerNeurons.Value);
                }
                pattern.ActivationFunction = new ActivationTANH();
                network = (BasicNetwork)pattern.Generate();
                network.Reset();
            }
        }

        public EncogMaze(MazeInfo maze, MazeGame gameRef)
            : this(maze)
        {
            this.gameRef = gameRef;
            gameRef.GameStartEvent += GameRef_GameStartEvent;
            gameRef.GameCycleEvent += GameRef_GameCycleEvent;
        }

        private void GameRef_GameCycleEvent(MazeCycleOutcome cycle, Traveler traveler)
        {
            // inputs
            double[] inputs = new[]
            {
                Convert.ToDouble(traveler.location.X),
                Convert.ToDouble(traveler.location.Y),
                Convert.ToDouble(cycle.BumpedIntoWall)
            };

            Int16 direction = this.Traveler.Play(inputs);

            //tmp delay since encog going too fast
            System.Threading.Thread.Sleep(10);

            gameRef.DirectionsStack.Enqueue(direction);
        }

        private void GameRef_GameStartEvent(Traveler traveler, int currentIteration = 1)
        {
            // inputs
            double[] inputs = new[]
            {
                Convert.ToDouble(traveler.location.X),
                Convert.ToDouble(traveler.location.Y),
                Convert.ToDouble(false)
            };

            Int16 direction = this.Traveler.Play(inputs);

            gameRef.DirectionsStack.Enqueue(direction);
        }

        public void Train(int mode = 0, int epochs = 1, int maxTemp = 10, int minTemp = 2, int cycles = 10, int timerTimeout = 5000)
        {
            var encogScore = new EncogScore(maze, timerTimeout);
            encogScore.EncogCycleComplete += EncogScore_EncogCycleComplete;
            encogScore.MazeCycleComplete += EncogScore_MazeCycleComplete;

            IMLTrain train;
            if (mode == 0)
            {
                //Simulated Annealing
                train = new NeuralSimulatedAnnealing(network, encogScore, maxTemp, minTemp, cycles);
            }
            else
            {
                //Genetic Algorithm
                train = new MLMethodGeneticAlgorithm(() => { return network; }, encogScore, cycles);
            }

            for (int epoch = 1; epoch <= epochs; epoch++)
            {
                train.Iteration();
                TrainingIterationComplete?.Invoke(epoch, train.Error);
            }

            //SaveTrainingData();
        }

        public Int16 Play(double[] inputs)
        {
            Int16 retVal = -1;

            if (inputs == null)
            {
                throw new NullReferenceException("Input array cannot be null");
            }

            retVal = Traveler.Play(inputs);

            return retVal;
        }

        private EncogMazeTraveler traveler;
        public EncogMazeTraveler Traveler
        {
            get
            {
                if (traveler == null)
                {
                    traveler = new EncogMazeTraveler((BasicNetwork)NetworkForPlay, maze);
                }

                return traveler;
            }
        }

        private BasicNetwork networkForPlay;
        internal BasicNetwork NetworkForPlay
        {
            get
            {
                if (networkForPlay == null)
                {
                    //Simulated Annealing
                    IMLTrain train = new NeuralSimulatedAnnealing(network, new EncogScore(maze), 10, 2, 100);
                    networkForPlay = (BasicNetwork)train.Method;
                }

                return networkForPlay;
            }
        }

        public void ResetNetworkForPlay()
        {
            if (networkForPlay != null)
            {
                networkForPlay = null;
            }
        }

        private void EncogScore_EncogCycleComplete(int cycleNum, double score, int moves)
        {
            if (EncogCycleComplete != null) EncogCycleComplete(cycleNum, score, moves);
        }

        private void EncogScore_MazeCycleComplete(int x, int y, bool bumpedIntoWall)
        {
            if (MazeCycleComplete != null) MazeCycleComplete(x, y, bumpedIntoWall);
        }


        ///Does a Current Instance of network exist?
        private bool LoadNetwork()
        {
            try
            {
                //net = (BasicNetwork)SerializeObject.Load(FilePath);
                if (File.Exists(filePath))
                {
                    FileInfo fileInfo = new FileInfo(filePath);
                    network = (BasicNetwork)Encog.Persist.EncogDirectoryPersistence.LoadObject(fileInfo);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return false;
            }
            if (network != null)
                return true;
            else
                return false;
        }

        public void SaveTrainingData()
        {
            try
            {
                //SerializeObject.Save(FilePath, net);
                FileInfo fileInfo = new FileInfo(filePath);
                Encog.Persist.EncogDirectoryPersistence.SaveObject(fileInfo, network);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }

        public void ResetNetwork()
        {
            if (network != null)
            {
                network.Reset();
                SaveTrainingData();
            }
        }
    }
}

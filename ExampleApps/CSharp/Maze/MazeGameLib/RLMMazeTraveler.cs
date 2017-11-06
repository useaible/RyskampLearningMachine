using RLM;
using RLM.Enums;
using RLM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace MazeGameLib
{
    public delegate void MazeCycleCompleteDelegate(int x, int y, bool bumpedIntoWall);
    public delegate void MazeCycleErrorDelegate(string networkName);
    public delegate void SessionCompleteDelegate(int sessionCnt, double score, int moves);
    public delegate void SessionStartedDelegate(double randomnessLeft);
    public delegate void SetRandomnessLeftDelegate(double val);

    public class RLMMazeTraveler : Traveler, IDisposable
    {
        private const string NETWORK_NAME = "AIMazeTraveler01";
        private const double CORRECT_SCORE = 100;
        private const double WRONG_SCORE = 0;
        private const int TIMER_TIMEOUT = 5000; // milliseconds
        protected RlmNetwork rlmNet;
        private Int64 currentSessionID;
        private Int64 currentCycleID;
        private SetRandomnessLeftDelegate SetRandomnessLeft;
        
        private MemoryCache rlmNetCache;
        private MazeInfo maze;

        public event MazeCycleCompleteDelegate MazeCycleComplete;
        public event MazeCycleErrorDelegate MazeCycleError;
        public event SessionStartedDelegate SessionStarted;
        public event SessionCompleteDelegate SessionComplete;

        public RlmNetwork CurrentNetwork { get; set; }
        public bool DataPersistenceDone { get; private set; } = false;
        public bool ShowDataPersistenceProgress { get; set; } = false;

        // for windowless version
        public RLMMazeTraveler(MazeInfo maze, bool learn = false, int numSessions = 1, int startRandomness = 1, int endRandomness = 1)
        {
            this.maze = maze;
            Learn = learn;
            rlmNetCache = MemoryCache.Default;

            rlmNet = CreateOrLoadNetwork(maze);

            rlmNet.NumSessions = numSessions;
            rlmNet.StartRandomness = startRandomness;
            rlmNet.EndRandomness = endRandomness;

            tmr.Elapsed += Tmr_Elapsed;

        }
        // for window version
        public RLMMazeTraveler(MazeInfo maze, IMazeGame gameref, bool learn = false, int numSessions = 1, int startRandomness = 1, int endRandomness = 1, SetRandomnessLeftDelegate setRandomnessLeft = null)
            : base(gameref)
        {
            Learn = learn;
            GameRef.GameStartEvent += GameRef_GameStartEvent;
            GameRef.GameCycleEvent += GameRef_GameCycleEvent;
            //GameRef.GameOverEvent += GameRef_GameOverEvent;

            rlmNet = CreateOrLoadNetwork(maze);

            // Temporary, while RFactor not yet implemented
            rlmNet.NumSessions = numSessions;
            rlmNet.StartRandomness = startRandomness;
            rlmNet.EndRandomness = endRandomness;
            
            rlmNet.CycleComplete += Rlm_net_CycleComplete;

            SetRandomnessLeft = setRandomnessLeft;

            tmr.Elapsed += Tmr_Elapsed;
        }

        public RLMMazeTraveler(IMazeGame gameRef, bool learn = false)
            : base (gameRef)
        {
            Learn = learn;
            GameRef.GameStartEvent += GameRef_GameStartEvent;
            GameRef.GameCycleEvent += GameRef_GameCycleEvent;
            //GameRef.GameOverEvent += GameRef_GameOverEvent;
                        
            tmr.Elapsed += Tmr_Elapsed;
        }

        private int timeout = 0;
        private System.Timers.Timer tmr = new System.Timers.Timer();
        private void Tmr_Elapsed(object sender, ElapsedEventArgs e)
        {
            Interlocked.CompareExchange(ref timeout, 1, 0);
        }

        public bool Learn { get; set; }

        public virtual RlmNetwork CreateOrLoadNetwork(MazeInfo maze)
        {
            var rlmNet = new RlmNetwork("RLM_maze_" + maze.Name); //+ "_" + Guid.NewGuid().ToString("N"));
            rlmNet.DataPersistenceComplete += RlmNet_DataPersistenceComplete;
            rlmNet.DataPersistenceProgress += RlmNet_DataPersistenceProgress;

            if (!rlmNetCache.Contains("rlmNet"))
            {
                var expiration = DateTimeOffset.UtcNow.AddDays(1);
                rlmNetCache.Add("rlmNet", rlmNet, expiration);
            }
            else
                rlmNet = (RlmNetwork)rlmNetCache.Get("rlmNet", null);

            if (!rlmNet.LoadNetwork(maze.Name))
            {
                var inputs = new List<RlmIO>()
                {
                    new RlmIO("X", typeof(Int32).ToString(), 0, maze.Width - 1, RlmInputType.Distinct),
                    new RlmIO("Y", typeof(Int32).ToString(), 0, maze.Height - 1,  RlmInputType.Distinct),
                };

                var outputs = new List<RlmIO>()
                {
                    new RlmIO("Direction", typeof(Int16).ToString(), 0, 3)
                };

                rlmNet.NewNetwork(maze.Name, inputs, outputs);
            }

            return rlmNet;
        }

        protected virtual void RlmNet_DataPersistenceProgress(long processed, long total)
        {
            if (ShowDataPersistenceProgress)
            {
                Console.WriteLine($"Data Persistence progress: {processed} / {total}");
            }
        }

        protected virtual void RlmNet_DataPersistenceComplete()
        {
            DataPersistenceDone = true;
            Console.WriteLine("RLM Data Persistence done.");
        }

        public double RandomnessLeft
        {
            get
            {
                return rlmNet.RandomnessCurrentValue;
            }
        }

        public void ResetRLMRandomization()
        {
            rlmNet.ResetRandomizationCounter();
        }

        public virtual IMazeGame GetNewGameInstance()
        {
            var game = new MazeGame();
            game.traveler = this;
            game.InitGame(maze);
            return game;
        }

        /// <summary>
        /// This method will let the AI play the maze for one game (windowless)
        /// </summary>
        public virtual MazeCycleOutcome Travel(int timerTimeout = 5000)
        {
            IMazeGame game = GetNewGameInstance();
            
            MazeCycleOutcome outcome = new MazeCycleOutcome();

            // Start AI Training
            currentSessionID = rlmNet.SessionStart();

            SessionStarted?.Invoke(rlmNet.RandomnessCurrentValue);

            tmr.Interval = timerTimeout;
            tmr.Start();
            timeout = 0;
            int movesCnt = 0;

            while (!outcome.GameOver && timeout == 0)
            {
                // set up input for x and y based on our current location on the maze
                var inputs = new List<RlmIOWithValue>()
                    {
                        new RlmIOWithValue(rlmNet.Inputs.First(a => a.Name == "X"), game.traveler.location.X.ToString()),
                        new RlmIOWithValue(rlmNet.Inputs.First(a => a.Name == "Y"), game.traveler.location.Y.ToString()),
                    };

                // get AI output based on inputs
                RlmCycle cycle = new RlmCycle();
                var aiResult = cycle.RunCycle(rlmNet, currentSessionID, inputs, Learn);
                var direction = Convert.ToInt16(aiResult.CycleOutput.Outputs.First().Value);

                // make the move
                outcome = game.CycleMaze(direction);

                // score AI output
                double score = ScoreAI(outcome, game.traveler);
                rlmNet.ScoreCycle(aiResult.CycleOutput.CycleID, score);

                MazeCycleComplete?.Invoke(game.traveler.location.X, game.traveler.location.Y, outcome.BumpedIntoWall);
                movesCnt++;
            }

            tmr.Stop();

            // compute final score
            outcome.FinalScore = game.CalculateFinalScore(game.Moves);

            // End AI Training
            rlmNet.SessionEnd(outcome.FinalScore);

            SessionComplete?.Invoke(rlmNet.SessionCount, outcome.FinalScore, movesCnt);

            return outcome;
        }

        protected virtual void Rlm_net_CycleComplete(RlmCyclecompleteArgs e)
        {
            if (e.RlmType != RlmNetworkType.Supervised) // Unsupervised & Predict only
            {
                currentCycleID = e.CycleOutput.CycleID;
                var aiOutput = Convert.ToInt16(e.CycleOutput.Outputs.First().Value);

                // queue AI's output - the Direction the AI will go
                GameRef.DirectionsStack.Enqueue(aiOutput);
            
            }
        }

        protected virtual void GameRef_GameStartEvent(Traveler traveler, int currentIteration = 1)
        {
            try
            {
                //Start AI Training Cycle
                currentSessionID = rlmNet.SessionStart();
                
                if (SetRandomnessLeft != null)
                {
                    SetRandomnessLeft(rlmNet.RandomnessCurrentValue);
                }

                //Make your first move
                var initial_inputs = new List<RlmIOWithValue>()
                {
                    new RlmIOWithValue(rlmNet.Inputs.First(a => a.Name == "X"), traveler.location.X.ToString()),
                    new RlmIOWithValue(rlmNet.Inputs.First(a => a.Name == "Y"), traveler.location.Y.ToString()),
                    //new RlmIOWithValue(rnn_net.Inputs.First(a => a.Name == "BumpedIntoWall"), false.ToString())
                };

                RlmCycle cycle = new RlmCycle();
                cycle.RunCycle(rlmNet, currentSessionID, initial_inputs, Learn);
            }
            catch (Exception)
            {
                if (MazeCycleError != null)
                    MazeCycleError(rlmNet.CurrentNetworkName);
            }
        }

        public virtual void GameRef_GameOverEvent(MazeGameFinalOutcome final)
        {
            System.Diagnostics.Debug.WriteLine($"Randomness left: {rlmNet.RandomnessCurrentValue}");

            //End AI Training Cycle
            rlmNet.SessionEnd(final.FinalScore);
        }

        protected virtual void GameRef_GameCycleEvent(MazeCycleOutcome mazeCycle, Traveler traveler)
        {
            try
            {
                // A Cycle Ended, Mark the Score, then make your next move

                // determine score
                double score = ScoreAI(mazeCycle, traveler);
                rlmNet.ScoreCycle(currentCycleID, score);

                if (!mazeCycle.GameOver)
                {
                    // start next AI's move
                    var inputs = new List<RlmIOWithValue>()
                    {
                        new RlmIOWithValue(rlmNet.Inputs.First(a => a.Name == "X"), traveler.location.X.ToString()),
                        new RlmIOWithValue(rlmNet.Inputs.First(a => a.Name == "Y"), traveler.location.Y.ToString()),
                        //new RlmIOWithValue(rnn_net.Inputs.First(a => a.Name == "BumpedIntoWall"), mazeCycle.BumpedIntoWall.ToString())
                    };

                    RlmCycle cycle = new RlmCycle();
                    cycle.RunCycle(rlmNet, currentSessionID, inputs, Learn);

                }
            }
            catch (Exception)
            {
                if (MazeCycleError != null)
                    MazeCycleError(rlmNet.CurrentNetworkName);
            }
        }

        protected virtual double ScoreAI(MazeCycleOutcome mazeCycle, Traveler traveler)
        {
            double retVal = WRONG_SCORE;

            if (mazeCycle.GameOver)
            {
                retVal = CORRECT_SCORE * 2;
            }
            else if (!mazeCycle.BumpedIntoWall)
            {
                retVal = CORRECT_SCORE;
            }

            return retVal;
        }

        public virtual void Dispose()
        {
            if (rlmNet != null)
            {
                rlmNet.CycleComplete -= Rlm_net_CycleComplete;
            }

            if (GameRef != null)
            {
                GameRef.GameCycleEvent -= GameRef_GameCycleEvent;
                GameRef.GameStartEvent -= GameRef_GameStartEvent;
                // GameRef.GameOverEvent -= GameRef_GameOverEvent;
            }
        }

        public virtual void TrainingDone()
        {
            rlmNet.TrainingDone();
        }

    }
}

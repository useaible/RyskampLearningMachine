using ChallengerLib.Models;
using MazeGameLib;
using PoCTools.Settings;
using RLM;
using RLM.SQLServer;
using RLM.Enums;
using RLM.Models;
using RLM.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RLM.PostgreSQLServer;

namespace ChallengerLib.RLM
{
    public delegate void OptimizationStatusDelegate(string status);

    public class RLMChallenger : Traveler
    {
        private SimulationConfig config;
        private ChallengerSimulationSettings simSettings;
        private RlmNetwork network;
        private ChallengerSimulator gameRef;       

        public event MazeCycleCompleteDelegate MazeCycleComplete;
        public event MazeCycleErrorDelegate MazeCycleError;
        public event SessionStartedDelegate SessionStarted;
        public event MazeGameLib.SessionCompleteDelegate SessionComplete;
        public event OptimizationStatusDelegate OptimizationStatus;

        private List<MoveDetails> recentMoves = new List<MoveDetails>();
        public IEnumerable<MoveDetails> RecentMoves
        {
            get { return recentMoves; }
        }

        public int HighestMoveCount { get; set; } = 0;
        
        public RLMChallenger(SimulationConfig config, ChallengerSimulationSettings simSettings)
        {
            this.config = config;
            this.simSettings = simSettings;
            //this.gameRef = gameRef;
            //network.CycleComplete += Rlm_net_CycleComplete;
        }
        
        public void CreateOrLoadNetwork(string dbIdentifier, int numSessions = 1, int startRandomness = 1, int endRandomness = 1)
        {
            //IRlmDbData rlmDbData = new RlmDbDataPostgreSqlServer(dbIdentifier);
            IRlmDbData rlmDbData = new RlmDbDataSQLServer(dbIdentifier);
            network = new RlmNetwork(rlmDbData);

            network.NumSessions = numSessions;
            network.StartRandomness = startRandomness;
            network.EndRandomness = endRandomness;

            //rlmNet.DataPersistenceComplete += RlmNet_DataPersistenceComplete;
            //rlmNet.DataPersistenceProgress += RlmNet_DataPersistenceProgress;

            if (!network.LoadNetwork(config.Name))
            {
                var inputs = new List<RlmIO>()
                {
                    //new RlmIO("X", typeof(Int32).ToString(), 0, config.Width - 1, RlmInputType.Distinct),
                    //new RlmIO("Y", typeof(Int32).ToString(), 0, config.Height - 1,  RlmInputType.Distinct),
                    new RlmIO("Move", typeof(Int32).ToString(), 0, 1000, RlmInputType.Distinct)
                };

                var outputs = new List<RlmIO>()
                {
                    new RlmIO("Direction", typeof(Int16).ToString(), 0, 3)
                };

                network.NewNetwork(config.Name, inputs, outputs);
            }
        }
        
        public virtual MazeCycleOutcome Travel(int? timerTimeout = null, CancellationToken? token = null)
        {
            const int START_RANDOM = 30;
            const int END_RANDOM = 5;

            OptimizationStatus?.Invoke("Initializing...");

            int sessions = simSettings.SimType == SimulationType.Sessions ? simSettings.Sessions.Value : 100;
            DateTime startedOn = simSettings.StartedOn = DateTime.Now;
            if (simSettings.SimType == SimulationType.Time)
            {
                simSettings.EndsOn = startedOn.AddHours(simSettings.Hours.Value);
            }

            CreateOrLoadNetwork(simSettings.DBIdentifier, sessions, START_RANDOM, END_RANDOM);

            OptimizationStatus?.Invoke("Training...");
               
            MazeCycleOutcome outcome = null;
            bool stopTraining = false;
            do
            {
                for (int i = 0; i < sessions; i++)
                {
                    outcome = Travel(true, token: token, perMoveDisplay: simSettings.EnableSimDisplay);

                    if ((simSettings.SimType == SimulationType.Score && outcome.FinalScore >= simSettings.Score.Value) ||
                        (simSettings.SimType == SimulationType.Time && simSettings.EndsOn < DateTime.Now) ||
                        (token.HasValue && token.Value.IsCancellationRequested))
                    {
                        stopTraining = true;
                        break;
                    }
                }

                if (simSettings.SimType == SimulationType.Sessions)
                {
                    stopTraining = true;
                }

                ResetRLM();
            } while (!stopTraining);

            if (!token.HasValue || (token.HasValue && !token.Value.IsCancellationRequested))
            {
                if (simSettings.SimType == SimulationType.Score)
                {
                    for (int i = 0; i < SimulationSettings.NUM_SCORE_HITS - 1; i++)
                    {
                        Travel(false, token: token, perMoveDisplay: simSettings.EnableSimDisplay);
                    }
                }

                Travel(false, timerTimeout, token, true);

                TrainingDone();

                OptimizationStatus?.Invoke("Done");
            }

            return outcome;
        }

        private MazeCycleOutcome Travel(bool learn, int? timerTimeout = null, CancellationToken? token = null, bool perMoveDisplay = false)
        {
            IMazeGame game = GetNewGameInstance();

            MazeCycleOutcome outcome = new MazeCycleOutcome();

            // Start AI Training
            var currentSessionID = network.SessionStart();

            SessionStarted?.Invoke(network.RandomnessCurrentValue);

            recentMoves.Clear();

            //tmr.Interval = timerTimeout;
            //tmr.Start();
            //timeout = 0;
            int movesCnt = 1;

            while (!outcome.GameOver) // && timeout == 0)
            {
                if (token?.IsCancellationRequested == true)
                {
                    break;
                }

                // set up input for x and y based on our current location on the maze
                var inputs = new List<RlmIOWithValue>()
                {
                    //new RlmIOWithValue(network.Inputs.First(a => a.Name == "X"), game.traveler.location.X.ToString()),
                    //new RlmIOWithValue(network.Inputs.First(a => a.Name == "Y"), game.traveler.location.Y.ToString()),
                    new RlmIOWithValue(network.Inputs.First(a => a.Name == "Move"), movesCnt.ToString())
                };

                // get AI output based on inputs
                RlmCycle cycle = new RlmCycle();
                var aiResult = cycle.RunCycle(network, currentSessionID, inputs, learn);
                var direction = Convert.ToInt16(aiResult.CycleOutput.Outputs.First().Value);

                // make the move
                outcome = game.CycleMaze(direction);


                // score AI output
                double score = 0.0;
                //score = ScoreAI(outcome, game.traveler);
                network.ScoreCycle(aiResult.CycleOutput.CycleID, score);

                if (timerTimeout.HasValue)
                    Task.Delay(timerTimeout.Value).Wait();

                if (perMoveDisplay)
                    MazeCycleComplete?.Invoke(game.traveler.location.X, game.traveler.location.Y, outcome.BumpedIntoWall);

                if (!learn)
                    recentMoves.Add(new MoveDetails() { Direction = direction, MoveNumber = movesCnt });

                movesCnt++;
            }

            //tmr.Stop();

            // compute final score
            outcome.FinalScore = game.CalculateFinalScore(game.Moves);

            // End AI Training
            network.SessionEnd(outcome.FinalScore);

            SessionComplete?.Invoke(network.SessionCount, outcome.FinalScore, movesCnt - 1);

            if (movesCnt > HighestMoveCount)
                HighestMoveCount = movesCnt;

            return outcome;
        }

        public void ResetRLM()
        {
            network.ResetRandomizationCounter();
        }

        public void TrainingDone()
        {
            network.TrainingDone();
        }

        public IMazeGame GetNewGameInstance()
        {
            var game = new ChallengerSimulator();
            game.traveler = this;
            game.InitGame(config);
            gameRef = game;
            return game;
        }

        private TravelerLocation mostRecent;
        private TravelerLocation previous;
        protected double ScoreAI(MazeCycleOutcome mazeCycle, Traveler traveler)
        {
            const double GOOD_MOVE = 1.0;
            const double BAD_MOVE = -1.0;

            double retVal = BAD_MOVE;
            
            Block block = gameRef.SimulationArea[traveler.location.X, traveler.location.Y];

            //if (gameRef.BumpedObject)
            //{
            //    Block block = gameRef.SimulationArea[traveler.location.X, traveler.location.Y];
            //    // empty block or good block or end block = good move
            //    if (block == null || (block != null && (block.Score > 0 || block.IsEndSimulation)))
            //    {
            //        retVal = GOOD_MOVE;
            //    }
            //}

            /* scores are either 1 or -1 */
            if ((block == null && !mazeCycle.BumpedIntoWall) || (block != null && (block.Score > 0 || block.IsEndSimulation)))
            {
                retVal = GOOD_MOVE;
            }


            /* scores are either 1 or -1 BUT punishes if it goes back to previous block */
            //if ((block == null && !mazeCycle.BumpedIntoWall) || (block != null && (block.Score > 0 || block.IsEndSimulation)))
            //{
            //    if (previous != null && previous.X == traveler.location.X && previous.Y == traveler.location.Y)
            //    {
            //        retVal = BAD_MOVE;
            //    }
            //    else
            //    {
            //        retVal = GOOD_MOVE;
            //    }
            //}


            /* score is based on the block and 1 or -1 if empty or hits boundaries */
            //if (block == null)
            //{
            //    if (!mazeCycle.BumpedIntoWall)
            //    {
            //        retVal = GOOD_MOVE;
            //    }
            //}
            //else
            //{
            //    retVal = block.Score;
            //}


            /* score is based on the block BUT punishes if it goes back to previous block */
            //if (block == null)
            //{
            //    if (!mazeCycle.BumpedIntoWall)
            //    {
            //        retVal = GOOD_MOVE;
            //    }
            //}
            //else
            //{
            //    if (previous != null && previous.X == traveler.location.X && previous.Y == traveler.location.Y)
            //    {
            //        retVal = (block.Score > 0) ? -(block.Score) : block.Score;
            //    }
            //    else
            //    {
            //        retVal = block.Score;
            //    }
            //}


            previous = (mostRecent == null) ? null : new TravelerLocation() { X = mostRecent.X, Y = mostRecent.Y };
            mostRecent = new TravelerLocation() { X = traveler.location.X, Y = traveler.location.Y };

            return retVal;
        }
    }
}

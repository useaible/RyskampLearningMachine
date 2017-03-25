using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using RNN;

namespace WPFMazeApp
{
    public abstract class Traveler
    {

        public MazeGame GameRef;
        public TravelerLocation location;

        public Traveler(MazeGame gameref)
        {
            GameRef = gameref;
        }

        public abstract void AcceptUIKeyDown(object sender, KeyEventArgs e);
    }

    public class HumanTraveler:Traveler
    {
        public HumanTraveler(MazeGame gameref):base(gameref)
        {
           
        }

        override public void AcceptUIKeyDown(object sender, KeyEventArgs e)
        {
            //send based upon key
            // ^=0, >=1, D=2, <=3
            if (e.Key == Key.Up)
            {
                GameRef.DirectionsStack.Enqueue(0);   //0 for UP
            }

            if (e.Key == Key.Right)
            {
                GameRef.DirectionsStack.Enqueue(1);   //1 for Right
            }

            if (e.Key == Key.Down)
            {
                GameRef.DirectionsStack.Enqueue(2);   //2 for Down
            }

            if (e.Key == Key.Left)
            {
                GameRef.DirectionsStack.Enqueue(3);   //3 for UP
            }
        }
    }

    public class AITraveler : Traveler, IDisposable
    {
        private const string NETWORK_NAME = "AIMazeTraveler01";
        private const double CORRECT_SCORE = 100;
        private const double WRONG_SCORE = 0;
        private rnn_network rnn_net;
        private Int64 currentSessionID;
        private Int64 currentCycleID;
        private bool learn;

        public AITraveler(MazeGame gameref, bool learn = false)
            : base(gameref)
        {
            this.learn = learn;
            GameRef.GameStartEvent += GameRef_GameStartEvent;
            GameRef.GameCycleEvent += GameRef_GameCycleEvent;
            //GameRef.GameOverEvent += GameRef_GameOverEvent;

            rnn_net = new rnn_network();

            if (!rnn_net.LoadNetwork(NETWORK_NAME))
            {
                var inputs = new List<rnn_io>()
                {
                    new rnn_io(rnn_net, "X", typeof(Int32).ToString(), 0, 49),
                    new rnn_io(rnn_net, "Y", typeof(Int32).ToString(), 0, 49),
                    new rnn_io(rnn_net, "BumpedIntoWall", typeof(Boolean).ToString(), 0, 1)
                };

                var outputs = new List<rnn_io>()
                {
                    new rnn_io(rnn_net, "Direction", typeof(Int16).ToString(), 0, 3)
                };

                rnn_net.NewNetwork(NETWORK_NAME, inputs, outputs);
            }
            
            rnn_net.CycleComplete += Rnn_net_CycleComplete;
        }

        private void Rnn_net_CycleComplete(rnn_cyclecomplete_event_args e)
        {
            if (e.RnnType != rnn_type.Supervised) // Unsupervised & Predict only
            {
                currentCycleID = e.CurrentCase.ID;
                var aiOutput = Convert.ToInt16(e.CurrentCase.Solution.Output_Values_Solutions.First().Value);

                // queue AI's output - the Direction the AI will go
                GameRef.DirectionsStack.Enqueue(aiOutput);
            }
        }

        void GameRef_GameStartEvent(Traveler traveler)
        {
            //Start AI Training Cycle
            currentSessionID = rnn_net.SessionStart();

            //Make your first move
            var initial_inputs = new List<rnn_io_with_value>()
            {
                new rnn_io_with_value(rnn_net.Inputs.First(a => a.Name == "X"), traveler.location.X.ToString()),
                new rnn_io_with_value(rnn_net.Inputs.First(a => a.Name == "Y"), traveler.location.Y.ToString()),
                new rnn_io_with_value(rnn_net.Inputs.First(a => a.Name == "BumpedIntoWall"), false.ToString())
            };

            rnn_cycle cycle = new rnn_cycle();
            cycle.RunCycle(rnn_net, currentSessionID, initial_inputs, learn);
        }

        public void GameRef_GameOverEvent(MazeGameFinalOutcome final)
        {
            //End AI Training Cycle
            rnn_net.SessionEnd(final.FinalScore);
        }

        void GameRef_GameCycleEvent(MazeCycleOutcome mazeCycle, Traveler traveler)
        {
            // A Cycle Ended, Mark the Score, then make your next move

            // determine score
            double score = WRONG_SCORE;
            if (mazeCycle.GameOver)
            {
                score = CORRECT_SCORE * 2;
            }
            else if (!mazeCycle.BumpedIntoWall)
            {
                score = CORRECT_SCORE;
            }
            rnn_net.ScoreCycle(currentCycleID, score);

            // start next AI's move
            var inputs = new List<rnn_io_with_value>()
            {
                new rnn_io_with_value(rnn_net.Inputs.First(a => a.Name == "X"), traveler.location.X.ToString()),
                new rnn_io_with_value(rnn_net.Inputs.First(a => a.Name == "Y"), traveler.location.Y.ToString()),
                new rnn_io_with_value(rnn_net.Inputs.First(a => a.Name == "BumpedIntoWall"), mazeCycle.BumpedIntoWall.ToString())
            };

            rnn_cycle cycle = new rnn_cycle();
            cycle.RunCycle(rnn_net, currentSessionID, inputs, learn);
        }

        override public void AcceptUIKeyDown(object sender, KeyEventArgs e)
        {
            //Ignore the human no input on AI driven games.
        }

        public void Dispose()
        {
            if (rnn_net != null)
            {
                rnn_net.CycleComplete -= Rnn_net_CycleComplete;
            }

            if (GameRef != null)
            {
                GameRef.GameCycleEvent -= GameRef_GameCycleEvent;
                GameRef.GameStartEvent -= GameRef_GameStartEvent;
               // GameRef.GameOverEvent -= GameRef_GameOverEvent;
            }
        }
    }
}

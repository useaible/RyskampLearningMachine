using MazeGameLib;
using RLM;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WPFMazeApp
{
    public enum PlayerType
    {
        Human,
        RNN,
        Encog
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        public MazeGameLib.MazeGame game;
        public Maze maze;
        public MazeInfo mazeInfo;
        public bool ClosedDueToGameOver { get; set; }
        public bool Learn { get; set; }
        public int Temp_num_sessions { get; set; }
        public int StartRandomness { get; set; }
        public int EndRandomness { get; set; }
        public int RandomnessOver { get; set; }
        private MazeRepo mazerepo;
        private PlayerType Type { get; set; }

        public int CurrentIteration { get; set; }

        public MainWindow(int mazeId, PlayerType type, Boolean learn = false, int temp_num_sessions = 1, int currentIteration = 1, int totalIterations = 1, int startRandomness = 1, int endRandomness = 1, int randomnessOver=1)
        {
            InitializeComponent();
            mazerepo = new MazeRepo();

            mazeInfo = mazerepo.GetByID(mazeId);
            RandomnessOver = temp_num_sessions;
            ClosedDueToGameOver = false;
            Learn = learn;
            Type = type;
            if (Type == PlayerType.Human)
            {
                this.Title = "Human Player";
                StatusText.Visibility = Visibility.Visible;
            }
            else if (Type == PlayerType.RNN)
            {            
                if (learn)
                {
                    Temp_num_sessions = totalIterations;
                    StartRandomness = startRandomness;
                    EndRandomness = endRandomness;

                    this.Title = "RNN Learning";
                    statGrid.Visibility = Visibility.Visible;
                    lblMazeName.Content = mazeInfo.Name;
                    lblTotalSession.Content = totalIterations;
                    lblCurrentSession.Content =  CurrentIteration = currentIteration;
                    lblScore.Content = LastScore;
                    lblMoves.Content = LastMovesCount;
                }
                else
                {
                    this.Title = "RNN Player";
                }
            }
            else
            {
                this.Title = "Encog Player";
            }
                        
            game = new MazeGameLib.MazeGame();
            maze = new Maze();
            game.InitGame(mazeInfo);
            //Initialize Grid
            game.TheMazeGrid = mazeInfo.Grid;
            maze.InitializeMaze(game, mazeGrid);
           
            //Init Game
            //game.InitGame(this.MainGrid, new RunThreadUI_Delegate(RunUIThread));
            
            //Send Keys to Game
            if (Type == PlayerType.Human)
            {
                this.KeyDown += MainWindow_KeyDown;
            }
            
            // reset static session count
            if (currentIteration == 1)
            {
                //RNNMazeTraveler.ResestStaticSessionData();
            }

            //Send shutdown event to game
            //this.Closed += MainWindow_Closed;
        }

        public static int LastMovesCount { get; set; }
        public static double LastScore { get; set; }
        public static double LastBumpedIntoWallData { get; set; }

        private static double _randomnessLeft { get; set; }
        object[] replayMemory;
        List<Location> locations;
        
        public static void ResetStaticData()
        {
            LastScore = 0;
            LastMovesCount = 0;
            LastBumpedIntoWallData = 0;
        }

        async void Init(MazeInfo mazeInfo)
        {

            if (!Learn)
            {
                Temp_num_sessions = 1;
                StartRandomness = 0;
                EndRandomness = 0;
            }
            replayMemory = new object[Temp_num_sessions];
            StatusText.Content = "Initializing RLM engine...";
            await Task.Run(() => {

                RLMMazeTraveler traveler = new RLMMazeTraveler(mazeInfo, Learn, Temp_num_sessions, StartRandomness, EndRandomness); //Instantiate RlmMazeTraveler game lib to configure the network.
                traveler.SessionComplete += Traveler_SessionComplete;
                traveler.MazeCycleComplete += Traveler_MazeCycleComplete;
                traveler.SessionStarted += Traveler_SessionStarted;
                traveler.MazeCycleError += mazecycle_error;
                game.traveler = traveler;

                if(Learn)
                {
                    RunUIThread(() => {
                        StatusText.Content = "Training started...";
                    });
                    //Start the training (Play the game)
                    for (int i = 0; i < RandomnessOver; i++)
                    {
                        locations = new List<Location>();
                        RunUIThread(() => {
                            lblCurrentSession.Content = 1;
                        });
                        traveler.Travel(5000);
                    }

                    // set to predict instead of learn for the remaining sessions
                    traveler.Learn = false;
                    
                    for (int i = 0; i < Temp_num_sessions - RandomnessOver; i++)
                    {
                        locations = new List<Location>();
                        RunUIThread(() => {
                            StatusText.Content = $"Training started... {CurrentIteration*100/Temp_num_sessions}%";
                        });
                        traveler.Travel(5000);
                    }


                    traveler.TrainingDone();
                    RunUIThread(() => {
                        StatusText.Content = $"Training done... 100%";
                    });
                }
                else
                {                 
                    RunUIThread(() => {
                        StatusText.Content = $"RLM preparing to play...";
                    });

                    traveler.Learn = false;
  
                    locations = new List<Location>();

                    RunUIThread(() => {
                        StatusText.Content = $"RLM Playing...";
                    });

                    traveler.Travel(5000);
                    traveler.TrainingDone();
                }
                

            }).ContinueWith(async (t) => {
                //show AI playing game
                Stopwatch watch = new Stopwatch();
                foreach (dynamic obj in replayMemory)
                {

                    RunUIThread(() =>
                    {
                        lblCurrentSession.Content = (int)obj.cycleNum + 1;
                        lblRandomness.Content = (int)obj.randomnessLeft;
                    });

                    watch.Start();
                    foreach (Location loc in obj.moves as List<Location>)
                    {
                       
                        var x = loc.X;
                        var y = loc.Y;
                        RunUIThread(() =>
                        {
                            maze.ChangeCellColor(new TravelerLocation() { X = loc.X, Y=loc.Y }, true);            
                        });
                        await Task.Delay(TimeSpan.FromMilliseconds(1));
                        //If game is not solved within 5s, go to the next session.
                        if (watch.Elapsed.TotalSeconds >= 5) 
                            break;
                    }
                    
                    watch.Reset();

                    RunUIThread(() => {
                        lblScore.Content = (double)obj.score;
                        lblMoves.Content = (int)obj.movesCnt;

                        if(!Learn)
                            StatusText.Content = $"RLM done playing...";
                        else
                            StatusText.Content = $"Showing rlm replay...";

                        maze.setGoalRect();
                    });
           
                }
            },TaskContinuationOptions.OnlyOnRanToCompletion);
           
        }

        private void Traveler_SessionStarted(double randomnessLeft)
        {
            _randomnessLeft = randomnessLeft;

        }

        private void Traveler_MazeCycleComplete(int x, int y, bool bumpedIntoWall)
        {
            locations.Add(new Location() { X=x, Y=y });           
        }

        private void Traveler_SessionComplete(int cycleNum, double score, int movesCnt)
        {
            replayMemory[cycleNum] = new { randomnessLeft = _randomnessLeft, moves=locations, movesCnt=movesCnt, score=score, cycleNum=cycleNum};
            CurrentIteration = cycleNum + 1;     
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Init(mazeInfo);    
        }

        private void mazecycle_error(string networkName)
        {
            MessageBox.Show($"Your AI needs to learn more. Click on the RNN Learn tab and click on Start learning. Your maze {networkName} will close now.");
            this.Close();
        }

       
        void MainWindow_Closed(object sender, EventArgs e)
        {
            game.CancelGame = true;
            
            if (!ClosedDueToGameOver)
            {

                Environment.Exit(0);
            }
        }

        public Task WhenClosed()
        {
            var tcs = new TaskCompletionSource<object>();

            this.Closed += (e, args) =>
            {
                game.CancelGame = true;
          
                if (!ClosedDueToGameOver)
                {
                    Dispose();
                }

                tcs.SetResult(null);
            };

            return tcs.Task;
        }

        void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            //RunUIThread(() =>
            //{
            //    StatusText.Text = $"X:{ game.traveler.location.X } Y:{ game.traveler.location.Y }";
            //});
            //game.UI_Send_KeyDown(sender, e);
        }

        //pass calls to thread UI
        public void RunUIThread(Action act)
        {
            Dispatcher.BeginInvoke(act);
        }

        public void Dispose()
        {
            if (game != null)
            {
                if (Type == PlayerType.RNN)
                {
                    ((MazeGameLib.RLMMazeTraveler)game.traveler).Dispose();
                }
            }
        }

    }
}

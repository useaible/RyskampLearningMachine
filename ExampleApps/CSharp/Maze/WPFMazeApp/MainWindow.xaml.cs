using MazeGameLib;
using RLM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
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
        private MazeRepo mazerepo;
        //Timer to make the cursor blink
        private System.Timers.Timer BlinkTimer;
        private PlayerType Type { get; set; }

        public int CurrentIteration { get; set; }

        public MainWindow(int mazeId, PlayerType type, Boolean learn = false, int temp_num_sessions = 1, int currentIteration = 1, int totalIterations = 1, int startRandomness = 1, int endRandomness = 1)
        {
            InitializeComponent();
            mazerepo = new MazeRepo();

            mazeInfo = mazerepo.GetByID(mazeId);
            
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
                    Temp_num_sessions = temp_num_sessions;
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
            
            //GameOverEvent
            game.GameOverEvent += game_GameOverEvent;

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

        public static void ResetStaticData()
        {
            LastScore = 0;
            LastMovesCount = 0;
            LastBumpedIntoWallData = 0;
        }

        async void Init(MazeInfo maze)
        {
            await Task.Run(() => { 
            ClosedDueToGameOver = false;
            //Setup Blink on UI Thread
            if (BlinkTimer == null)
            {
                BlinkTimer = new System.Timers.Timer(1);
                BlinkTimer.Elapsed += Blink_Elapsed;
                BlinkTimer.Start();
            }
            else
            {
                BlinkTimer.Start();
            }

            //Start Game
            if (Type == PlayerType.RNN)
            {
                if (game.traveler != null && game.traveler is MazeGameLib.RLMMazeTraveler)
                {
                    ((MazeGameLib.RLMMazeTraveler)game.traveler).Dispose();
                    game.traveler = null;
                }

                var rnn_traveler = new MazeGameLib.RLMMazeTraveler(maze, game, Learn, Temp_num_sessions, StartRandomness, EndRandomness, SetRandomness);
                
                if (Learn)
                {
                        RunUIThread(() => { lblRandomness.Content = rnn_traveler.RandomnessLeft.ToString("#0.##") + "%"; });
                        
                    if (CurrentIteration > 1)
                    {
                            RunUIThread(() => { lblBumpedIntoWall.Content = $"{LastBumpedIntoWallData.ToString("#0.##")}%"; });
                            
                    }
                }
                
                rnn_traveler.MazeCycleError += mazecycle_error;
                
                //start game
                
                game.StartGame(rnn_traveler, CurrentIteration);
            }
            else if (Type == PlayerType.Encog)
            {
                var encogMaze = new EncogMaze(maze, game);
                game.StartGame(encogMaze);
            }
            else
            {
                //game.StartGame();
            }
            });
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

        void game_GameOverEvent(MazeGameLib.MazeGameFinalOutcome final)
        {
            LastScore = final.FinalScore;
            LastMovesCount = final.CycleCount;

            if (Type == PlayerType.RNN)
            {
                var rnnTraveler = ((MazeGameLib.RLMMazeTraveler)game.traveler);
                rnnTraveler.GameRef_GameOverEvent(final);
                if (CurrentIteration == 1)
                {
                    //LastBumpedIntoWallData = rnnTraveler.BumpedIntoWallPercentage;
                }
                else
                {
                    //LastBumpedIntoWallData = rnnTraveler.BumpedIntoWallPercentage;
                }
            }

            Action act = new Action(delegate
            { 
                StatusText.Content = "The game is over.  The final score is " + final.FinalScore.ToString() + ".  It took " + final.CycleCount.ToString() + " moves to complete the game.";

            });

            RunUIThread(act);

            if (Learn == true)
            {               
                ClosedDueToGameOver = true;

                RunUIThread(() => { this.Close(); });
            }


        }

        void MainWindow_Closed(object sender, EventArgs e)
        {
            game.CancelGame = true;
            
            if (!ClosedDueToGameOver)
            {

                if (BlinkTimer != null)
                {
                    BlinkTimer.Stop();
                    BlinkTimer.Dispose(); 
                }
                Environment.Exit(0);
            }
        }

        public Task WhenClosed()
        {
            var tcs = new TaskCompletionSource<object>();

            this.Closed += (e, args) =>
            {
                game.CancelGame = true;

                //if (!ClosedDueToGameOver)
                //{
                //    Environment.Exit(0);
                //}
               
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

        private void Blink_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            BlinkTimer.Stop();
            //Create an action and blink on the UI thread
            if (game.BumpIntoWall)
            {
                OutOfBoundsOccurred();
                game.BumpIntoWall = false;
            }
                
            RunUIThread(() =>
            {
                
                maze.ChangeCellColor(game.OldLocation, false);
                maze.ChangeCellColor(game.traveler.location, !game.cursorstate.IsDarkened);
            });
            //RunThreadUI(act);
            BlinkTimer.Start();
        }

        public void Dispose()
        {
            if (game != null)
            {
                if(BlinkTimer != null)
                {
                    BlinkTimer.Stop();
                    BlinkTimer.Dispose();
                }

                if (Type == PlayerType.RNN)
                {
                    ((MazeGameLib.RLMMazeTraveler)game.traveler).Dispose();
                }
            }
        }

        private void UI_Send_KeyDown(object sender, KeyEventArgs e)
        {
            RunUIThread(() =>
            {
                StatusText.Content = $"X:{ game.traveler.location.X } Y:{ game.traveler.location.Y }";
            });
            short key = 0;

            if (e.Key == Key.Up)
            {
                key = 0;
                //GameRef.DirectionsStack.Enqueue(0);   //0 for UP
            }

            if (e.Key == Key.Right)
            {
                key = 1;
                //GameRef.DirectionsStack.Enqueue(1);   //1 for Right
            }

            if (e.Key == Key.Down)
            {
                key = 2;
                //GameRef.DirectionsStack.Enqueue(2);   //2 for Down
            }

            if (e.Key == Key.Left)
            {
                key = 3;
                //GameRef.DirectionsStack.Enqueue(3);   //3 for UP
            }

            //game.traveler.AcceptUIKeyDown(key);

            
        }

        private void OutOfBoundsOccurred()
        {
            //Play sound
            SystemSounds.Exclamation.Play();
        }

        private void SetRandomness(double val)
        {
            RunUIThread(() =>
            {
                lblRandomness.Content = val.ToString("#0.##") + "%";
            });        
        }
    }
}

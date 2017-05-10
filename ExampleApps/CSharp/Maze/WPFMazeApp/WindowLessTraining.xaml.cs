using MazeGameLib;
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
using System.Windows.Shapes;

namespace WPFMazeApp
{
    /// <summary>
    /// Interaction logic for WindowLessTraining.xaml
    /// </summary>
    public partial class WindowLessTraining : Window
    {
        public MazeGameLib.MazeGame game;
        public bool ClosedDueToGameOver { get; set; }
        public bool Learn { get; set; }
        public int TotalIterations { get; set; }
        public int CurrentIteration { get; set; }
        public int Temp_num_sessions { get; set; }
        public int StartRandomness { get; set; }
        public int EndRandomness { get; set; }
        
        private int MazeId;
        private MazeRepo mazerepo;
        private MazeInfo maze;
        private RLMMazeTraveler traveler;
        private bool isEncog;
        
        // constructor for RNN
        public WindowLessTraining(int mazeId, Boolean learn, int totalIterations, int temp_num_sessions, int startRandomness = 1, int endRandomness = 1)
        {
            InitializeComponent();

            mazerepo = new MazeRepo();
            maze = mazerepo.GetByID(mazeId);
            MazeId = mazeId;

            Learn = learn;
            TotalIterations = totalIterations;

            Temp_num_sessions = temp_num_sessions;
            StartRandomness = startRandomness;
            EndRandomness = endRandomness;
            isEncog = false;

            lblMazeName.Content = maze.Name;
            lblCurrentSession.Content = 1;
            lblSessionCount.Content = totalIterations.ToString();
            lblMoveCount.Content = 0;

            this.Title = "No Maze RNN Learning";
        }

        private int maxTemp;
        private int minTemp;
        private int encogCycles;
        // constructor for Encog
        public WindowLessTraining(int mazeId, int totalIterations, int maxTemp, int minTemp, int encogCycles)
        {
            InitializeComponent();

            mazerepo = new MazeRepo();
            maze = mazerepo.GetByID(mazeId);
            MazeId = mazeId;

            TotalIterations = totalIterations;
            this.maxTemp = maxTemp;
            this.minTemp = minTemp;
            this.encogCycles = encogCycles;
            isEncog = true;

            lbl1.Content = "# of Iterations:";
            lbl2.Content = "Iterations done:";
            lbl3.Content = "# of Cycles per:";
            lbl4.Content = "Cycles done:";
            lbl5.Visibility = Visibility.Hidden;

            lblMazeName.Content = maze.Name;
            lblSessionCount.Content = totalIterations.ToString();
            lblCurrentSession.Content = 0;
            lblMoveCount.Content = encogCycles.ToString();
            lblSessionsCompleted.Content = 0;
            lblRandomness.Visibility = Visibility.Hidden;

            this.Title = "No Maze Encog Learning";
        }
                
        private async Task StartGame(RLMMazeTraveler traveler)
        {
            int currentSession = 0;
            while (currentSession < TotalIterations)
            {
                currentSession++;
                lblCurrentSession.Content = currentSession;

                //var outcome = await traveler.Travel(currentSession);
                var outcome = traveler.Travel(currentSession);
                //var data = traveler.BumpedIntoWallPercentage;
                //lblBumpedIntoWall.Content = $"{data.ToString("#0.##")}%";
                lblMoveCount.Content = outcome.Moves;
                lblSessionsCompleted.Content = outcome.FinalScore;
            }
        }

        //private Task StartGame(EncogMaze encogMaze)
        //{
        //    Task task = Task.Run(() =>
        //    {
        //        encogMaze.Train(TotalIterations, maxTemp, minTemp, encogCycles);
        //    });

        //    return task;
        //}

        public void RunUIThread(Action act)
        {
            Dispatcher.BeginInvoke(act);
        }
        
        private async void windowLessTraining_Loaded_1(object sender, RoutedEventArgs e)
        {
            if (isEncog)
            {
                //EncogMaze encogMaze = new EncogMaze(maze);
                //encogMaze.MazeCycleComplete += Traveler_MazeCycleComplete;
                ////encogMaze.EncogCycleComplete += EncogMaze_EncogCycleComplete;
                //encogMaze.TrainingIterationComplete += EncogMaze_TrainingIterationComplete;
                //await StartGame(encogMaze);
            }
            else
            {
                // reset session count
                //RNNMazeTraveler.ResestStaticSessionData();
                traveler = new RLMMazeTraveler(maze, Learn, Temp_num_sessions, StartRandomness, EndRandomness);
                traveler.MazeCycleComplete += Traveler_MazeCycleComplete;
                traveler.SessionStarted += Traveler_SessionStarted;
                await StartGame(traveler);
            }
        }

        private void Traveler_SessionStarted(double randomnessLeft)
        {
            RunUIThread(() =>
            {
                lblRandomness.Content = randomnessLeft.ToString("#0.##") + "%";
            });
        }

        private void Traveler_MazeCycleComplete(int x, int y, bool bumpedIntoWall)
        {
            RunUIThread(() =>
            {
                textBlockInfo.Text = $"X: {x} Y: {y} Bump: {bumpedIntoWall}";
            });
        }

        private void EncogMaze_TrainingIterationComplete(int epoch, double score)
        {
            RunUIThread(() =>
            {
                lblCurrentSession.Content = $"{epoch} (score: {score})";
            });
        }

        private void EncogMaze_EncogCycleComplete(int cycleNum)
        {
            RunUIThread(() =>
            {
                lblSessionsCompleted.Content = cycleNum.ToString();
            });
        }
    }
}

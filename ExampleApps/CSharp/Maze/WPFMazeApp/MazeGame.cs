using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Threading;
using System.Media;

namespace WPFMazeApp
{
    //Delegates
    public delegate void GameStartedOccurred_Delegate(Traveler traveler);
    public delegate void GameCycleCompleteOccurred_Delegate(MazeCycleOutcome cycle, Traveler traveler); 
    public delegate void GameOverOccurred_Delegate(MazeGameFinalOutcome final);
    
    //UI Threading Delegates
    public delegate void RunThreadUI_Delegate(Action act);

    //Structs
    public struct TravelerLocation
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public struct MazeCycleOutcome
    {
        public Boolean BumpedIntoWall { get; set; }
        public Boolean GameOver { get; set; }
        public Int32 FinalScore { get; set; }
    }

    public struct MazeGameFinalOutcome
    {
        public int CycleCount { get; set; }
        public Int32 FinalScore { get; set; }
    }

    public struct MazeCursorState
    {
        public Boolean IsDarkened { get; set; }
    }

    public class MazeGame
    {
        //Events
        public event GameStartedOccurred_Delegate GameStartEvent;
        public event GameCycleCompleteOccurred_Delegate GameCycleEvent;
        public event GameOverOccurred_Delegate GameOverEvent;

        //Callbacks
        RunThreadUI_Delegate RunThreadUI;

        public Boolean CancelGame=false;
        public Maze maze;
        public Traveler traveler; //Human or AI, depending on what is called in set traveler
        public MazeCursorState cursorstate;
        public TravelerLocation GoalLocation = new TravelerLocation(); 
        
        //Perfect Game
        Int16 PerfectGameMovesCount = 49;

        //Timer to make the cursor blink
        private System.Timers.Timer BlinkTimer;

        //Multi-threaded directions stack
        public ConcurrentQueue<Int16> DirectionsStack = new ConcurrentQueue<short>();

        public void InitGame(System.Windows.Controls.Grid grd, RunThreadUI_Delegate runthreadui)
        {
            maze = new Maze();
            //Set Goal Location
            GoalLocation.X = 49; GoalLocation.Y = 24;
            //UIThread
            RunThreadUI = runthreadui;
            //Init Maze
            maze.InitializeMaze(this,grd);
            //Init Blink Timer
            //Setup Blink on UI Thread
            if (BlinkTimer == null)
            {
                BlinkTimer = new System.Timers.Timer(300);
                BlinkTimer.Elapsed += Blink_Elapsed;
                BlinkTimer.Start();
            }
            else
            {
                BlinkTimer.Start();
            }
        }

        //For windowless training
        public void InitGame()
        {
            maze = new Maze();
            //Set Goal Location
            GoalLocation.X = 49; GoalLocation.Y = 24;
        }

        public void StartGame(Traveler SpecificTraveler = null, bool windowless = false)
        {
            //Set Default Traveler
            if (SpecificTraveler == null)
            {
                //Default traveler is human for now
                traveler = new HumanTraveler(this);
            }
            else
            {
                traveler = SpecificTraveler;
            }

            //Initial Location
            traveler.location.X = 0; traveler.location.Y = 24;

            //Fire GameStart Event
            if (GameStartEvent != null) GameStartEvent(traveler);

            Thread GameLoopThread = new Thread(() => { this.GameLoop(windowless); });
            GameLoopThread.Start();
        }

        public void StopGame(bool windowless = false)
        {
            if(windowless) BlinkTimer.Stop();
        }

        private void GameLoop(bool windowless = false)
        {
            int i = 0;
            //Cycle
            MazeCycleOutcome LastOutcome = new MazeCycleOutcome();
            while (LastOutcome.GameOver == false )
            {
                if (CancelGame) return;

                Int16 dir = -1;
                if (DirectionsStack.TryDequeue(out dir))
                {               
                    //incriment the count
                    i++;
                    //Run the maze cycle
                    LastOutcome = CycleMaze(dir, windowless);

                    //GameCycledEvent
                    if (GameCycleEvent != null) GameCycleEvent(LastOutcome, traveler);

                    //Check for Game Over
                    if(LastOutcome.GameOver)
                    {
                        //Build Final Game
                        MazeGameFinalOutcome FinalOutcome = new MazeGameFinalOutcome();
                        FinalOutcome.CycleCount = i;
                        FinalOutcome.FinalScore = CalculateFinalScore(i);

                        //Event
                        if (GameOverEvent!=null) GameOverEvent(FinalOutcome);
                    }
                }
                else
                {
                    System.Threading.Thread.Sleep(10);
                }
            }
        }

        public void UI_Send_KeyDown(object sender, KeyEventArgs e)
        {
            traveler.AcceptUIKeyDown(sender, e);
        }

        private MazeCycleOutcome CycleMaze(int direction, bool windowless = false)
        {
            MazeCycleOutcome outcome = new MazeCycleOutcome();
            outcome.BumpedIntoWall = false; outcome.GameOver = false; outcome.FinalScore = 0;

            TravelerLocation new_location = new TravelerLocation();

            //Calculate new location
            switch (direction)
            {
                case 0:
                    if (traveler.location.Y <= 0)
                    {
                        OutOfBoundsOccurred();
                        outcome.BumpedIntoWall = true;
                        return outcome;
                    }
                    new_location.X = traveler.location.X; 
                    new_location.Y = traveler.location.Y-1;
                    break;
                case 1:
                    if (traveler.location.X >= 49)
                    {
                        OutOfBoundsOccurred();
                        outcome.BumpedIntoWall = true;
                        return outcome;
                    }
                    new_location.X = traveler.location.X+1; 
                    new_location.Y = traveler.location.Y;
                    break;
                case 2:
                    if (traveler.location.Y >= 49)
                    {
                        OutOfBoundsOccurred();
                        outcome.BumpedIntoWall = true;
                        return outcome;
                    }
                    new_location.X = traveler.location.X; 
                    new_location.Y = traveler.location.Y+1;
                    break;
                case 3:
                    if (traveler.location.X <= 0)
                    {
                        OutOfBoundsOccurred();
                        outcome.BumpedIntoWall = true;
                        return outcome;
                    }
                    new_location.X = traveler.location.X-1; 
                    new_location.Y = traveler.location.Y;
                    break;
                default:
                    throw new Exception("Not valid input");
            }


            //Is BumpedIntoWall?
            if (maze.TheMazeGrid[new_location.X, new_location.Y])
            {
                outcome.BumpedIntoWall = true;
                outcome.FinalScore = 0;
                outcome.GameOver=false;
                //Play sound
                SystemSounds.Hand.Play();
                return outcome;
            }

            //New location is now current location
            TravelerLocation old_location = traveler.location;
            traveler.location = new_location;

            //Is GameOver?
            if(traveler.location.X==GoalLocation.X && traveler.location.Y == GoalLocation.Y)
            {
                StopGame();
                outcome.GameOver = true;
                return outcome;
            }


            if (!windowless)
            {
                //Clear old location
                Action act = new Action(delegate
                {
                    maze.ChangeCellColor(old_location, false);
                });
                RunThreadUI(act);

                //first blink at new location
                //Clear old location
                Action act2 = new Action(delegate
                {
                    maze.ChangeCellColor(traveler.location, true);
                });
                RunThreadUI(act2);
            }

            return outcome;
        }

        private void Blink_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            BlinkTimer.Stop();
            //Create an action and blink on the UI thread
            Action act = new Action(delegate
            {
                maze.ChangeCellColor(traveler.location, !cursorstate.IsDarkened);
            });
            RunThreadUI(act);
            BlinkTimer.Start();
        }

        private void OutOfBoundsOccurred()
        {
            //Play sound
            SystemSounds.Exclamation.Play();
        }

        private Int32 CalculateFinalScore(Int32 i)
        {
            return (1000000 + (PerfectGameMovesCount*1000)) - (i * 1000); //Dock one hundred points per move
        }
    }
}

   

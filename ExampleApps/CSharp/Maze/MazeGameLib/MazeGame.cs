using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MazeGameLib
{
    //Delegates
    public delegate void GameStartedOccurred_Delegate(Traveler traveler, int currentIteration = 1);
    public delegate void GameCycleCompleteOccurred_Delegate(MazeCycleOutcome cycle, Traveler traveler);
    public delegate void GameOverOccurred_Delegate(MazeGameFinalOutcome final);

    //UI Threading Delegates
    public delegate void RunThreadUI_Delegate(Action act);

    //Structs
    public class TravelerLocation
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class MazeCycleOutcome
    {
        public Boolean BumpedIntoWall { get; set; }
        public Boolean GameOver { get; set; }
        public double FinalScore { get; set; }
        public Location PreviousLocation { get; set; }
        public int Moves { get; set; }
    }

    public class MazeGameFinalOutcome
    {
        public int CycleCount { get; set; }
        public double FinalScore { get; set; }
    }

    public class MazeCursorState
    {
        public Boolean IsDarkened { get; set; }
    }


    public class MazeGame : IMazeGame
    {
        //Events
        public event GameStartedOccurred_Delegate GameStartEvent;
        public event GameCycleCompleteOccurred_Delegate GameCycleEvent;
        public event GameOverOccurred_Delegate GameOverEvent;

        public Boolean CancelGame { get; set; } = false;
        public Traveler traveler { get; set; }
        public MazeCursorState cursorstate { get; set; } = new MazeCursorState();
        public TravelerLocation GoalLocation { get; set; } = new TravelerLocation();
        public TravelerLocation OldLocation { get; set; } = new TravelerLocation();
        public int Width { get; set; } = -1;
        public int Height { get; set; } = -1;
        public bool BumpIntoWall { get; set; } = false;
        public int Moves { get; set; } = 0;
        //Perfect Game
        Int16 PerfectGameMovesCount = 49;

        //Multi-threaded directions stack
        public ConcurrentQueue<Int16> DirectionsStack { get; set; } = new ConcurrentQueue<short>();

        //Build Grid
        public Boolean[,] TheMazeGrid;

        public MazeGame()
        {
            traveler = new Traveler();
        }

        public virtual void InitGame(MazeInfo maze)
        {
            //Set Goal Location
            TheMazeGrid = maze.Grid;
            Height = maze.Height;
            Width = maze.Width;
            PerfectGameMovesCount = maze.PerfectGameMovesCount;
            GoalLocation.X = maze.GoalPosition.X;
            GoalLocation.Y = maze.GoalPosition.Y;
            OldLocation.X = maze.StartingPosition.X;
            OldLocation.Y = maze.StartingPosition.Y;
            if (traveler != null)
            {
                traveler.location.X = maze.StartingPosition.X;
                traveler.location.Y = maze.StartingPosition.Y;
            }
        }

        public virtual void StartGame(Traveler SpecificTraveler = null, int currentIteration = 1, bool windowless = false)
        {
            //Set Default Traveler
            if (SpecificTraveler == null)
            {
                //Default traveler is human for now
                //traveler = new HumanTraveler(this);
            }
            else
            {
                traveler = SpecificTraveler;
            }

            //Initial Location
            traveler.location.X = OldLocation.X;
            traveler.location.Y = OldLocation.Y;

            //Fire GameStart Event
            if (GameStartEvent != null) GameStartEvent(traveler, currentIteration);

            Thread GameLoopThread = new Thread(() => { this.GameLoop(); });
            GameLoopThread.Start();
           
        }

        public void ResetGame()
        {

        }

        private void GameLoop()
        {
            int i = 0;
            //Cycle
            MazeCycleOutcome LastOutcome = new MazeCycleOutcome();
            while (LastOutcome.GameOver == false)
            {
                if (CancelGame) return;

                Int16 dir = -1;
                if (DirectionsStack.TryDequeue(out dir))
                {
                    //incriment the count
                    i++;
                    //Run the maze cycle
                    LastOutcome = CycleMaze(dir);

                    //GameCycledEvent
                    if (GameCycleEvent != null) GameCycleEvent(LastOutcome, traveler);

                    //Check for Game Over
                    if (LastOutcome.GameOver)
                    {
                        //Build Final Game
                        MazeGameFinalOutcome FinalOutcome = new MazeGameFinalOutcome();
                        FinalOutcome.CycleCount = i;
                        FinalOutcome.FinalScore = CalculateFinalScore(i);

                        //Event
                        if (GameOverEvent != null) GameOverEvent(FinalOutcome);
                        
                    }
                }
                else
                {
                    System.Threading.Thread.Sleep(10);
                }
            }
        }

        public virtual MazeCycleOutcome CycleMaze(int direction)
        {
            MazeCycleOutcome outcome = new MazeCycleOutcome();
            outcome.BumpedIntoWall = false;
            outcome.GameOver = false;
            outcome.FinalScore = 0;
            outcome.PreviousLocation = new Location() { X = traveler.location.X, Y = traveler.location.Y };

            TravelerLocation new_location = new TravelerLocation();
            outcome.Moves = Moves++;
            //Calculate new location
            switch (direction)
            {
                case 0: // up
                    if (traveler.location.Y <= 0)
                    {
                        //OutOfBoundsOccurred();
                        BumpIntoWall = true;
                        outcome.BumpedIntoWall = true;
                        return outcome;
                    }
                    new_location.X = traveler.location.X;
                    new_location.Y = traveler.location.Y - 1;
                    break;
                case 1: // right
                    if (traveler.location.X >= Width - 1)
                    {
                        //OutOfBoundsOccurred();
                        BumpIntoWall = true;
                        outcome.BumpedIntoWall = true;
                        return outcome;
                    }
                    new_location.X = traveler.location.X + 1;
                    new_location.Y = traveler.location.Y;
                    break;
                case 2: // down
                    if (traveler.location.Y >= Height - 1)
                    {
                        //OutOfBoundsOccurred();
                        BumpIntoWall = true;
                        outcome.BumpedIntoWall = true;
                        return outcome;
                    }
                    new_location.X = traveler.location.X;
                    new_location.Y = traveler.location.Y + 1;
                    break;
                case 3: // left
                    if (traveler.location.X <= 0)
                    {
                        //OutOfBoundsOccurred();
                        BumpIntoWall = true;
                        outcome.BumpedIntoWall = true;
                        return outcome;
                    }
                    new_location.X = traveler.location.X - 1;
                    new_location.Y = traveler.location.Y;
                    break;
                default:
                    throw new Exception("Not valid input");
            }


            //Is BumpedIntoWall?
            if (BumpedIntoObject(new_location))
            {
                BumpIntoWall = true;
                outcome.BumpedIntoWall = true;
                outcome.FinalScore = 0;
                outcome.GameOver = false;
                //Play sound
                //SystemSounds.Hand.Play();
                return outcome;
            }


            //New location is now current location
            //TravelerLocation old_location = traveler.location;
            OldLocation = traveler.location;
            traveler.location = new_location;

            //Is GameOver?
            if (IsGameOver())
            {
                outcome.GameOver = true;
                return outcome;
            }

            return outcome;
        }

        public virtual bool BumpedIntoObject(TravelerLocation location)
        {
            return this.TheMazeGrid[location.X, location.Y];
        }

        public virtual bool IsGameOver()
        {
            return traveler.location.X == GoalLocation.X && traveler.location.Y == GoalLocation.Y;
        }

        public virtual double CalculateFinalScore(Int32 i)
        {
            double val;
            try
            {
                checked
                {
                    val = (20000D + (PerfectGameMovesCount * 1000D)) - (Convert.ToDouble(i) * 1000D);
                }
            }
            catch (OverflowException e)
            {
                Console.WriteLine("Error: " + e.Message);
                val = double.MinValue;
            }
            return val;
        }
    }
}

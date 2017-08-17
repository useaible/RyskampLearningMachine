using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MazeGameLib
{
    public interface IMazeGame
    {
        ConcurrentQueue<Int16> DirectionsStack { get; set; }
        Boolean CancelGame { get; set; }
        Traveler traveler { get; set; }
        MazeCursorState cursorstate { get; set; }
        TravelerLocation GoalLocation { get; set; }
        TravelerLocation OldLocation { get; set; }
        int Width { get; set; }
        int Height { get; set; }
        bool BumpIntoWall { get; set; }
        int Moves { get; set; }

        event GameStartedOccurred_Delegate GameStartEvent;
        event GameCycleCompleteOccurred_Delegate GameCycleEvent;
        event GameOverOccurred_Delegate GameOverEvent;
        
        void InitGame(MazeInfo maze);
        void StartGame(Traveler SpecificTraveler = null, int currentIteration = 1, bool windowless = false);
        void ResetGame();
        MazeCycleOutcome CycleMaze(int direction);
        bool BumpedIntoObject(TravelerLocation location);
        bool IsGameOver();
        double CalculateFinalScore(Int32 i);
    }
}

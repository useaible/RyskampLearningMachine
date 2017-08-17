using ChallengerLib.Models;
using MazeGameLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChallengerLib
{
    public class ChallengerSimulator : MazeGame, IMazeGame
    {
        private SimulationConfig config;

        //public bool BumpedObject { get; set; } = false;
        public Block[,] SimulationArea { get; private set; }
        public double AccumulatedScore { get; private set; }
        
        public void InitGame(SimulationConfig config)
        {
            this.config = config;

            //Width = config.Width;
            //Height = config.Height;
            Height = config.Width;
            Width = config.Height;
            SimulationArea = config.Blocks;
            if (traveler != null)
            {
                traveler.location.X = config.StartingLocation.X;
                traveler.location.Y = config.StartingLocation.Y;
            }
        }

        //public override MazeCycleOutcome CycleMaze(int direction)
        //{
        //    BumpedObject = false;
        //    return base.CycleMaze(direction);
        //}

        public override bool IsGameOver()
        {
            Block block = SimulationArea[traveler.location.X, traveler.location.Y];
            return (block != null && block.IsEndSimulation);// || Moves >= config.MaximumMoves;
        }

        public override bool BumpedIntoObject(TravelerLocation location)
        {
            bool retVal = false;

            Block block = SimulationArea[location.X, location.Y];
            if (block != null)
            {
                //BumpedObject = true;
                traveler.location = location;
                if (!block.IsEndSimulation)
                {
                    retVal = true;
                    AccumulatedScore += block.Score;
                }
            }
            
            return retVal;
        }

        public override double CalculateFinalScore(int i)
        {
            double movesDeduction = 0.0;
            if (Moves > config.FreeMoves)
            {
                movesDeduction = (Moves - config.FreeMoves) * (config.MoveFactor / 100.0);
            }
            return AccumulatedScore - movesDeduction;
        }
    }
}

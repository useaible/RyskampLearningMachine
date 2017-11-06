using MazeGameLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChallengerLib.Models
{
    public class SimulationConfig
    {
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public Location StartingLocation { get; set; }
        public List<Block> BlockTemplates { get; set; } = new List<Block>();
        public Block[,] Blocks { get; set; }
        public int FreeMoves { get; set; } = 10;
        public double MoveFactor { get; set; } = 100;
        //public double MaximumMoves { get; set; } = 100;
    }
}

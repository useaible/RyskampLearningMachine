using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChallengerLib.Models
{
    public class Block : BlockTemplate
    {
        public Block() { }
        public Block(BlockTemplate template)
        {
            ID = template.ID;
            Name = template.Name;
            Icon = template.Icon;
            IsEndSimulation = template.IsEndSimulation;
        }
        public Block(Block blockCopy)
        {
            ID = blockCopy.ID;
            Name = blockCopy.Name;
            Icon = blockCopy.Icon;
            IsEndSimulation = blockCopy.IsEndSimulation;
            Score = blockCopy.Score;
            X = blockCopy.X;
            Y = blockCopy.Y;
        }

        public int X { get; set; }
        public int Y { get; set; }
        public double Score { get; set; }
        public string BlockID { get; set; }
    }
}

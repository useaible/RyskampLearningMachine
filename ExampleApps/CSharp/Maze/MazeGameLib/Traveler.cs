using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MazeGameLib
{
    public class Traveler
    {

        public MazeGame GameRef;
        public TravelerLocation location;

        public Traveler()
        {
            location = new TravelerLocation();
        }

        public Traveler(MazeGame gameref)
            : this()
        {
            GameRef = gameref;
        }
    }
}

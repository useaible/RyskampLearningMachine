using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MazeGameLib
{
    public class Traveler
    {

        public IMazeGame GameRef;
        public TravelerLocation location;

        public Traveler()
        {
            location = new TravelerLocation();
        }

        public Traveler(IMazeGame gameref)
            : this()
        {
            GameRef = gameref;
        }
    }
}

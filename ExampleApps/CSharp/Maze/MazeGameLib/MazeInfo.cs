using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MazeGameLib
{
    public class MazeInfo
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public Location StartingPosition { get; set; }
        public Location GoalPosition { get; set; }
        public short PerfectGameMovesCount { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public Boolean[,] Grid { get; set; }

        public string Metadata
        {
            get
            {
                string retVal = JsonConvert.SerializeObject(new { StartingPosition, GoalPosition, PerfectGameMovesCount, Grid, Height, Width });
                return retVal;
            }
            set
            {
                string val = value;
                if (val != null)
                {
                    MazeInfo temp = JsonConvert.DeserializeObject<MazeInfo>(val);
                    if (temp != null)
                    {
                        StartingPosition = temp.StartingPosition;
                        GoalPosition = temp.GoalPosition;
                        PerfectGameMovesCount = temp.PerfectGameMovesCount;
                        Grid = temp.Grid;
                        Width = temp.Width;
                        Height = temp.Height;
                    }
                }
            }
        }
    }
}

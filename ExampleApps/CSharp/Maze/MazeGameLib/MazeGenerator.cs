using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MazeGameLib
{
    public struct Location
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class MazeGenerator
    {
        //use for solving the maze
        public List<Location> solutionPath = new List<Location>();
        public Location GoalLocation;
        public Location StartingPosition;
        public short PerfectGameMovesCount;

        private Random random;
        public Boolean[,] TheMazeGrid;
        private const int sleepPeriod = 1000;
        public MazeGenerator()
        {
            GoalLocation = new Location();
            StartingPosition = new Location();
        }

        public void Generate(int width, int height)
        {
            TheMazeGrid = new Boolean[width, height];
            Stack<Location> stack = new Stack<Location>();
            int totalCells = (width * height) / 4;
            random = new Random();
            int visitedCells = 1;


            for (int x1 = 0; x1 <= TheMazeGrid.GetUpperBound(0); x1++)
            {
                for (int y1 = 0; y1 <= TheMazeGrid.GetUpperBound(1); y1++)
                {
                    TheMazeGrid[x1, y1] = true; //make everything a wall
                }
            }

            Location currentLocation = new Location() { X = random.Next(width), Y = random.Next(height) }; //get a random cell as a starting position
            TheMazeGrid[currentLocation.X, currentLocation.Y] = false; //knockdown the first wall

            while (visitedCells < totalCells)
            {
                var neighbours = getNeighbours(TheMazeGrid, currentLocation, width, height);

                if (neighbours.Count > 0)
                {
                    Location temp = neighbours[random.Next(neighbours.Count)];
                    stack.Push(currentLocation);

                    removeBlock(TheMazeGrid, ref currentLocation, ref temp);

                    currentLocation = temp; //update the current location

                    visitedCells++;
                }
                else
                {

                    if (stack.Count != 0)
                        currentLocation = stack.Pop();
                    else
                        break;
                }

                Thread.SpinWait(sleepPeriod);
            }

            //todo: set the starting position and the goal.

            if (TheMazeGrid[0, height - 1])
            {
                int x = 0;
                int y = height - 1;

                while (TheMazeGrid[x, y])
                {
                    x++;
                    if (x == width - 1)
                    {
                        x = 0;
                        y--;
                    }
                }

                StartingPosition.X = x;
                StartingPosition.Y = y;
            }
            else
            {
                StartingPosition.X = 0;
                StartingPosition.Y = height - 1;
            }

            if (TheMazeGrid[width - 1, 0])
            {
                int x = width - 1;
                int y = 0;

                while (TheMazeGrid[x, y])
                {
                    y++;
                    if (y == height - 1)
                    {
                        y = 0;
                        x--;
                    }
                }

                GoalLocation.X = x;
                GoalLocation.Y = y;
            }
            else
            {
                GoalLocation.X = width - 1;
                GoalLocation.Y = 0;
            }
        }

        private void removeBlock(Boolean[,] maze, ref Location current, ref Location next)
        {
            if (current.X == next.X && current.Y > next.Y)
            {
                maze[current.X, current.Y] = false;
                maze[current.X, next.Y + 1] = false; //the wall between them which is a cell
                maze[next.X, next.Y] = false;
            }

            else if (current.X == next.X && current.Y < next.Y)
            {
                maze[current.X, current.Y] = false;
                maze[current.X, current.Y + 1] = false; //the wall between them which is a cell
                maze[next.X, next.Y] = false;
            }

            else if (current.Y == next.Y && current.X > next.X)
            {
                maze[current.X, current.Y] = false;
                maze[next.X + 1, current.Y] = false; //the wall between them which is a cell
                maze[next.X, next.Y] = false;
            }
            else
            {
                maze[current.X, current.Y] = false;
                maze[current.X + 1, current.Y] = false;
                maze[next.X, next.Y] = false;
            }
        }

        private List<Location> getNeighbours(Boolean[,] arr, Location cell, int width, int height)
        {

            Location temp = cell;
            List<Location> availablePlaces = new List<Location>();

            // Left
            temp.X = cell.X - 2;
            if (temp.X >= 0 && arr[temp.X, temp.Y])
            {
                availablePlaces.Add(temp);
            }
            // Right
            temp.X = cell.X + 2;
            if (temp.X < width && arr[temp.X, temp.Y])
            {
                availablePlaces.Add(temp);
            }

            // Up
            temp.X = cell.X;
            temp.Y = cell.Y - 2;
            if (temp.Y >= 0 && arr[temp.X, temp.Y])
            {
                availablePlaces.Add(temp);
            }
            // Down
            temp.Y = cell.Y + 2;
            if (temp.Y < height && arr[temp.X, temp.Y])
            {
                availablePlaces.Add(temp);
            }

            return availablePlaces;
        }
        //solve the maze and get the perfect move count
        public void Solve()
        {
            solutionPath.Clear();
            solveMaze(StartingPosition.X, StartingPosition.Y, -1);
            PerfectGameMovesCount = short.Parse((solutionPath.Count() - 1).ToString());
        }
        //solve the maze using recursive
        private bool solveMaze(int x, int y, int d)
        {
            bool ok = false;

            for (int i = 0; i < 4 && !ok; i++)
            {
                if (i != d)
                {
                    switch (i)
                    {
                        case 0: //up
                            if (y > 0)
                            {

                                if (TheMazeGrid[x, y - 1] == false)
                                    ok = solveMaze(x, y - 2, 2);
                            }
                            break;
                        case 1: //right
                            if (x < TheMazeGrid.GetUpperBound(0))
                            {
                                if (TheMazeGrid[x + 1, y] == false)
                                    ok = solveMaze(x + 2, y, 3);
                            }
                            break;
                        case 2: //down
                            if (y < TheMazeGrid.GetUpperBound(1))
                            {
                                if (TheMazeGrid[x, y + 1] == false)
                                    ok = solveMaze(x, y + 2, 0);
                            }
                            break;
                        case 3://left
                            if (x > 0)
                            {
                                if (TheMazeGrid[x - 1, y] == false)
                                    ok = solveMaze(x - 2, y, 1);
                            }
                            break;
                    }
                }

                if (x == GoalLocation.X && GoalLocation.Y == y)
                {
                    //found the goal
                    ok = true;
                }

                if (ok)
                {
                    Location path = new Location();
                    path.X = x;
                    path.Y = y;
                    solutionPath.Add(path);
                    switch (d)
                    {
                        case 0:
                            path.Y = y - 1;
                            solutionPath.Add(path);
                            break;
                        case 1:
                            path.X = x + 1;
                            solutionPath.Add(path);
                            break;
                        case 2:
                            path.Y = y + 1;
                            solutionPath.Add(path);
                            break;
                        case 3:
                            path.X = x - 1;
                            solutionPath.Add(path);
                            break;
                    }
                }

            }

            return ok;
        }

    }
}

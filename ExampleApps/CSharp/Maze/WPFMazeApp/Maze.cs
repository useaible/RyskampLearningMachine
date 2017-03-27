using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Shapes;

namespace WPFMazeApp
{
    public class Maze
    {
        MazeGameLib.MazeGame ParentGame;
        System.Windows.Controls.Grid grid;
        //Build Grid
        //public Boolean[,] TheMazeGrid = new Boolean[50, 50];

        public void InitializeMaze(MazeGameLib.MazeGame parentgame, System.Windows.Controls.Grid grd)
        {
            ParentGame = parentgame;
            grid = grd;
   
            //Setup the UI Grid
            for (int x = 0; x < ParentGame.Width; x++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());
            }
            for (int y = 0; y < parentgame.Height; y++)
            {
                grid.RowDefinitions.Add(new RowDefinition());
            }

            ////Default
            //for(int x1=0; x1<=parentgame.TheMazeGrid.GetUpperBound(0); x1++)
            //{
            //    for(int y1=0; y1<=parentgame.TheMazeGrid.GetUpperBound(1);y1++)
            //    {
            //        parentgame.TheMazeGrid[x1,y1] = false;
            //    }
            //}

            ////Apply blocks for maze

            ////One long line across X at Y=125 except for 50, 100, 150, 200, 
            //for(int y2=0; y2<= parentgame.TheMazeGrid.GetUpperBound(0); y2++)
            //{
            //    if( y2<=23 || y2>=25 ) parentgame.TheMazeGrid[25, y2] = true;
            //}

            //The 
            //Default
            for (int x3 = 0; x3<= parentgame.TheMazeGrid.GetUpperBound(0); x3++)
            {
                for (int y3 = 0; y3 <= parentgame.TheMazeGrid.GetUpperBound(1); y3++)
                {
                    Rectangle  rect = new System.Windows.Shapes.Rectangle();
                    //rect.Stroke = System.Windows.Media.Brushes.Black;
                    if(parentgame.TheMazeGrid[x3,y3])
                        rect.Fill = System.Windows.Media.Brushes.Black;
                    else
                        rect.Fill = System.Windows.Media.Brushes.White;

                    rect.HorizontalAlignment = HorizontalAlignment.Center;
                    rect.VerticalAlignment = VerticalAlignment.Center;
                    rect.Height = 10;
                    rect.Width = 10;
                    System.Windows.Controls.Grid.SetRow(rect, y3);
                    System.Windows.Controls.Grid.SetColumn(rect, x3);
                    grd.Children.Add(rect);
                }
            }

            //The 
            //Goal
           
            Rectangle GoalRect =  grid.Children
                     .Cast<Rectangle>()
                     .First(e => Grid.GetRow(e) == ParentGame.GoalLocation.Y && Grid.GetColumn(e) == ParentGame.GoalLocation.X);
            GoalRect.Fill = System.Windows.Media.Brushes.Green;
        }

        public void ChangeCellColor(MazeGameLib.TravelerLocation loc, Boolean Darken=true)
        {
            Rectangle rect;

            try
            {
                var steelblues = grid.Children.Cast<Rectangle>().Where(x => x.Fill == System.Windows.Media.Brushes.SteelBlue);
                foreach(var blue in steelblues)
                {
                    blue.Fill = System.Windows.Media.Brushes.White;
                }
                rect = grid.Children
                     .Cast<Rectangle>()
                     .First(e => Grid.GetRow(e) == loc.Y && Grid.GetColumn(e) == loc.X);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return;
            }

            if(Darken)
            {
                rect.Fill = System.Windows.Media.Brushes.SteelBlue;
                ParentGame.cursorstate.IsDarkened = true;
            }
            else
            {
                rect.Fill = System.Windows.Media.Brushes.White;
                ParentGame.cursorstate.IsDarkened = false;
            }
        }
    }
}

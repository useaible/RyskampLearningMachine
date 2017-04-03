using MazeGameLib;
using RLM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WPFMazeApp
{
    /// <summary>
    /// Interaction logic for MazeCreator.xaml
    /// </summary>
    public partial class MazeCreator : Window
    {

        private MazeGenerator generator;
        private int MazeId;
        private int width = 50;
        private int height = 50;
        private int pen = 0; //default
        private bool editMode;
        private bool generated;
        private string mazeName;
        public MazeCreator(int mazeId = -1)
        {
            InitializeComponent();
         
            var repo = new MazeRepo();

            if (mazeId != -1) //we are editing the maze
            {
                generated = false;
                MazeId = mazeId;
                editMode = true;
                
                ResetGrid();
                var mazeToEdit = repo.GetByID(mazeId);
                width = mazeToEdit.Width;
                height = mazeToEdit.Height;
                generator.GoalLocation = mazeToEdit.GoalPosition;
                generator.StartingPosition = mazeToEdit.StartingPosition;
                generator.TheMazeGrid = mazeToEdit.Grid;
                generator.PerfectGameMovesCount = mazeToEdit.PerfectGameMovesCount;

                mazeName = mazeToEdit.Name;
                txtBoxHeight.Text = height.ToString();
                txtBoxWidth.Text = width.ToString();
                txtBoxMazeName.Text = mazeToEdit.Name;
                lblMoveCount.Content = $"Perfect Move Count: {mazeToEdit.PerfectGameMovesCount}";
                this.Title = $"Maze Designer Editing {mazeToEdit.Name}";

                initializeGrid(mazeGrid, width, height);
                generateMaze(mazeGrid);
            }
            else
            {
                editMode = false;
                ResetGrid();
                //initialize the grid with all the blocks
                initializeGrid(mazeGrid, width, height);
                //build the maze
                generator.Generate(width, height);
                //Generate the maze in the UI
                generateMaze(mazeGrid);
                int mazeCount = repo.Count();
                txtBoxMazeName.Text = $"New Maze({mazeCount + 1})";
                generated = true;
            }
          
           
        
        }

        private void initializeGrid(Grid grid, int width, int height)
        {
            
            //Setup the UI Grid
            for (int x = 0; x < width; x++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());
            }
            for (int y = 0; y < height; y++)
            {
                grid.RowDefinitions.Add(new RowDefinition());
               
            }

            //The 
            //Default
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Rectangle rect = new System.Windows.Shapes.Rectangle();
                    //rect.Stroke = System.Windows.Media.Brushes.Black;                   
                    rect.Fill = System.Windows.Media.Brushes.Black;
                    rect.HorizontalAlignment = HorizontalAlignment.Center;
                    rect.VerticalAlignment = VerticalAlignment.Center;
                    rect.Height = 10;
                    rect.Width = 10;
                    System.Windows.Controls.Grid.SetRow(rect, y);
                    System.Windows.Controls.Grid.SetColumn(rect, x);
                    grid.Children.Add(rect);
                }
            }

        }
        private void mazeGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //if (e.ClickCount == 2) // for double-click, remove this condition if only want single click
            //{
                var point = Mouse.GetPosition(mazeGrid);

                int row = 0;
                int col = 0;
                double accumulatedHeight = 0.0;
                double accumulatedWidth = 0.0;

                // calc row mouse was over
                foreach (var rowDefinition in mazeGrid.RowDefinitions)
                {
                    accumulatedHeight += rowDefinition.ActualHeight;
                    if (accumulatedHeight >= point.Y)
                        break;
                    row++;
                }

                // calc col mouse was over
                foreach (var columnDefinition in mazeGrid.ColumnDefinitions)
                {
                    accumulatedWidth += columnDefinition.ActualWidth;
                    if (accumulatedWidth >= point.X)
                        break;
                    col++;
                }

                Mouse.OverrideCursor = Cursors.Arrow;
                // row and col now correspond Grid's RowDefinition and ColumnDefinition mouse was 
            if(pen == 1 && !generator.TheMazeGrid[col, row]) //pen is starting position
            {
                if (generator.StartingPosition.X != -1 && generator.StartingPosition.Y != -1)
                    ResetCell(generator.StartingPosition.X, generator.StartingPosition.Y);

                generator.StartingPosition.X = col;
                generator.StartingPosition.Y = row;
                SetStartingPosition(col, row);
            }

            if(pen == 2 && !generator.TheMazeGrid[col, row]) //pen is goal
            {
                if (generator.GoalLocation.X != -1 && generator.GoalLocation.Y != -1)
                    ResetCell(generator.GoalLocation.X, generator.GoalLocation.Y);

                generator.GoalLocation.X = col;
                generator.GoalLocation.Y = row;
                SetGoalPosition(col, row);
            }
            //}
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            
            Regex regex = new Regex("^[0-9]*[02468]$");
            bool widthMatch = regex.IsMatch(txtBoxWidth.Text);
            bool heightMatch = regex.IsMatch(txtBoxHeight.Text);

            if (widthMatch && heightMatch)
            {
                width = int.Parse(txtBoxWidth.Text);
                height = int.Parse(txtBoxHeight.Text);
                if ((width >= 6 && width <= 50) && (height >= 6 && height <= 50))
                {
                    ResetGrid();
                    //initialize the grid with all the blocks
                    initializeGrid(mazeGrid, width, height);
                    //build the maze
                    generator.Generate(width, height);
                    //Generate the maze in the UI
                    generateMaze(mazeGrid);
                    generated = true;

                    if(editMode)
                    {
                        //RlmUtils.ResetTrainingData(mazeName);
                    }
                }
                else
                {
                    MessageBox.Show("The maximum and minimum dimension of the maze is 50 and 6.");
                }
            }
            else
            {
                MessageBox.Show("Dimension of the maze should be in even numbers only and no spaces.");
            }

            if(generated)
            {
                txtBoxHeight.IsEnabled = false;
                txtBoxWidth.IsEnabled = false;
            }
        }

        private void generateMaze(Grid grid)
        {
            for (int x3 = 0; x3 <= generator.TheMazeGrid.GetUpperBound(0); x3++)
            {
                for (int y3 = 0; y3 <= generator.TheMazeGrid.GetUpperBound(1); y3++)
                {                 
                    if (!generator.TheMazeGrid[x3, y3])
                    {
                        System.Diagnostics.Debug.WriteLine($"x: {x3}, y: {y3}");
                        var child = mazeGrid.Children.Cast<Rectangle>()
                        .First(e => Grid.GetRow(e) == y3 && Grid.GetColumn(e) == x3);
                        child.Fill = System.Windows.Media.Brushes.White;

                    }
                    
                }
            }

            SetGoalPosition(generator.GoalLocation.X, generator.GoalLocation.Y);
            SetStartingPosition(generator.StartingPosition.X, generator.StartingPosition.Y);

            generator.Solve();
            lblMoveCount.Content = $"Perfect Move Count: {generator.PerfectGameMovesCount}";
            ShowSolution();
           

        }

        private void SetStartingPosition(int x, int y)
        {
            var child = mazeGrid.Children.Cast<Rectangle>()
                     .First(e => Grid.GetRow(e) == y && Grid.GetColumn(e) == x);
            child.Fill = System.Windows.Media.Brushes.Orange;                 
            pen = 0;
            
        }

        private void ResetCell(int x, int y)
        {
            Rectangle GoalRect = mazeGrid.Children
                     .Cast<Rectangle>()
                     .First(e => Grid.GetRow(e) == y && Grid.GetColumn(e) == x);
            GoalRect.Fill = System.Windows.Media.Brushes.White;
        }

        private void SetGoalPosition(int x, int y)
        {
            var child = mazeGrid.Children.Cast<Rectangle>()
                     .First(e => Grid.GetRow(e) == y && Grid.GetColumn(e) == x);
            child.Fill = System.Windows.Media.Brushes.Green;
            pen = 0;
        }

        private void ShowSolution()
        {
            foreach(var loc in generator.solutionPath)
            {
                var child = mazeGrid.Children.Cast<Rectangle>()
                     .First(e => Grid.GetRow(e) == loc.Y && Grid.GetColumn(e) == loc.X);
                child.Fill = System.Windows.Media.Brushes.DarkOrange;
            }
        }

        private void btnSetStart_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Pen;
            pen = 1;
        }

        private void btnSetEnd_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Pen;
            pen = 2;
        }

        private void btnSaveMaze_Click(object sender, RoutedEventArgs e)
        {
            var repo = new MazeRepo();

            // check name for duplicate
            if (repo.HasDuplicate(txtBoxMazeName.Text) && !editMode)
            {
                MessageBox.Show($"'{txtBoxMazeName.Text}' maze name already exist.", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            else if (String.IsNullOrWhiteSpace(txtBoxMazeName.Text))
            {
                MessageBox.Show("Please provide a name for the maze.");
                return;
            }

            var maze = new MazeInfo()
            {
                Name = txtBoxMazeName.Text,
                StartingPosition = generator.StartingPosition,
                GoalPosition = generator.GoalLocation,
                PerfectGameMovesCount = generator.PerfectGameMovesCount,
                Grid = generator.TheMazeGrid,
                Width = width,
                Height = height
            };

            if (editMode)
                repo.UpdateMaze(MazeId, maze);
            else
                repo.CreateMaze(maze);
            //var newMaze = repo.CreateMaze(maze);

            MessageBox.Show("Maze successfully saved!");
            this.Close();


            //try get by id
            //var mazefromdb = repo.GetByID(newMaze.ID);

            // try to update maze
            //newMaze.PerfectScore = 100;
            //repo.UpdateMaze(newMaze.ID, newMaze);

            // try to get ID Name dic
            //var result = repo.GetIDNameDictionary();
        }

        private void ResetGrid()
        {
            //ShowGridLines="false"  Margin="0,0,0,9" HorizontalAlignment="Center" VerticalAlignment="Center" PreviewMouseLeftButtonDown="mazeGrid_PreviewMouseLeftButtonDown"
            mazeGrid.Children.Clear();
            mazeGrid.PreviewMouseLeftButtonDown -= mazeGrid_PreviewMouseLeftButtonDown;
            gridBorder.Child = mazeGrid = null;

            mazeGrid = new Grid();
            mazeGrid.PreviewMouseLeftButtonDown += mazeGrid_PreviewMouseLeftButtonDown;
            mazeGrid.ShowGridLines = false;
            mazeGrid.HorizontalAlignment = HorizontalAlignment.Center;
            mazeGrid.VerticalAlignment = VerticalAlignment.Center;
            gridBorder.Child = mazeGrid;

            generator = null;
            generator = new MazeGenerator();
        }

        //we only allow numbers
        private void txtBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            
            Regex regex = new Regex("[^0-9]+$");
            e.Handled = regex.IsMatch(e.Text);
            
        }

    }
}

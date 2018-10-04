using Challenger.Models;
using ChallengerLib.Data;
using ChallengerLib.Models;
using ChallengerLib.RLM;
using MazeGameLib;
using Microsoft.Win32;
using Newtonsoft.Json;
using RLV.Core;
using RLV.Core.Interfaces;
using RLV.Core.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using ToastNotifications;
using ToastNotifications.Position;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using RLM.Models.Interfaces;
using RLM.SQLServer;
using RLM.PostgreSQLServer;

namespace Challenger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    //public partial class MainWindow : Window
    public partial class MainWindow
    {
        private int previousHeight = 0;
        private int previousWidth = 0;
        private string draggedFromName = "";
        private bool allowCopy = true;
        private Point anchorPoint;
        private Point currentPoint;
        private bool dragging;
        private TranslateTransform transform = new TranslateTransform();
        private int? previousRow = null;
        private int? previousCol = null;
        private Point draggedPoint;
        private CancellationTokenSource tokenSrc = new CancellationTokenSource();
        private Random rand = new Random();
        private Rectangle lastRectangle;
        private Brush lastBrush;
        private SolidColorBrush brush = new SolidColorBrush(Colors.LightGray);
        private ImageBrush playerBrush = new ImageBrush();
        private Rectangle[,] rectangleContainer;
        private Rectangle startingPoint;
        private bool isPlaying = false;

        private VisualizerWindow rlvPanel;
        //private RLVOutputVisualizer visualizer;
        private RLVCore core;
        private IRlmDbData rlmDbData;
        
        private SimulationConfig config = new SimulationConfig();
        private ChallengerSimulationSettings simSettings = new ChallengerSimulationSettings()
        {

            DefaultScorePercentage = 100,
            SimType = PoCTools.Settings.SimulationType.Score,
            Score = 100
        };
        private RLMChallenger challenger;
        private string dbIdentifier;
        private Notifier notifier = new Notifier(cfg =>
        {
            cfg.PositionProvider = new WindowPositionProvider(
                parentWindow: Application.Current.MainWindow,
                corner: Corner.TopRight,
                offsetX: 10,
                offsetY: 10);

            cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                notificationLifetime: TimeSpan.FromSeconds(3),
                maximumNotificationCount: MaximumNotificationCount.FromCount(5));

            cfg.Dispatcher = Application.Current.Dispatcher;
        });

        public Notifier Notification { get { return notifier; } }

        public MainWindow()
        {
            InitializeComponent();
            
            //loads initialization panel on mainWindow
            //this.InitializationPanel.Content = new InitializationPanel();

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;

            txtSessionScore.Text = string.Empty;
            txtStatus.Text = string.Empty;
            txtSessionMoves.Text = string.Empty;
            txtTopScore.Text = string.Empty;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDefaultConfiguration();

            string dbIdentifier = $"RLM_Challenger_{Guid.NewGuid().ToString("N")}";

            simSettings.DBIdentifier = dbIdentifier;

            //core = new RLVCore(dbIdentifier);
            //visualizer = new RLVOutputVisualizer(this);
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            tokenSrc.Cancel();

            if (rlvPanel != null)
            {
                rlvPanel.Close();
            }
        }

        private void EnableConfigControls(bool enable)
        {
            gridConfigProperties.IsEnabled = enable;
            gridSimulationBlocks.IsEnabled = enable;
            gridTopControls.IsEnabled = enable;
            btnVisualizer.IsEnabled = enable;
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (!HasStartSimObject())
            {
                notifier.ShowError("Please add \"START\" object.");
                return;
            }

            if (!HasEndSimObject())
            {
                notifier.ShowError("Please add at least 1 \"END or STOP\" object.");
                return;
            }

            if (isPlaying)
            {
                isPlaying = false;
                tokenSrc.Cancel();
                EnableConfigControls(true);
                btnPlay.Content = "Play";
                txtStatus.Text = "Stopped";
            }
            else
            {
                SimulationPanel panel = new SimulationPanel();
                panel.SetSimSettings(simSettings);
                if (panel.ShowDialog() == true)
                {
                    if (rlvPanel != null)
                        rlvPanel.Close();

                    rlvPanel = null;

                    // update player brush
                    // TODO change this to no longer use the ID to determine the type
                    var blockTemplate = config.BlockTemplates.FirstOrDefault(a => a.ID == 1);
                    playerBrush = new ImageBrush() { ImageSource = new BitmapImage(new Uri(blockTemplate.Icon, UriKind.Relative)) };

                    isPlaying = true;
                    tokenSrc = new CancellationTokenSource();
                    EnableConfigControls(false);
                    btnPlay.Content = "Stop";

                    simSettings.SimType = panel.SimType;
                    simSettings.Score = panel.Score;
                    simSettings.Hours = panel.Hours;
                    simSettings.Sessions = panel.Sessions;
                    simSettings.EnableSimDisplay = panel.EnableSimDisplay;

                    dbIdentifier = $"RLM_Challenger_{Guid.NewGuid().ToString("N")}";

                    simSettings.DBIdentifier = dbIdentifier;

                  
                    //rlvPanel.Show();

                    //var startRect = rectangleContainer[config.StartingLocation.X, config.StartingLocation.Y];
                    //playerBrush = (ImageBrush)startRect.Fill;

                    challenger = new RLMChallenger(config, simSettings);

                    challenger.OptimizationStatus += (status) =>
                    {
                        if (tokenSrc.IsCancellationRequested) return;
                        Dispatcher.Invoke(() => 
                        {
                            txtStatus.Text = status;
                            if (status == "Done")
                            {
                                EnableConfigControls(true);
                                btnPlay.Content = "Play";
                                isPlaying = false;                                
                            }
                        });
                    };

                    double topScore = 0.0;
                    challenger.SessionComplete += (sessCnt, score, moves) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"Sessions: {sessCnt} Score: {score} Moves: {moves}");
                        if (tokenSrc.IsCancellationRequested) return;
                        Dispatcher.Invoke(() =>
                        {
                            txtSessionScore.Text = score.ToString("N");
                            txtSessionMoves.Text = moves.ToString();
                            if (score > topScore)
                                topScore = score;
                            txtTopScore.Text = topScore.ToString("N");
                        });
                    };

                    challenger.MazeCycleComplete += (x, y, bump) =>
                    {
                        //System.Diagnostics.Debug.Write($"x: {x} y: {y} bump: {bump} \r");
                        if (tokenSrc.IsCancellationRequested) return;
                        Dispatcher.Invoke(() =>
                        {
                            //var index = (x == 0 && y == 0) ? 0 : x * gridMain.ColumnDefinitions.Count + y - 1;
                            var rect = rectangleContainer[x, y];

                            //if (rect.Tag != null && ((ChallengerLib.Models.Block)rect.Tag).IsEndSimulation)
                            //{
                            //    return;
                            //}
                            //else 
                            if (lastRectangle != null && lastRectangle.Equals(rect))
                            {
                                return;
                            }

                            if (lastRectangle != null)
                            {
                                lastRectangle.Fill = lastBrush;
                            }

                            lastRectangle = rect;
                            lastBrush = lastRectangle.Fill;
                            rect.Fill = playerBrush;

                        });
                    };

                    //return;

                    Task.Run(() =>
                    {
                        challenger.Travel(100, tokenSrc.Token);
                    }, tokenSrc.Token);
                }            
            }
        }

        private void generateBlockTest()
        {
            //config.Name = "default";
            //config.Height = 10;
            //config.Width = 10;
            //config.StartingLocation = new Location() { X = 0, Y = 0 };
            //config.BlockTemplates.Add(new ChallengerLib.Models.Block() { ID = 1, BlockID = Guid.NewGuid().ToString("N"), Icon = System.IO.Path.Combine("Icons", "robot.png"), Name = "Start Simulation" });
            //config.BlockTemplates.Add(new ChallengerLib.Models.Block() { ID = 2, BlockID = Guid.NewGuid().ToString("N"), Icon = System.IO.Path.Combine("Icons", "finish.png"), Name = "End Simulation", IsEndSimulation = true });
            //config.BlockTemplates.Add(new ChallengerLib.Models.Block() { ID = 3, BlockID = Guid.NewGuid().ToString("N"), Icon = System.IO.Path.Combine("Icons", "money.png"), Name = "Basic", Score = 5 });
            //config.BlockTemplates.Add(new ChallengerLib.Models.Block() { ID = 3, BlockID = Guid.NewGuid().ToString("N"), Icon = System.IO.Path.Combine("Icons", "poison.png"), Name = "Basic", Score = -5 });

            //config.Blocks = new ChallengerLib.Models.Block[config.Height, config.Width];
            //for (int x = 0; x < config.Height; x++)
            //{
            //    for (int y = 0; y < config.Width; y++)
            //    {
            //        if (x == 0 && y == 0)
            //        {
            //            config.Blocks[x, y] = new ChallengerLib.Models.Block(config.BlockTemplates.ElementAt(0)) { X = x, Y = y };
            //        }
            //        else
            //        {
            //            if (x == config.Height - 1 && y == config.Width - 1)
            //            {
            //                config.Blocks[x, y] = new ChallengerLib.Models.Block(config.BlockTemplates.ElementAt(1)) { X = x, Y = y };
            //            }
            //            else
            //            {
            //                if (rand.Next(0, 100) < 10)
            //                {
            //                    if (rand.Next() % 2 == 0)
            //                    {
            //                        config.Blocks[x, y] = new ChallengerLib.Models.Block(config.BlockTemplates.ElementAt(2)) { X = x, Y = y };
            //                    }
            //                    else
            //                    {
            //                        config.Blocks[x, y] = new ChallengerLib.Models.Block(config.BlockTemplates.ElementAt(3)) { X = x, Y = y };
            //                    }
            //                }
            //                else
            //                {
            //                    config.Blocks[x, y] = null;
            //                }
            //            }
            //        }
            //    }
            //}

            //SaveConfiguration();
            LoadDefaultConfiguration();
        }

        private bool SaveConfiguration()
        {
            try
            {
                if (string.IsNullOrEmpty(config.Name))
                {
                    //MessageBox.Show("Configuration name must not be empty.", "Action Not Allowed", MessageBoxButton.OK, MessageBoxImage.Error);
                    notifier.ShowError("Configuration name must not be empty.");
                    return false;
                }

                string path = System.IO.Path.Combine("SimulationConfig", $"{config.Name}.json");
                FileStream file = File.Open(path, FileMode.Create);
                using (StreamWriter st = new StreamWriter(file))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.TypeNameHandling = TypeNameHandling.Auto;
                    serializer.Formatting = Formatting.Indented;
                    serializer.Serialize(st, config);
                }
                return true;
            }

            catch (Exception e)
            {
                return false;
                throw (e);
            }
        }

        private void LoadDefaultConfiguration()
        {
            LoadConfiguration("default.json");
        }

        private void ClearGrid()
        {
            gridMain.RowDefinitions.Clear();
            gridMain.ColumnDefinitions.Clear();
            gridMain.Children.Clear();
        }

        private void LoadConfiguration(string configName)
        {
            if (rlvPanel != null)
                rlvPanel.Close();

            rlvPanel = null;

            gridBlocks.Children.Clear();

            try
            {
                string path = System.IO.Path.Combine("SimulationConfig", $"{configName}");
                var valuesToStr = File.ReadAllText(path, Encoding.UTF8);
                config = JsonConvert.DeserializeObject<SimulationConfig>(valuesToStr, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

                LoadSimulationBlocks();

                txtName.Text = config.Name;
                txtWidth.Text = config.Width.ToString();
                txtHeight.Text = config.Height.ToString();
                txtFreeMoves.Text = config.FreeMoves.ToString();
                sliderMoveFactor.Value = config.MoveFactor;
                checkAllowCopy.IsChecked = allowCopy;
                previousHeight = config.Height;
                previousWidth = config.Width;

                LoadConfigurationBlocks();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("No configuration file found.");
            }
        }

        private void LoadSimulationBlocks()
        {
            gridBlocks.Children.Clear();
            List<ChallengerLib.Models.Block> blockTemplates = config.BlockTemplates;
            foreach (ChallengerLib.Models.Block tmpl in blockTemplates)
            {
                Border border = new Border();
                border.Margin = new Thickness(5);
                border.BorderBrush = new SolidColorBrush(Colors.Orange);
                border.BorderThickness = new Thickness(1);

                Grid grid = new Grid();
                grid.Margin = new Thickness(2);

                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(70) });
                grid.RowDefinitions.Add(new RowDefinition());

                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(70) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(30) });

                Rectangle rect = new Rectangle();
                rect.Height = 70;
                rect.Width = 70;
                rect.Tag = tmpl;
                rect.Fill = new ImageBrush { ImageSource = new BitmapImage(new Uri(tmpl.Icon, UriKind.Relative)) };
                rect.PreviewMouseLeftButtonDown += Rectangle_PreviewMouseLeftButtonDown;
                rect.PreviewMouseMove += Rectangle_PreviewMouseMove;
                rect.Cursor = Cursors.Hand;
                Grid.SetRow(rect, 0);
                Grid.SetColumn(rect, 0);
                
                TextBlock btnEdit = new TextBlock();
                btnEdit.Text = "Edit";
                btnEdit.FontSize = 8;
                btnEdit.HorizontalAlignment = HorizontalAlignment.Left;
                btnEdit.VerticalAlignment = VerticalAlignment.Top;
                btnEdit.Margin = new Thickness(10, 14, 0, 0);
                btnEdit.Tag = tmpl;
                btnEdit.FontFamily = new FontFamily("Fonts/#Oswald Light");
                btnEdit.PreviewMouseLeftButtonDown += BtnEdit_PreviewMouseLeftButtonDown;
                btnEdit.TextWrapping = TextWrapping.Wrap;
                btnEdit.Cursor = Cursors.Hand;
                Grid.SetRow(btnEdit, 0);
                Grid.SetColumn(btnEdit, 1);

                TextBlock btnDel = new TextBlock();
                btnDel.Text = "Delete";
                btnDel.FontSize = 8;
                btnDel.HorizontalAlignment = HorizontalAlignment.Left;
                btnDel.VerticalAlignment = VerticalAlignment.Top;
                btnDel.Margin = new Thickness(6, 43, 0, 0);
                btnDel.Tag = tmpl;
                btnDel.FontFamily = new FontFamily("Fonts/#Oswald Light");
                btnDel.PreviewMouseLeftButtonDown += BtnDel_PreviewMouseLeftButtonDown; ;
                btnDel.TextWrapping = TextWrapping.Wrap;
                btnDel.Cursor = Cursors.Hand;
                Grid.SetRow(btnDel, 0);
                Grid.SetColumn(btnDel, 1);

                TextBlock textName = new TextBlock();
                textName.Text = tmpl.Name;
                textName.VerticalAlignment = VerticalAlignment.Top;
                textName.HorizontalAlignment = HorizontalAlignment.Left;
                textName.FontFamily = new FontFamily("Fonts/#Oswald Light");
                textName.FontSize = 12;
                textName.Width = 100;
                textName.TextAlignment = TextAlignment.Center;
                textName.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFE8F41"));
                textName.Foreground = new SolidColorBrush(Colors.White);
                textName.Margin = new Thickness(0, 3, 0, 0);
                Grid.SetRow(textName, 1);
                Grid.SetColumnSpan(textName, 2);

                grid.Children.Add(rect);
                grid.Children.Add(btnEdit);
                grid.Children.Add(btnDel);
                grid.Children.Add(textName);

                border.Child = grid;

                gridBlocks.Children.Add(border);
            }
        }

        private void BtnDel_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (MessageBox.Show("Deleting this object will also removed all the references of this object from the entire grid. Continue?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                TextBlock delBtn = (TextBlock)sender;
                var toDelete = (ChallengerLib.Models.Block)delBtn.Tag;
                config.BlockTemplates.Remove(toDelete);
                DeleteAllFromGrid(toDelete.BlockID);
                LoadSimulationBlocks();
                SaveConfiguration();
                LoadConfigurationBlocks();
            }
        }

        private void BtnEdit_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            NewEditBoxWindow addBlockWindow = new NewEditBoxWindow(config.BlockTemplates.Select(a=>a.Name));
            addBlockWindow.SetEdit(((ChallengerLib.Models.Block)((TextBlock)sender).Tag));
            bool? result = addBlockWindow.ShowDialog();
            if (result.HasValue && result.Value == true)
            {
                if (addBlockWindow.NewBlock.Value)
                {
                    config.BlockTemplates.Add(addBlockWindow.CreatedBlock);
                }

                EditAllFromGrid(addBlockWindow.CreatedBlock);

                previousHeight = config.Height;
                previousWidth = config.Width;

                SaveConfiguration();
                LoadSimulationBlocks();
                LoadConfigurationBlocks();
            }
        }

        private void BtnDel_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Deleting this object will also removed all the references of this object from the entire grid. Continue?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                Button delBtn = (Button)sender;
                var toDelete = (ChallengerLib.Models.Block)delBtn.Tag;
                config.BlockTemplates.Remove(toDelete);
                DeleteAllFromGrid(toDelete.BlockID);
                SaveConfiguration();
                LoadSimulationBlocks();
                LoadConfigurationBlocks();
            }
        }

        private void DeleteAllFromGrid(string id)
        {
            for(int i = 0; i < config.Height; i++)
            {
                for (int j = 0; j < config.Width; j++)
                {
                    var toRem = config.Blocks[i, j];
                    if (toRem == null)
                        continue;
                    if (toRem.BlockID == id)
                    {
                        config.Blocks[i, j] = null;
                    }
                }
            }
        }

        private void EditAllFromGrid(ChallengerLib.Models.Block newBlock)
        {
            for (int i = 0; i < config.Height; i++)
            {
                for (int j = 0; j < config.Width; j++)
                {
                    var toRem = config.Blocks[i, j];
                    if (toRem == null)
                        continue;
                    if (toRem.BlockID == newBlock.BlockID)
                    {
                        config.Blocks[i, j].Score = newBlock.Score;
                        config.Blocks[i, j].Name = newBlock.Name;
                        config.Blocks[i, j].Icon = newBlock.Icon;
                    }
                }
            }
        }

        private bool HasEndSimObject()
        {
            bool hasEnd = false;
            for (int i = 0; i < config.Height; i++)
            {
                for (int j = 0; j < config.Width; j++)
                {
                    var toRem = config.Blocks[i, j];
                    if (toRem == null)
                        continue;
                    if (toRem.IsEndSimulation == true)
                    {
                        hasEnd = true;
                        return hasEnd;
                    }
                }
            }

            return hasEnd;
        }

        private bool HasStartSimObject()
        {
            bool hasStart = false;
            for (int i = 0; i < config.Height; i++)
            {
                for (int j = 0; j < config.Width; j++)
                {
                    var toRem = config.Blocks[i, j];
                    if (toRem == null)
                        continue;
                    if (toRem.ID == 1) //TODO: LATER THIS ID WILL NOT BE THE IDENTIFIER FOR THIS OBJECT.
                    {
                        hasStart = true;
                        return hasStart;
                    }
                }
            }

            return hasStart;
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            NewEditBoxWindow addBlockWindow = new NewEditBoxWindow(config.BlockTemplates.Select(a=>a.Name));
            addBlockWindow.SetEdit(((ChallengerLib.Models.Block)((Button)sender).Tag));
            bool? result = addBlockWindow.ShowDialog();
            if (result.HasValue && result.Value == true)
            {
                if (addBlockWindow.NewBlock.Value)
                {
                    config.BlockTemplates.Add(addBlockWindow.CreatedBlock);
                }

                EditAllFromGrid(addBlockWindow.CreatedBlock);

                SaveConfiguration();
                LoadSimulationBlocks();
                LoadConfigurationBlocks();
            }
        }

        private void LoadConfigurationBlocks()
        {
            int newHeight = Convert.ToInt32(txtHeight.Text);
            int newWidth = Convert.ToInt32(txtWidth.Text);

            bool resize = false;
            if (newHeight != previousHeight || newWidth != previousWidth)
            {
                if(MessageBox.Show("Resizing the Play Area will delete the current blocks on it. Continue?","", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    resize = true;
                    previousHeight = config.Height;
                    previousWidth = config.Width;
                }
            }

            if (resize)
            {
                ((IList)config.Blocks).Clear();
            }

            ClearGrid();
            ChallengerLib.Models.Block[,] blocks = config.Blocks;
            blocks = (ChallengerLib.Models.Block[,])Utilities.ResizeArray(blocks, new int[] { config.Height, config.Width });
            config.Blocks = blocks;

            for (int row = 0; row < config.Height; row++)
            {
                var rowDefinition = new RowDefinition();
                gridMain.RowDefinitions.Add(rowDefinition);
            }

            for (int col = 0; col < config.Width; col++)
            {
                var colDefinition = new ColumnDefinition();
                gridMain.ColumnDefinitions.Add(colDefinition);
            }

            rectangleContainer = new Rectangle[config.Height, config.Width];
            for (int row = 0; row < config.Height; row++)
            {
                for (int col = 0; col < config.Width; col++)
                {
                    var item = blocks[row, col];

                    TravelerLocation location = new TravelerLocation() { X = row, Y = col };
                    Rectangle gridRect = new Rectangle();
                    gridRect.Stroke = new SolidColorBrush(Colors.Gray);
                    gridRect.StrokeThickness = 0.1;
                    gridRect.Tag = item;
                    gridRect.AllowDrop = true;
                    gridRect.DragEnter += GridRect_DragEnter;
                    gridRect.DragLeave += GridRect_DragLeave;
                    gridRect.Drop += GridRect_Drop;
                    //gridRect.ToolTip = "Right click to delete.";
                    
                    if(item != null)
                    {
                        //gridRect.GiveFeedback += GridRect_GiveFeedback;
                        //gridRect.Cursor = Cursors.Hand;
                        gridRect.PreviewMouseLeftButtonDown += Rectangle_PreviewMouseLeftButtonDown;
                        gridRect.PreviewMouseLeftButtonUp += GridRect_PreviewMouseLeftButtonUp;
                        gridRect.PreviewMouseMove += Rectangle_PreviewMouseMove;
                        gridRect.MouseEnter += GridRect_MouseEnter;
                        gridRect.MouseLeave += GridRect_MouseLeave;

                        ContextMenu gridRectMenu = new ContextMenu();
                        gridRect.ContextMenu = gridRectMenu;

                        MenuItem gridRectMenuItem = new MenuItem();
                        gridRectMenuItem.Tag = gridRect;
                        gridRectMenuItem.Header = "Delete";
                        gridRectMenuItem.Click += GridRectMenuItem_Click;
                        gridRectMenu.Items.Add(gridRectMenuItem);
                    }

                    //gridRect.InputBindings.Add(new MouseBinding()
                    //{
                    //    Gesture = new MouseGesture(MouseAction.LeftClick),
                    //    Command = new ItemClickedCommand(() =>
                    //    {                            
                    //        OnSelectedItem(location, null);
                    //    })
                    //});

                    if (item == null)
                    {
                        gridRect.Fill = brush;
                    }
                    else
                    {
                        gridRect.Fill = new ImageBrush { ImageSource = new BitmapImage(new Uri(item.Icon, UriKind.Relative)) };
                    }

                    Grid.SetColumn(gridRect, col);
                    Grid.SetRow(gridRect, row);

                    gridMain.Children.Add(gridRect);
                    rectangleContainer[row, col] = gridRect;
                }
            }
        }

        private void GridRect_MouseLeave(object sender, MouseEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void GridRect_MouseEnter(object sender, MouseEventArgs e)
        {
            Rectangle s = (Rectangle)sender;
            s.Cursor = Cursors.Hand;
            e.Handled = true;
        }

        private void GridRect_DragLeave(object sender, DragEventArgs e)
        {
            Rectangle s = (Rectangle)sender;
            s.Stroke = new SolidColorBrush(Colors.LightGray);
            s.StrokeThickness = 0.1;
        }

        private void GridRect_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            e.Handled = true;
        }

        private void GridRectMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            Rectangle rect = (Rectangle)menuItem.Tag;
            int rectrow = Grid.GetRow(rect);
            int rectcol = Grid.GetColumn(rect);
            config.Blocks[rectrow, rectcol] = null;
            rect.ClearValue(Rectangle.FillProperty);
            SaveConfiguration();
            LoadConfigurationBlocks();
        }

        private void OnSelectedItem(object sender, MouseButtonEventArgs e)
        {
            ////if (dragging) return;

            //if (rlvPanel == null) return;

            //dragging = false;

            //if (rlvPanel != null && (rlvPanel.Visibility == Visibility.Collapsed || rlvPanel.Visibility == Visibility.Hidden))
            //{
            //    rlvPanel.Show();
            //}

            //var location = (TravelerLocation)sender;
            //System.Diagnostics.Debug.WriteLine($"x: {location.X} y: {location.Y}");

            //var inputValues = new List<IRLVIOValues>()
            //{
            //    //new RLVIOValues() { IOName = "X", Value = location.X.ToString() },
            //    //new RLVIOValues() { IOName = "Y", Value = location.Y.ToString() }
            //    new RLVIOValues() { IOName = "Move", Value = location.Y.ToString() }
            //};

            //var outputValues = new List<IRLVIOValues>()
            //{
            //    new RLVIOValues() { IOName = "Direction", Value = "1" }
            //};

            //visualizer.SelectNewItem(inputValues, outputValues, null);
        }

        private void GridRect_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                var element = sender as FrameworkElement;
                currentPoint = e.GetPosition(null);

                transform.X += currentPoint.X - anchorPoint.X;
                transform.Y += (currentPoint.Y - anchorPoint.Y);
                element.RenderTransform = transform;
                anchorPoint = currentPoint;
            }
        }

        private void GridRect_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element != null)
            {
                element.CaptureMouse();
                anchorPoint = e.GetPosition(null);
            }
            dragging = false;
            e.Handled = true;
        }

        private void GridRect_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!dragging) return;
            var element = sender as FrameworkElement;
            if (element != null)
                element.ReleaseMouseCapture();
            dragging = true;
            e.Handled = true;
        }

        private void GridRect_Drop(object sender, DragEventArgs e)
        {
            if (!dragging)
                return;

            bool failed = false;
            Rectangle rect = (Rectangle)sender;
            int currentRow = Grid.GetRow(rect);
            int currentCol = Grid.GetColumn(rect);
            ChallengerLib.Models.Block block = (ChallengerLib.Models.Block)e.Data.GetData(typeof(ChallengerLib.Models.Block));
            ChallengerLib.Models.Block newBlock = new ChallengerLib.Models.Block();
            newBlock.ID = block.ID;
            newBlock.Icon = block.Icon;
            newBlock.IsEndSimulation = block.IsEndSimulation;
            newBlock.Name = block.Name;
            newBlock.Score = block.Score;
            newBlock.X = block.X;
            newBlock.Y = block.Y;
            newBlock.BlockID = block.BlockID;

            //Robot or the starting location is moved,
            //should update the config's starting location as well.
            if (block.ID == 1) //TODO: ID WILL NOT BE USED AS IDENTIFIER LATER
            {
                if (draggedFromName != "gridMain")
                {
                    foreach (var a in config.Blocks)
                    {
                        if (a == null)
                            continue;

                        if (a.ID == 1)
                        {
                            //MessageBox.Show("You are not allowed to create more than 1 starting point.", "Action Not Allowed", MessageBoxButton.OK, MessageBoxImage.Error);
                            notifier.ShowError("You are not allowed to create more than 1 starting point.");
                            LoadConfigurationBlocks();
                            failed = true;
                            dragging = false;
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var a in config.Blocks)
                    {
                        if (a == null)
                            continue;

                        if (a.ID == 1 && allowCopy)
                        {
                            //MessageBox.Show("You are not allowed to create more than 1 starting point, please uncheck allow copy option above.", "Action Not Allowed", MessageBoxButton.OK, MessageBoxImage.Error);
                            notifier.ShowError("You are not allowed to create more than 1 starting point, please uncheck allow copy option above.");
                            LoadConfigurationBlocks();
                            failed = true;
                            dragging = false;
                            break;
                        }
                    }
                }
                config.StartingLocation = new Location { X = currentRow, Y = currentCol };
            }

            previousHeight = config.Height;
            previousWidth = config.Width;

            if (failed)
            {
                return;
            }

            newBlock.X = currentRow;
            newBlock.Y = currentCol;

            rect.Fill = new ImageBrush { ImageSource = new BitmapImage(new Uri(block.Icon, UriKind.Relative)) };
            rect.Tag = newBlock;
            config.Blocks[currentRow, currentCol] = newBlock;

            if (!allowCopy) // allowCopy = rectangle inside the grid will replicate each time it is moved.
            {
                if (draggedFromName == "gridMain") // Ignore the dragged rectangle from the side panel.
                {
                    //Get the previous position of the rectangle from the grid and removed it from there
                    //as it was already transferred with the code above.
                    var previousRect = gridMain.Children
                        .Cast<Rectangle>()
                        .Where(i => Grid.GetRow(i) == previousRow && Grid.GetColumn(i) == previousCol)
                        .FirstOrDefault();
                    previousRect.ClearValue(Rectangle.FillProperty);
                    config.Blocks[previousRow.Value, previousCol.Value] = null;
                }
            }

            SaveConfiguration();
            LoadConfigurationBlocks();

            previousRow = null;
            previousCol = null;

            dragging = false;
            draggedFromName = "";
        }

        private void GridRect_DragEnter(object sender, DragEventArgs e)
        {
            var element = e.Source as Rectangle;

            element.Stroke = new SolidColorBrush(Colors.Gray);
            element.StrokeThickness = 2;

            if (element == null)
                return;

            if (previousCol != null && previousRow != null && draggedFromName == "gridMain")
                return;

            //Save the row/col position of the rectangle from the grid that will be moved somewhere else
            //to be removed later after dropping.
            previousCol = Grid.GetColumn(element);
            previousRow = Grid.GetRow(element);

            System.Diagnostics.Debug.WriteLine($"Row: {previousRow}, Col: {previousCol}");
        }

        private void Rectangle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            draggedFromName = ((Grid)((Rectangle)e.Source).Parent).Name;
            dragging = true;
            // Store the mouse position
            draggedPoint = e.GetPosition(null);
        }

        private void Rectangle_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var element = sender as Rectangle;
            if (element.Tag == null) return;

            // Initialize the drag & drop operation
            ChallengerLib.Models.Block data = (ChallengerLib.Models.Block)element.Tag;
            if (data == null)
                return;

            if (dragging)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = draggedPoint - mousePos;

                if (e.LeftButton == MouseButtonState.Pressed &&
                    Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    // Initialize the drag & drop operation
                    DragDrop.DoDragDrop(element, data, DragDropEffects.Move);
                }
            }
        }

        private void AddBlockButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(config.Name))
            {
                //MessageBox.Show("Configuration name must not be empty.", "Action Not Allowed", MessageBoxButton.OK, MessageBoxImage.Error);
                notifier.ShowError("Configuration name must not be empty.");
                return;
            }

            NewEditBoxWindow addBlockWindow = new NewEditBoxWindow(config.BlockTemplates.Select(a=>a.Name).ToList());
            bool? result = addBlockWindow.ShowDialog();
            if (result.HasValue && result.Value == true)
            {
                previousHeight = config.Height;
                previousWidth = config.Width;
                config.BlockTemplates.Add(addBlockWindow.CreatedBlock);
                LoadSimulationBlocks();
                SaveConfiguration();
            }
        }

        private void btnLoadConfig_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog configLoader = new OpenFileDialog();
            configLoader.Filter = "Text files (*.json)|*.json|All files (*.*)|*.*";
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            configLoader.InitialDirectory = System.IO.Path.Combine(baseDir,"SimulationConfig");
            if (configLoader.ShowDialog() == true)
            {
                LoadConfiguration(configLoader.FileName);
            }
        }

        private void btnClearConfigGrid_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete all your existing objects?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                ((IList)config.Blocks).Clear();
                LoadConfigurationBlocks();
                SaveConfiguration();
            }
        }

        private void txtName_TextChanged(object sender, TextChangedEventArgs e)
        {
            config.Name = txtName.Text;
            //SaveConfiguration();
        }

        private void txtWidth_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtWidth.Text))
            {
                txtWidth.Text = config.Width.ToString();
                return;
            }
            config.Width = Convert.ToInt32(txtWidth.Text);
            //loadConfigurationBlocks();
            //SaveConfiguration();
        }

        private void txtHeight_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtHeight.Text))
            {
                txtHeight.Text = config.Height.ToString();
                return;
            }
            config.Height = Convert.ToInt32(txtHeight.Text);
            //loadConfigurationBlocks();
            //SaveConfiguration();
        }

        private void checkAllowCopy_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox check = (CheckBox)sender;
            allowCopy = check.IsChecked.Value;
        }

        private void checkAllowCopy_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox check = (CheckBox)sender;
            allowCopy = check.IsChecked.Value;
        }

        private void btnSaveConfig_Click(object sender, RoutedEventArgs e)
        {
            if (!HasStartSimObject())
            {
                notifier.ShowError("Please add \"START\" object.");
                return;
            }

            if (!HasEndSimObject())
            {
                notifier.ShowError("Please add at least 1 \"END or STOP\" object.");
                return;
            }

            if (SaveConfiguration())
            {
                LoadConfigurationBlocks();
                notifier.ShowSuccess("Settings successfully saved.");
            }
        }

        private void txtFreeMoves_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtFreeMoves.Text))
            {
                txtFreeMoves.Text = config.FreeMoves.ToString();
                return;
            }
            config.FreeMoves = Convert.ToInt32(txtFreeMoves.Text);
        }

        private static bool IsTextAllowed(string text)
        {
            Regex regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
            return !regex.IsMatch(text);
        }

        private void txtFreeMoves_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void txtHeight_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void txtWidth_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void sliderMoveFactor_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Slider slider = (Slider)sender;
            config.MoveFactor = slider.Value;
            SaveConfiguration();
        }

        private void sliderMoveFactor_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lblMoveFactorValue.Text = sliderMoveFactor.Value.ToString() + "%";
        }

        private void btnNewConfiguration_Click(object sender, RoutedEventArgs e)
        {
            ResetConfiguration();
        }

        private void ResetConfiguration()
        {
            if (rlvPanel != null)
                rlvPanel.Close();

            rlvPanel = null;

            config.Name = string.Empty;
            ((IList)config.Blocks).Clear();
            config.BlockTemplates.Clear();
            config.FreeMoves = 1000;
            config.Height = 20;
            config.Width = 20;
            config.MoveFactor = 100;
            config.StartingLocation = new Location { X = 0, Y = 0 };

            config.Blocks = (ChallengerLib.Models.Block[,])Utilities.ResizeArray(config.Blocks, new int[] { config.Height, config.Width });
            LoadSimulationBlocks();
            LoadConfigurationBlocks();

            txtName.Text = config.Name;
            txtFreeMoves.Text = config.FreeMoves.ToString();
            txtHeight.Text = config.Height.ToString();
            txtWidth.Text = config.Width.ToString();
            sliderMoveFactor.SetValue(Slider.ValueProperty, config.MoveFactor);
        }

        private void btnInitialize_Click(object sender, RoutedEventArgs e)
        {
            LoadConfigurationBlocks();
        }

        private void btnVisualizer_Click(object sender, RoutedEventArgs e)
        {
            //rlmDbData = new RlmDbDataPostgreSqlServer(dbIdentifier);
            rlmDbData = new RlmDbDataSQLServer(dbIdentifier);
            core = new RLVCore(rlmDbData);

            rlvPanel = new VisualizerWindow(core, challenger.HighestMoveCount, challenger.RecentMoves);
            rlvPanel.Show();
        }
    }
}

using RetailPoCSimple.Models;
using RLM.Models;
using RLV.Core;
using RLV.Core.Enums;
using RLV.Core.Interfaces;
using RLV.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
using System.Windows.Threading;
using WPFVisualizer;

namespace RetailPoCSimple
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {

        const int SMALL_ITEMS_COUNT = 105;
        const int LARGE_ITEMS_COUNT = 105;
        const int SHELVES = 2;
        const int SLOTS_PER_SHELF = 4;

        private PlanogramOptimizer optimizer;
        private Item[] itemsCache = null;
        private NoDataNotificationWindow noData = null;
        private int previousSelectedSlotIndex = -1;
        private Rectangle previousSelectedRect = null;
        private int _row = 0;
        private int _col = 0;
        private PlanogramOptResults currentResults;
        private int itemRow = -1;
        private int itemCol = -1;
        private RlmLearnedSessionDetails tmpSelectedData;
        private RlmLearnedSessionDetails tmpComparisonData;
        private VisualizerWindow rlvPanel;
        int selectedSlotIndex = -1;
        private IRLVOutputVisualizer visualizer = null;
        private IRLVCore core = null;
        private Random rnd;
        private IDictionary<Color, SolidColorBrush> coloredBrushesDict = new Dictionary<Color, SolidColorBrush>();
        private IDictionary<int, ImageBrush> imageBrushesDict = new Dictionary<int, ImageBrush>();
        
        public List<Attributes> attributes;
        public List<Item> items;
        public List<string> flavors;
        public bool NoData { get; set; }
        public Item[] ItemsCache { get { return itemsCache; } }
        public SimulationCsvLogger Logger { get; set; } = new SimulationCsvLogger();
        public SimulationSettings SimulationSettings { get { return simSettings; } }

        private MockData mock = null;
        public MainWindow()
        {
            InitializeComponent();

            //Populates Icecream Item Grid
            //flavors = new List<string> {
            //    "Mint Chocolate Chip","Vanilla","Chocolate Fudge",
            //    "Coffee","Raspberry Sherbet","Mocha",
            //    "Banana Nut Fudge","Cookies and Cream","Birthday Cake",
            //    "Strawberry Cheesecake","Rock and Pop Swirl","French Vanilla",
            //    "Daiquiri Ice","Peanut Butter and Chocolate","Pistachio Almond",
            //    "Peanut Butter Cup","Rocky Road","Vanilla Graham",
            //    "Mango","Nutty Coconut","Berry Strawberry",
            //    "Orange sherbet","Pink Bubblegum","Chocoloate Almond",
            //    "Egg Nog","Ube Avocado","Cherry Strawyberry",
            //    "Chocolate Almond","Vanilla Coffee swirl","Lemon Custard",
            //    "Chocolate Chip Cookie Dough","Blackberry"
            //};
            //attributes = new List<Attributes>();
            //items = new List<Item>();
            //Gives random images on grid
            //rnd = new Random();

            mock = new MockData(); 
            mock.Generate();

            //for (int row = 0; row < 4; row++)
            //{
            //    for (int column = 0; column < 8; column++)
            //    {
            //        //gives random images on grid
            //        int ItemImageNumber = rnd.Next(1, 32);

            //        Rectangle itemAttributes = new Rectangle();
            //        itemAttributes.Fill = new ImageBrush
            //        {
            //            ImageSource = new BitmapImage(new Uri(@"pack://application:,,,/RetailPoCSimple;component/Images/" + ItemImageNumber + ".jpg", UriKind.Absolute))
            //        };
            //        itemAttributes.Stroke = new SolidColorBrush(Colors.Black);
            //        Grid.SetColumn(itemAttributes, column);
            //        Grid.SetRow(itemAttributes, row);
            //        gridIceCreamShelves.Children.Add(itemAttributes);
            //    }
            //}

            InitializeGrid(gridIceCreamShelves, Colors.LightGray);
            //FillGrid(gridIceCreamShelves, Colors.LightGray);
            EnableControlButtons(false);

            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => {
                showCriteriaSettingsDialog();
            }));
        }

        private SolidColorBrush GetBrushFromColor(Color color)
        {
            SolidColorBrush retVal = null;

            if (coloredBrushesDict.ContainsKey(color))
            {
                retVal = coloredBrushesDict[color];
            }
            else
            {
                coloredBrushesDict.Add(color, retVal = new SolidColorBrush(color));
            }

            return retVal;
        }

        private ImageBrush GetImgBrushFromItem(ShelfItem item)
        {
            ImageBrush retVal = null;

            if (imageBrushesDict.ContainsKey(item.ItemID))
            {
                retVal = imageBrushesDict[item.ItemID];
            }
            else
            {
                imageBrushesDict.Add(item.ItemID, retVal = new ImageBrush { ImageSource = new BitmapImage(new Uri(item.ImageUri, UriKind.Absolute)) });
            }

            return retVal;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string dbIdentifier = "RLV_small";
            // instantiate visualizer with this window as its parent reference
            visualizer = new RLVOutputVisualizer(this);
            core = new RLVCore(dbIdentifier);

            this.Top = 20;
            rlvPanel = new VisualizerWindow(core, visualizer);
        }

        private void showRLVConfigurationPanel()
        {
            vsConfig = new RLVConfigurationPanel(rlvPanel.rlv.ChartControl, rlvPanel.rlv.DetailsControl);
            vsConfig.Show();
            vsConfig.Top = this.Top;
            vsConfig.Left = this.Left + this.Width;
        }

        private void gridIceCreamShelves_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            rlvPanel.Visibility = Visibility.Visible;

            var point = Mouse.GetPosition(gridIceCreamShelves);

            int row = 0;
            int col = 0;
            double accumulatedHeight = 0.0;
            double accumulatedWidth = 0.0;

            foreach (var rowDefinition in gridIceCreamShelves.RowDefinitions)
            {
                accumulatedHeight += rowDefinition.ActualHeight;
                if (accumulatedHeight >= point.Y)
                {
                    break;
                }

                row++;
            }

            foreach (var columnDefinition in gridIceCreamShelves.ColumnDefinitions)
            {
                accumulatedWidth += columnDefinition.ActualWidth;
                if (accumulatedWidth >= point.X)
                    break;
                col++;
            }

            RemoveHighlightedItems();

            // row and col now correspond Grid's RowDefinition and ColumnDefinition mouse was 
            // over when double clicked!            
            var rect = gridIceCreamShelves.Children
                .Cast<Rectangle>()
                .Where(i => Grid.GetRow(i) == row && Grid.GetColumn(i) == col)
                .FirstOrDefault();

            HighlightItem(rect);

            selectedSlotIndex = gridIceCreamShelves.Children.IndexOf(rect);

            var inputValues = new[]
            {
                new RLVIOValues() { IOName = "Slot", Value = (selectedSlotIndex + 1).ToString() }
            };

            // todo outputValues

            var itemDisplay = new[]
            {
                new RLVItemDisplay() { Name = "Item", Value = "Item #" + (selectedSlotIndex + 1), Visibility = RLVVisibility.Visible } // todo pass in actual item name (see: Value)
            };

            visualizer.SelectNewItem(inputValues, null, itemDisplay);
        }
        private void HighlightItem(int row, int col)
        {
            var rect = gridIceCreamShelves.Children
                          .Cast<Rectangle>()
                          .Where(i => Grid.GetRow(i) == row && Grid.GetColumn(i) == col).FirstOrDefault();

            HighlightItem(rect);
        }

        private void HighlightItem(Rectangle rect)
        {
            rect.StrokeThickness = 3;
            //rect.Stroke = new SolidColorBrush(Colors.GreenYellow);
            rect.Stroke = GetBrushFromColor(Colors.Blue);
        }

        private void RemoveHighlightedItems()
        {
            var shapes = gridIceCreamShelves.Children
                          .Cast<Rectangle>();

            foreach (var shape in shapes)
            {
                shape.StrokeThickness = 1;
                shape.Stroke = new SolidColorBrush(Colors.Black);
                //shape.Tag = null;
            }
        }

        private double RandomDoubleNumber(double minimum, double maximum)
        {
            int randomNumber = rnd.Next(1, 11);
            if (randomNumber > 8)
            {
                double highRange = Convert.ToDouble(rnd.Next(80, 101)) / 100D;
                var value = highRange * (maximum * 2) + minimum;
                return value;
            }
            else
            {
                return rnd.NextDouble() * (maximum * 0.5) + minimum;
            }
        }

        private RLVConfigurationPanel vsConfig = null;
        private void TextBlock_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //vswin.detailsControl.LoadConfigurationPanel(this);
            if(rlvPanel != null)
            {
                showRLVConfigurationPanel();
            }
        }

        private SimulationSettings simSettings = new SimulationSettings()
        {
            SimType = SimulationType.Time,
            NumItems = LARGE_ITEMS_COUNT,
            Sessions = 10000,
            Metric1 = 20,
            Metric2 = 20,
            Metric3 = 20,
            Metric4 = 20,
            Metric5 = 20,
            NumShelves = SHELVES,
            NumSlots = SLOTS_PER_SHELF,
            DefaultScorePercentage = 85,
            Hours = 0.0666666667 // 4 minutes
        };

        private void EnableControlButtons(bool enabled)
        {
            //toggleColorBtn.IsEnabled = value;
            //btnOptimize.IsEnabled = value;
            //btnMetrics.IsEnabled = value;
            //if (enabled)
            //{
            //    btnOptimize.Visibility = Visibility.Visible;
            //}
            //else
            //{
            //    btnOptimize.Visibility = Visibility.Hidden;
            //}
        }

        public void DisplayLearningComparisonResults(RlmLearnedSessionDetails selectedData, RlmLearnedSessionDetails comparisonData)
        {
            if (core.IsComparisonModeOn)
            {
                tmpSelectedData = selectedData;
                tmpComparisonData = comparisonData;
                itemRow = _row;
                itemCol = _col;
            }
            comparisonOverlay.Visibility = Visibility.Visible;

            IEnumerable<Shelf> selectedPlanogram = GetShelfData(selectedData);
            IEnumerable<Shelf> comparisonPlanogram = null;
            if (comparisonData != null)
            {
                comparisonPlanogram = GetShelfData(comparisonData);
            }

            FillGrid(gridIceCreamShelves, selectedPlanogram, false, mouseEnterHandler: ItemAttributes_Compare_MouseEnter, mouseLeaveHandler: ItemAttributes_Compare_MouseLeave, comparisonResult: comparisonPlanogram);

            //btnComparisonClose.Visibility = Visibility.Visible;
            //btnComparisonClose.IsEnabled = true;

            txtStatus.Visibility = Visibility.Hidden;
            txtTimeElapsed.Visibility = Visibility.Hidden;
            txtItemScore.Visibility = Visibility.Hidden;

        }

        protected override void OnClosed(EventArgs e)
        {
            if (rlvPanel != null)
            {
                rlvPanel.Close();
            }

            base.OnClosed(e);
            Application.Current.Shutdown();
        }

        private void OnSelectedItem(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var point = Mouse.GetPosition(gridIceCreamShelves);

            int row = 0;
            int col = 0;
            double accumulatedHeight = 0.0;
            double accumulatedWidth = 0.0;

            foreach (var rowDefinition in gridIceCreamShelves.RowDefinitions)
            {
                accumulatedHeight += rowDefinition.ActualHeight;
                if (accumulatedHeight >= point.Y)
                {
                    break;
                }

                row++;
            }

            foreach (var columnDefinition in gridIceCreamShelves.ColumnDefinitions)
            {
                accumulatedWidth += columnDefinition.ActualWidth;
                if (accumulatedWidth >= point.X)
                    break;
                col++;
            }

            RemoveHighlightedItems();

            // row and col now correspond Grid's RowDefinition and ColumnDefinition mouse was 
            // over when double clicked!     
            var rect = gridIceCreamShelves.Children
                .Cast<Rectangle>()
                .Where(i => Grid.GetRow(i) == row && Grid.GetColumn(i) == col)
                .FirstOrDefault();

            if (rect == null)
            {
                row = itemRow;
                col = itemCol;

                rect = gridIceCreamShelves.Children
                .Cast<Rectangle>()
                .Where(i => Grid.GetRow(i) == row && Grid.GetColumn(i) == col)
                .FirstOrDefault();
            }

            HighlightItem(rect);
            _row = row;
            _col = col;
            selectedSlotIndex = gridIceCreamShelves.Children.IndexOf(rect);
            //MessageBox.Show($"row: {row}, col: {col}, index: {rectIndex}");

            var inputValues = new[]
            {
                new RLVIOValues() { IOName = "Slot", Value = (selectedSlotIndex + 1).ToString() }
            };

            int itemIndex = 0;
            if (rect.Tag is ShelfItem)
            {
                itemIndex = ((ShelfItem)rect.Tag).Index - 1;
            }
            else if (rect.Tag is KeyValuePair<ShelfItem, ShelfItem>)
            {
                itemIndex = ((KeyValuePair<ShelfItem, ShelfItem>)rect.Tag).Key.Index - 1;
            }

            var outputValues = new[]
            {
                new RLVIOValues() { IOName = "Item", Value = itemIndex.ToString() }
            };

            var itemDisplay = new[]
            {
                new RLVItemDisplay() { Name = "Item", Value = "Item #" + (selectedSlotIndex + 1), Visibility = RLVVisibility.Visible } // todo pass in actual item name (see: Value)
            };


            visualizer?.SelectNewItem(inputValues, outputValues, itemDisplay);

            if (!NoData)
            {
                if (noData != null)
                {
                    noData.Close();
                }

                if (rlvPanel != null)
                {
                    rlvPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    if (rlvPanel != null)
                    {
                        rlvPanel.Close();
                    }
                    rlvPanel = new VisualizerWindow(core, visualizer);
                    rlvPanel.Show();
                }

                previousSelectedSlotIndex = selectedSlotIndex;
                previousSelectedRect = rect;
                alignPanels();
            }
            else
            {
                if (noData != null)
                {
                    noData.Close();
                }
                noData = new NoDataNotificationWindow();
                noData.Show();
                noData.Top = this.Top;
                noData.Left = this.Left + this.Width;

                selectedSlotIndex = previousSelectedSlotIndex;

                // Highlight the previous item with data
                if (rect != null)
                {
                    rect.StrokeThickness = 1;
                    rect.Stroke = new SolidColorBrush(Colors.Black);
                }

                if (previousSelectedRect != null)
                {
                    HighlightItem(previousSelectedRect);
                }
            }
            //HighlightItem(row, col);

            //MessageBox.Show($"row: {row}, col: {col}");
        }

        private void alignPanels()
        {
            if (rlvPanel != null)
            {
                //this.Top = 20;
                rlvPanel.Top = this.Top;

                //rlvPanel.Left = 10;
                //this.Left = rlvPanel.Left + rlvPanel.Width;
                rlvPanel.Left = this.Left - rlvPanel.Width;
            }
        }

        private void InitializeGrid(Grid grid, Color color, bool isRLM = true)
        {
            grid.Children.Clear();

            int slotNumber = 0;
            for (int row = 0; row < simSettings.NumShelves; row++)
            {
                for (int column = 0; column < simSettings.NumSlots; column++)
                {
                    Rectangle item = new Rectangle();
                    item.Fill = GetBrushFromColor(color);
                    item.Stroke = GetBrushFromColor(Colors.Black);
                    item.Tag = null;
                    //itemAttributes.HorizontalAlignment = HorizontalAlignment.Center;

                    //itemAttributes.InputBindings.Add(new MouseBinding()
                    //{
                    //    Gesture = new MouseGesture(MouseAction.LeftClick),
                    //    Command = new ItemClickedCommand(() =>
                    //    {
                    //        OnSelectedItem(itemAttributes, null);
                    //    })
                    //});

                    if (isRLM)
                    {
                        item.InputBindings.Add(new MouseBinding()
                        {
                            Gesture = new MouseGesture(MouseAction.LeftClick),
                            Command = new ItemClickedCommand(() =>
                            {
                                OnSelectedItem(item, null);
                            })
                        });
                    }

                    if (slotNumber == selectedSlotIndex)
                    {
                        HighlightItem(item);
                    }

                    Grid.SetColumn(item, column);
                    Grid.SetRow(item, row);
                    grid.Children.Add(item);

                    slotNumber++;
                }
            }
        }
        
        private void FillGrid(Grid grid, Color color)
        {
            int slotNumber = 0;
            foreach(var item in grid.Children.Cast<Rectangle>())
            {
                item.Fill = GetBrushFromColor(color);
                item.Stroke = GetBrushFromColor(Colors.Black);
                item.Tag = null;

                item.MouseEnter -= ItemAttributes_MouseEnter;
                item.MouseEnter -= ItemAttributes_Compare_MouseEnter;

                item.MouseLeave -= ItemAttributes_MouseLeave;
                item.MouseLeave -= ItemAttributes_Compare_MouseLeave;                

                if (slotNumber == selectedSlotIndex)
                {
                    HighlightItem(item);
                }

                slotNumber++;
            }
        }

        private void FillGrid(Grid planogramGrid, IEnumerable<Shelf> result, bool usePerfColor = false, MouseEventHandler mouseEnterHandler = null, MouseEventHandler mouseLeaveHandler = null, IEnumerable<Shelf> comparisonResult = null, bool isRLM = true)
        {
            if (result == null || planogramGrid == null)
                return;

            var itemRectangles = planogramGrid.Children.Cast<Rectangle>();

            int slotNumber = 0;
            int row = 0;
            foreach (var shelf in result)
            {
                int col = 0;
                //bool highlight = false;
                foreach (var item in shelf.Items)
                {
                    Rectangle itemRect = itemRectangles.ElementAt(slotNumber);
                    itemRect.ToolTip = item.Name;

                    if (usePerfColor)
                    {
                        itemRect.Fill = GetImgBrushFromItem(item); //new ImageBrush { ImageSource = new BitmapImage(new Uri(item.ImageUri, UriKind.Absolute)) };
                    }
                    else
                    {
                        itemRect.Fill = GetImgBrushFromItem(item); //new ImageBrush { ImageSource = new BitmapImage(new Uri(item.ImageUri, UriKind.Absolute)) };
                        //itemAttributes.Fill = new SolidColorBrush(item.Color);
                    }

                    itemRect.Stroke = GetBrushFromColor(Colors.Black); //new SolidColorBrush(Colors.Black);
                    itemRect.Tag = item;

                    itemRect.MouseEnter -= ItemAttributes_MouseEnter;
                    itemRect.MouseEnter -= ItemAttributes_Compare_MouseEnter;

                    itemRect.MouseLeave -= ItemAttributes_MouseLeave;
                    itemRect.MouseLeave -= ItemAttributes_Compare_MouseLeave;

                    if (comparisonResult == null)
                    {
                        if (mouseEnterHandler != null)
                            itemRect.MouseEnter += mouseEnterHandler;
                        if (mouseLeaveHandler != null)
                            itemRect.MouseLeave += mouseLeaveHandler;
                    }
                    else
                    {
                        var compItem = comparisonResult.ElementAt(row).Items.ElementAt(col);

                        if (compItem.ItemID != item.ItemID)
                        {
                            itemRect.Stroke = GetBrushFromColor(Colors.OrangeRed); 
                            itemRect.StrokeThickness = 2;
                            itemRect.Tag = new KeyValuePair<ShelfItem, ShelfItem>(item, compItem);

                            if (mouseEnterHandler != null)
                                itemRect.MouseEnter += mouseEnterHandler;
                            if (mouseLeaveHandler != null)
                                itemRect.MouseLeave += mouseLeaveHandler;
                        }
                    }

                    if (slotNumber == selectedSlotIndex)
                    {
                        HighlightItem(itemRect);
                    }
                    
                    col++;
                    slotNumber++;
                }
                row++;
            }
        }

        private void ItemAttributes_Compare_MouseEnter(object sender, MouseEventArgs e)
        {
            var itemPair = (KeyValuePair<ShelfItem, ShelfItem>)((Rectangle)sender).Tag;
            //comparisonGrid.Visibility = Visibility.Visible;
            setComparisonData(itemPair.Key, itemPair.Value);
            //txtItemScore.Text = itemPair.Value.Score.ToString("#,###.##");
        }

        private void ItemAttributes_Compare_MouseLeave(object sender, MouseEventArgs e)
        {
            txtItemScore.Text = " ";
            //comparisonGrid.Visibility = Visibility.Hidden;
            if (compPanel != null)
            {
                compPanel.Close();
            }
        }

        private void ItemAttributes_MouseEnter(object sender, MouseEventArgs e)
        {
            var item = ((ShelfItem)((Rectangle)sender).Tag);
            txtItemScore.Text = item.Score.ToString("c");
        }

        private void ItemAttributes_MouseLeave(object sender, MouseEventArgs e)
        {
            txtItemScore.Text = " ";
        }

        private IEnumerable<Shelf> GetShelfData(RlmLearnedSessionDetails data)
        {
            var shelves = new List<Shelf>();

            IDictionary<string, RlmIODetails> inputDict = data.Inputs.Where(a => a.CycleScore == 1).ToDictionary(a => a.Value, a => a);
            IDictionary<long, RlmIODetails> outputDict = data.Outputs.Where(a => a.CycleScore == 1).ToDictionary(a => a.CaseId, a => a);

            int numSlots = simSettings.NumShelves * simSettings.NumSlots;


            var shelf = new Shelf();
            for (int i = 1; i <= numSlots; i++)
            {
                var input = inputDict[i.ToString()];
                var output = outputDict[input.CaseId];

                Item itemReference = itemsCache[Convert.ToInt32(output.Value)];
                shelf.Add(itemReference, PlanogramOptimizer.GetCalculatedWeightedMetrics(itemReference, simSettings));

                if (i % simSettings.NumSlots == 0)
                {
                    shelves.Add(shelf);
                    shelf = new Shelf();
                }
            }

            return shelves;
        }


        private void UpdateRLMResults(PlanogramOptResultsSettings results, bool enableSimDisplay)
        {
            if (enableSimDisplay)
                Task.Delay(1).Wait();

            Dispatcher.Invoke(() =>
            {
                currentResults = results;
                if (enableSimDisplay && !core.IsComparisonModeOn)
                {
                    FillGrid(gridIceCreamShelves, results.Shelves, false, ItemAttributes_MouseEnter, ItemAttributes_MouseLeave);
                }

                if (core.IsComparisonModeOn)
                {
                    DisplayLearningComparisonResults(tmpSelectedData, tmpComparisonData);
                }
                else
                {
                    string scoreText = (simSettings.SimType == SimulationType.Score) ? $"{results.Score.ToString("c")} ({results.NumScoreHits})" : results.Score.ToString("c");
                    txtCurrentScore.Text = scoreText;

                    //sessionRunTxt.Text = results.CurrentSession.ToString();
                    txtTimeElapsed.Text = results.TimeElapsed.ToString();
                    //txtMinScore.Text = results.MinScore.ToString("#,###.##");
                    txtMaxScore.Text = results.MaxScore.ToString("c");
                    //txtAverageScore.Text = results.AvgScore.ToString("#,###.##");
                    //txtAverageOfTenScores.Text = results.AvgLastTen.ToString("#,###.##");
                    //txtCurrentRandomnes.Text = results.CurrentRandomnessValue.ToString();
                    //txtStartRandomnes.Text = results.StartRandomness.ToString();
                    //txtEndRandomnes.Text = results.EndRandomness.ToString();
                    //txtSessionPerBatch.Text = results.SessionsPerBatch.ToString();
                    //txtInputType.Text = results.InputType;
                }
            });
        }

        private void UpdateRLMStatus(string statusMsg, bool isDone = false)
        {
            Dispatcher.Invoke(() =>
            {
                txtStatus.Text = statusMsg;
                if (isDone) 
                    EnableControlButtons(true);
            });
        }

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            showCriteriaSettingsDialog();
        }

        private void startOptimizing()
        {
            trainingOverlay.Visibility = Visibility.Visible;
            selectedSlotIndex = -1;
            _row = -1;
            _col = -1;
            itemRow = -1;
            itemCol = 1;

            SimulationPanel simPanel = new SimulationPanel(mock);
            simPanel.SetSimSettings(simSettings);
            //bool? result = simPanel.ShowDialog();

            simPanel.btnRun_Click(null, null);

            //if (result.HasValue && result.Value == true)
            {
                // resets grid to default
                FillGrid(gridIceCreamShelves, Colors.LightGray);
                EnableControlButtons(false);

                // set simulation settings
                simSettings.SimType = simPanel.SimType;
                simSettings.Sessions = simPanel.Sessions;
                simSettings.Hours = simPanel.Hours;
                simSettings.Score = simPanel.Score;
                simSettings.EnableSimDisplay = simPanel.EnableSimDisplay;
                simSettings.DefaultScorePercentage = simPanel.simScoreSlider.Value;

                txtTargetScore.Text = "";
                if (simSettings.SimType == SimulationType.Score)
                {
                    txtTargetScore.Text = simSettings.Score.Value.ToString("n");
                }
                else
                {
                    txtTargetScore.Visibility = Visibility.Hidden;
                }

                //if (simSettings.SimType == SimulationType.Sessions)
                //{
                //    lblSessionPerBatch.Visibility = Visibility.Hidden;
                //    txtSessionPerBatch.Visibility = Visibility.Hidden;
                //}
                //else
                //{
                //    lblSessionPerBatch.Visibility = Visibility.Visible;
                //    txtSessionPerBatch.Visibility = Visibility.Visible;
                //}


                string dbIdentifier = "RLM_planogram_" + Guid.NewGuid().ToString("N");
                // instantiate visualizer with this window as its parent reference
                visualizer = new RLVOutputVisualizer(this);
                core = new RLVCore(dbIdentifier);

                // open temporary RLV container panel
                // todo this must be embeded in this Window instead of the temporary container
                if (rlvPanel != null)
                {
                    rlvPanel.Close();
                }

                rlvPanel = new VisualizerWindow(core, visualizer);
                Task.Run(() =>
                {
                    // get items from db as well as the min and max metric scores as we need that for the calculation later on
                    Item[] items;
                    //using (PlanogramContext ctx = new PlanogramContext())
                    {
                        //MockData mock = new MockData(ctx);
                        items = itemsCache = mock.GetItemsWithAttr();
                        simSettings.ItemMetricMin = mock.GetItemMinimumScore(simSettings);
                        simSettings.ItemMetricMax = mock.GetItemMaximumScore(simSettings);
                    }

                    // initialize and start RLM training
                    optimizer = new PlanogramOptimizer(items, simSettings, this.UpdateRLMResults, this.UpdateRLMStatus, Logger, dbIdentifier, OnRLMDataPersistProgress);
                    //optimizer.OnSessionDone += Optimizer_OnSessionDone;
                    optimizer.StartOptimization();
                });
            }
        }

        private ShelfItem currentItem;
        private ShelfItem prevItem;
        private ItemComparisonPanel compPanel = new ItemComparisonPanel();
        private void setComparisonData(ShelfItem current, ShelfItem prev)
        {
            compPanel.SetItems(current, prev);

            if (compPanel != null)
            {
                compPanel.Close();
            }

            compPanel.Show();
            compPanel.Top = this.Top;
            compPanel.Left = this.Left + this.Width;

            //currentItem = current;
            //prevItem = prev;

            //rectCurr.Fill = new ImageBrush { ImageSource = new BitmapImage(new Uri(current.ImageUri, UriKind.Absolute)) };
            //txtCurrName.Text = current.Name;
            //txtCurrScore.Text = current.Score.ToString("#,##0.##");

            //rectPrev.Fill = new ImageBrush { ImageSource = new BitmapImage(new Uri(prev.ImageUri, UriKind.Absolute)) };
            //txtPrevName.Text = prev.Name;
            //txtPrevScore.Text = prev.Score.ToString("#,##0.##");
        }

        private void mainWindow_LocationChanged(object sender, EventArgs e)
        {
            alignPanels();
        }

        private void btnMetrics_Click(object sender, RoutedEventArgs e)
        {
            MetricPanel metricPanel = new MetricPanel();

            metricPanel.SetMetricSliderValues(simSettings); // Setting the metric values to what's on the simulation settings

            bool? result = metricPanel.ShowDialog();

            if (result.HasValue && result.Value == true)
            {
                // Set the simulation setting values for metrics from the metric panel
                simSettings.Metric1 = metricPanel.Metric1;
                simSettings.Metric2 = metricPanel.Metric2;
                simSettings.Metric3 = metricPanel.Metric3;
                simSettings.Metric4 = metricPanel.Metric4;
                simSettings.Metric5 = metricPanel.Metric5;
            }
        }

        private void showCriteriaSettingsDialog()
        {
            MetricPanel metricPanel = new MetricPanel();

            metricPanel.SetMetricSliderValues(simSettings); // Setting the metric values to what's on the simulation settings

            metricPanel.Closing += (s, e) =>
            {
                if (metricPanel.DialogResult == null)
                {
                    bool yesSelected = MessageBox.Show("Closing this dialog box will use the default criteria.", "", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
                    if (yesSelected)
                    {
                        startOptimizing();
                    }
                    else
                    {
                        e.Cancel = true;
                    }
                }
            };

            metricPanel.Top = this.Top + 60;
            metricPanel.Left = this.Left + ((this.Width/2) -  (metricPanel.Width/2));

            bool? result = metricPanel.ShowDialog();

            if (result.HasValue && result.Value == true)
            {
                // Set the simulation setting values for metrics from the metric panel
                simSettings.Metric1 = metricPanel.Metric1;
                simSettings.Metric2 = metricPanel.Metric2;
                simSettings.Metric3 = metricPanel.Metric3;
                simSettings.Metric4 = metricPanel.Metric4;
                simSettings.Metric5 = metricPanel.Metric5;

                //btnRun_Click(null, null);
                startOptimizing();
            }
        }

        private void btnComparisonClose_Click(object sender, RoutedEventArgs e)
        {
            comparisonOverlay.Visibility = Visibility.Hidden;
            visualizer.CloseComparisonMode();
            FillGrid(gridIceCreamShelves, currentResults.Shelves, false, ItemAttributes_MouseEnter, ItemAttributes_MouseLeave);
            //FillGrid(planogram, Colors.Gray);

            //btnComparisonClose.Visibility = Visibility.Hidden;

            RemoveHighlightedItems();

            // row and col now correspond Grid's RowDefinition and ColumnDefinition mouse was 
            // over when double clicked!            
            var rect = gridIceCreamShelves.Children
                .Cast<Rectangle>()
                .Where(i => Grid.GetRow(i) == itemRow && Grid.GetColumn(i) == itemCol)
                .FirstOrDefault();

            HighlightItem(rect);
            OnSelectedItem(rect, null);

            txtStatus.Visibility = Visibility.Visible;
            txtTimeElapsed.Visibility = Visibility.Visible;
            txtItemScore.Visibility = Visibility.Visible;
        }

        private void closeComparisonLink_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            btnComparisonClose_Click(null, null);
        }

        //private void OnRLMDataPersistComplete()
        //{
        //    Dispatcher.Invoke(() =>
        //    {
        //        trainingOverlay.Visibility = Visibility.Hidden;
        //        // TODO ryan's code for removing the overlay
        //        txtStatus.Text = "Ready";
        //        watch.Stop();

        //        //MessageBox.Show($"Data persistence done! {watch.Elapsed}");
        //    });
        //}

        private void OnRLMDataPersistProgress(long processed, long total)
        {
            System.Diagnostics.Debug.WriteLine($"{processed} / {total}");

            if (processed >= total && optimizer.IsTrainingDone)
            {
                Dispatcher.Invoke(() =>
                {
                    trainingOverlay.Visibility = Visibility.Hidden;
                    // TODO ryan's code for removing the overlay
                    txtStatus.Text = "Ready";

                    //MessageBox.Show($"Data persistence done! {watch.Elapsed}");
                });
            }
        }
    }
}

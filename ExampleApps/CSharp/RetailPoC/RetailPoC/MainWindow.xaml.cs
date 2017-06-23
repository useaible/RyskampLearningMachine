using RetailPoC.Models;
using System;
using System.Collections.Generic;
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
using System.Data.Entity;
using RetailPoC.ViewModels;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;
using System.Diagnostics;
using RLV.Core;
using WPFVisualizer;
using RLV.Core.Interfaces;
using RLV.Core.Models;
using RLV.Core.Enums;
using RLM.Models;

namespace RetailPoC
{
    public delegate void SimulationStart(Item[] items, SimulationSettings simSettings);

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        const int SMALL_ITEMS_COUNT = 100;
        const int LARGE_ITEMS_COUNT = 5000;
        const int SHELVES = 12;
        const int SLOTS_PER_SHELF = 24;
        private DataFactory dataFactory = new DataFactory();
        private DataPanel dataPanel = new DataPanel();
        public bool NoData { get; set; } = true;

        private SimulationSettings simSettings = new SimulationSettings()
        {
            //SimType = SimulationType.Sessions,
            NumItems = LARGE_ITEMS_COUNT,
            Sessions = 10000,
            Metric1 = 10,
            Metric2 = 10,
            Metric3 = 10,
            Metric4 = 10,
            Metric5 = 10,
            Metric6 = 10,
            Metric7 = 10,
            Metric8 = 10,
            Metric9 = 10,
            Metric10 = 10,
            NumShelves = SHELVES,
            NumSlots = SLOTS_PER_SHELF,
            DefaultScorePercentage = 85
        };

        private Item[] itemsCache = null;
        private bool headToHead = false;
        private PlanogramOptResults currentResults;
        private PlanogramOptResults currentResultsTensor;
        private bool isRLMDone = false;
        private bool isTensorflowDone = false;
        private bool usePerfColor = false;
        private int selectedSlotIndex = -1;

        public event SimulationStart OnSimulationStart;

        public SimulationCsvLogger Logger { get; set; } = new SimulationCsvLogger();
        public string HelpPath { get; set; } = AppDomain.CurrentDomain.BaseDirectory;
        public SimulationSettings SimulationSettings { get { return simSettings; } }
        public Item[] ItemsCache { get { return itemsCache; } }

        private ItemComparisonPanel itemCompPanel = new ItemComparisonPanel();
        
        private IRLVCore core = new RLVCore("RLV_small");
        //private IRLVSelectedDetailsPanel detailsPanel = new RLVSelectedDetailsPanel();
        //private IRLVProgressionChartPanel chartPanel = new RLVProgressionChartPanel();
        private IRLVPlangoramOutputVisualizer visualizer = null;
        public MainWindow()
        {
            InitializeComponent();
            headToHead = false;
            Width = 580;
            //toggleColorBtn.Margin = new Thickness(422, 14, 0, 0); //new Thickness(62, 10, 0, 0);
            //dataGenerationBtn.Margin = new Thickness(24, 14, 0, 0); //new Thickness(182, 10, 0, 0);
            //metricsBtn.Margin = new Thickness(146, 14, 0, 0); //new Thickness(302, 10, 0, 0);
            //runSlmBtn.Margin = new Thickness(266, 14, 0, 0); //new Thickness(422, 10, 0, 0);

            rectHeadToHeadDivider.Visibility = Visibility.Hidden;

            planogramTensorflow.Visibility = Visibility.Hidden;
 

            tensorFlowPlanogramScore.Visibility = Visibility.Hidden;
            GrdTensorFlowsetting.Visibility = Visibility.Hidden;
            grpBox_Tensorflow.Visibility = Visibility.Hidden;

            FillGrid(planogram, Colors.LightGray);
            //For testing only
            string dbIdentifier = "RLV_small";
            // instantiate visualizer with this window as its parent reference
            visualizer = new RLVOutputVisualizer(this);
            core = new RLVCore(dbIdentifier);

            //core.SetupVisualizer(new List<IRLVPanel>
            //{
            //    detailsPanel,
            //    chartPanel
            //}, visualizer);

            //this.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml") });
            //this.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml") });
            //this.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Colors.xaml") });
            //this.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Accents/Blue.xaml") });
            //this.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Accents/BaseLight.xaml") });
        }

        public MainWindow(bool headtohead)
        {
            InitializeComponent();
            headToHead = headtohead;
            Title += " - Head to Head";
            FillGrid(planogram, Colors.LightGray);
            FillGrid(planogramTensorflow, Colors.LightGray);

            //targetScoreTxt.Margin = new Thickness(522, 128, 0, 0);
            //targetScoreLbl.Margin = new Thickness(497, 98, 0, 0);
        }

        private void dataGenerationBtn_Click(object sender, RoutedEventArgs e)

        {
            showDataPanelDlg();
        }

        private void showDataPanelDlg()
        {
            dataPanel.Closed += (s, ee) => {
                dataPanel = new DataPanel();
                setSelectedPlanogramSize();
            };

            dataPanel.NumItems = simSettings.NumItems;
            dataPanel.NumShelves = simSettings.NumShelves;
            dataPanel.NumSlots = simSettings.NumSlots;

            dataPanel.dataGrid.Loaded += (ss, evv) => {

                bool exist = false;
                using (PlanogramContext ctx = new PlanogramContext())
                {
                    exist = ctx.Database.Exists();
                }

                if (exist)
                {
                    dataPanel.dataGrid.ItemsSource = dataFactory.Items; // Setting data source for the DataPanel's datagrid
                    dataPanel.Items = dataFactory.Items; // Store the items to a new List for searching purposes
                    dataPanel.dataGrid.AutoGenerateColumns = false;

                    // Binding and settings of datagrid columns
                    dataPanel.dataGrid.Columns.Add(new DataGridTextColumn() { Header = "ID", Binding = new Binding("ID"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), IsReadOnly = true });
                    dataPanel.dataGrid.Columns.Add(new DataGridTextColumn() { Header = "SKU", Binding = new Binding("SKU"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), IsReadOnly = true });
                    dataPanel.dataGrid.Columns.Add(new DataGridTextColumn() { Header = "Name", Binding = new Binding("Name"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), IsReadOnly = true });
                }
            };

            dataPanel.ShowDialog();
        }

        private void metricsBtn_Click(object sender, RoutedEventArgs e)
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
                simSettings.Metric6 = metricPanel.Metric6;
                simSettings.Metric7 = metricPanel.Metric7;
                simSettings.Metric8 = metricPanel.Metric8;
                simSettings.Metric9 = metricPanel.Metric9;
                simSettings.Metric10 = metricPanel.Metric10;
            }
        }

        private PlanogramOptimizer optimizer;
        private void runSlmBtn_Click(object sender, RoutedEventArgs e)
        {
            SimulationPanel simPanel = new SimulationPanel();
            simPanel.SetSimSettings(simSettings);
            bool? result = simPanel.ShowDialog();

            if (result.HasValue && result.Value == true)
            {
                // resets grid to default
                usePerfColor = false;
                FillGrid(planogram, Colors.LightGray);
                if (headToHead)
                    FillGrid(planogramTensorflow, Colors.LightGray);

                // disable control buttons
                //statusTxt.Text = statusTxtTensor.Text = "";
                statusTxtTensor.Text = "Waiting for RLM to finish running...";
                EnableControlButtons(false);

                // set simulation settings
                simSettings.SimType = simPanel.SimType;
                simSettings.Sessions = simPanel.Sessions;
                simSettings.Hours = simPanel.Hours;
                simSettings.Score = simPanel.Score;
                simSettings.EnableSimDisplay = simPanel.EnableSimDisplay;
                simSettings.DefaultScorePercentage = simPanel.simScoreSlider.Value;

                targetScoreTxt.Text = "";
                if (simSettings.SimType == SimulationType.Score)
                {
                    targetScoreLbl.Visibility = Visibility.Visible;
                    targetScoreTxt.Visibility = Visibility.Visible;
                    targetScoreTxt.Text = simSettings.Score.Value.ToString("n");
                    targetScoreTxt2.Visibility = Visibility.Visible;
                    targetScoreTxt2.Text = simSettings.Score.Value.ToString("n");
                }
                else
                {
                    targetScoreLbl.Visibility = Visibility.Hidden;
                    targetScoreTxt.Visibility = Visibility.Hidden;
                    targetScoreTxt2.Visibility = Visibility.Hidden;
                }

                if (simSettings.SimType == SimulationType.Sessions)
                {
                    sessionPerBatchLbl.Visibility = Visibility.Hidden;
                    sessionPerBatchTxt.Visibility = Visibility.Hidden;
                }
                else
                {
                    sessionPerBatchLbl.Visibility = Visibility.Visible;
                    sessionPerBatchTxt.Visibility = Visibility.Visible;
                }

                Logger.Clear();


                string dbIdentifier = "RLM_planogram_" + Guid.NewGuid().ToString("N");
                // instantiate visualizer with this window as its parent reference
                visualizer = new RLVOutputVisualizer(this);
                core = new RLVCore(dbIdentifier);

                // subscribe mainwindow to the comparison event
                //visualizer.LearningComparisonDisplayResultsEvent += DisplayLearningComparisonResults;

                // open temporary RLV container panel
                // todo this must be embeded in this Window instead of the temporary container
                if (tmpPanel != null)
                {
                    tmpPanel.Close();
                }

                tmpPanel = new TempRLVContainerPanel(core, visualizer);

                //this.Top = 20;
                //tmpPanel.Top = this.Top;
                //this.Height = tmpPanel.Height;
                //tmpPanel.Left = 10;
                //this.Left = tmpPanel.Width + tmpPanel.Left;
                //tmpPanel.Visibility = Visibility.Hidden;

                Task.Run(() =>
                {
                    // get items from db as well as the min and max metric scores as we need that for the calculation later on
                    Item[] items;
                    using (PlanogramContext ctx = new PlanogramContext())
                    {
                        MockData mock = new MockData(ctx);
                        items = itemsCache = mock.GetItemsWithAttr();
                        simSettings.ItemMetricMin = mock.GetItemMinimumScore(simSettings);
                        simSettings.ItemMetricMax = mock.GetItemMaximumScore(simSettings);
                    }

                    // let's tensorflow (or other listeners) know that it should start training
                    //OnSimulationStart?.Invoke(items, simSettings); //return;

                    // initialize and start RLM training
                    enableSimulation = simSettings.EnableSimDisplay;
                    optimizer = new PlanogramOptimizer(items, simSettings, this.UpdateRLMResults, this.UpdateRLMStatus, Logger, dbIdentifier);
                    //optimizer.OnSessionDone += Optimizer_OnSessionDone;
                    optimizer.StartOptimization();
                });
            }
        }

        private void EnableControlButtons(bool value)
        {
            //toggleColorBtn.IsEnabled = value;
            dataGenerationBtn.IsEnabled = value;
            metricsBtn.IsEnabled = value;
            runSlmBtn.IsEnabled = value;
            rlmCsvBtn.IsEnabled = value;
            tfCsvBtn.IsEnabled = value;
            CbPlanogramSize.IsEnabled = value;
        }

        private bool enableSimulation = false;
        private void UpdateRLMResults(PlanogramOptResultsSettings results, bool enableSimDisplay)
        {
            enableSimDisplay = enableSimulation;
            Dispatcher.Invoke(() =>
            {

                if (enableSimDisplay)
                {
                    currentResults = results;
                    FillGrid(planogram, results.Shelves, false, ItemAttributes_MouseEnter, ItemAttributes_MouseLeave);
                }

                if (core.IsComparisonModeOn)
                {
                    DisplayLearningComparisonResults(tmpSelectedData, tmpComparisonData);
                }
                else
                {
                    string scoreText = (simSettings.SimType == SimulationType.Score) ? $"{results.Score.ToString("#,###.##")} ({results.NumScoreHits})" : results.Score.ToString("#,###.##");
                    scoreTxt.Text = scoreText;
                    sessionRunTxt.Text = results.CurrentSession.ToString();
                    timElapseTxt.Text = results.TimeElapsed.ToString();//string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}", results.TimeElapsed.Hours, results.TimeElapsed.Minutes, results.TimeElapsed.Seconds, results.TimeElapsed.Milliseconds);
                    minScoretxt.Text = results.MinScore.ToString("#,###.##");
                    maxScoreTxt.Text = results.MaxScore.ToString("#,###.##");
                    //engineTxt.Text = "RLM";
                    averageScoreTxt.Text = results.AvgScore.ToString("#,###.##");
                    averageScoreOf10Txt.Text = results.AvgLastTen.ToString("#,###.##");
                    currentRandomnessTxt.Text = results.CurrentRandomnessValue.ToString();
                    startRandomnessTxt.Text = results.StartRandomness.ToString();
                    endRandomnessTxt.Text = results.EndRandomness.ToString();
                    sessionPerBatchTxt.Text = results.SessionsPerBatch.ToString();
                    inputTypeTxt.Text = results.InputType;
                }
            });
        }

        private void UpdateRLMStatus(string statusMsg, bool isDone = false)
        {
            Dispatcher.Invoke(() =>
            {
                statusTxt.Text = statusMsg;
                isRLMDone = isDone;

                if (headToHead)
                {
                    if (isRLMDone)
                    {
                        Task.Run(() => {
                            Item[] items;
                            using (PlanogramContext ctx = new PlanogramContext())
                            {
                                MockData mock = new MockData(ctx);
                                items = mock.GetItemsWithAttr();
                                simSettings.ItemMetricMin = mock.GetItemMinimumScore(simSettings);
                                simSettings.ItemMetricMax = mock.GetItemMaximumScore(simSettings);
                            }

                            // let's tensorflow (or other listeners) know that it should start training
                            OnSimulationStart?.Invoke(items, simSettings);
                        });

                        if (isTensorflowDone)
                            EnableControlButtons(true);
                    }
                }
                else
                {
                    if (isRLMDone)
                        EnableControlButtons(true);
                }
            });
        }

        public void setSelectedPlanogramSize()
        {
            int count;
            using (PlanogramContext ctx = new PlanogramContext())
            {
                MockData data = new MockData(ctx);
                count = data.GetItemsCount();
            }

            if (count == SMALL_ITEMS_COUNT)
            {
                CbPlanogramSize.SelectedIndex = 0;
            }
            else if (count == LARGE_ITEMS_COUNT)
            {
                CbPlanogramSize.SelectedIndex = 1;
            }
            else
            {
                showDataPanelUntil();
            }
        }

        private void showDataPanelUntil()
        {
            var confirmation = MessageBox.Show("Warning! Please generate new data.", "Confirmation", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            if (confirmation == MessageBoxResult.OK)
            {
                showDataPanelDlg();
            }
        }



        private void FillGrid(Grid planogramGrid, Color color)
        {
            planogramGrid.Children.Clear();

            int slotNumber = 0;
            for (int row = 0; row < simSettings.NumShelves; row++)
            {
                for (int column = 0; column < simSettings.NumSlots; column++)
                {
                    slotNumber++;
                    Rectangle itemAttributes = new Rectangle();
                    itemAttributes.Fill = new SolidColorBrush(color);
                    itemAttributes.Stroke = new SolidColorBrush(Colors.Black);
                    //itemAttributes.HorizontalAlignment = HorizontalAlignment.Center;

                    if (slotNumber - 1 == selectedSlotIndex)
                    {
                        HighlightItem(itemAttributes);
                    }

                    Grid.SetColumn(itemAttributes, column);
                    Grid.SetRow(itemAttributes, row);
                    planogramGrid.Children.Add(itemAttributes);
                    
                }
            }
        }

        private NoDataNotificationWindow noData = null;
        private int previousSelectedSlotIndex = -1;
        private Rectangle previousSelectedRect = null;
        private int _row = 0;
        private int _col = 0;
        private void OnSelectedItem(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var point = Mouse.GetPosition(planogram);

            int row = 0;
            int col = 0;
            double accumulatedHeight = 0.0;
            double accumulatedWidth = 0.0;

            foreach (var rowDefinition in planogram.RowDefinitions)
            {
                accumulatedHeight += rowDefinition.ActualHeight;
                if (accumulatedHeight >= point.Y)
                {
                    break;
                }

                row++;
            }

            foreach (var columnDefinition in planogram.ColumnDefinitions)
            {
                accumulatedWidth += columnDefinition.ActualWidth;
                if (accumulatedWidth >= point.X)
                    break;
                col++;
            }

            RemoveHighlightedItems();

            // row and col now correspond Grid's RowDefinition and ColumnDefinition mouse was 
            // over when double clicked!     
            var rect = planogram.Children
                .Cast<Rectangle>()
                .Where(i => Grid.GetRow(i) == row && Grid.GetColumn(i) == col)
                .FirstOrDefault();

            if(rect == null)
            {
                row = itemRow;
                col = itemCol;

                rect = planogram.Children
                .Cast<Rectangle>()
                .Where(i => Grid.GetRow(i) == row && Grid.GetColumn(i) == col)
                .FirstOrDefault();
            }
           
            HighlightItem(rect);
            _row = row;
            _col = col;
            selectedSlotIndex = planogram.Children.IndexOf(rect);
            //MessageBox.Show($"row: {row}, col: {col}, index: {rectIndex}");

            var inputValues = new[]
            {
                new RLVIOValues() { IOName = "Slot", Value = (selectedSlotIndex + 1).ToString() }
            };

            // TODO remove this. for debugging purposes, we need hardcoded items since the planogram does not have items at startup
            //int[] preloadedItems = new int[] {61
            //    ,25
            //    ,71
            //    ,64
            //    ,32
            //    ,54
            //    ,67
            //    ,29
            //    ,89
            //    ,31
            //    ,20
            //    ,39
            //    ,76
            //    ,60
            //    ,55
            //    ,52
            //    ,16
            //    ,84
            //    ,76
            //    ,35
            //    ,74
            //    ,62
            //    ,37
            //    ,47};

            int itemIndex = 0;// preloadedItems[selectedSlotIndex]; 
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

                if (tmpPanel != null)
                {
                    tmpPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    if (tmpPanel != null)
                    {
                        tmpPanel.Close();
                    }
                    tmpPanel = new TempRLVContainerPanel(core, visualizer);
                    tmpPanel.Show();
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
            this.Top = 20;
            tmpPanel.Top = this.Top;

            tmpPanel.Left = 10;
            this.Left = tmpPanel.Left + tmpPanel.Width;
        }

        private void HighlightItem(int row, int col)
        {
            var rect = planogram.Children
                          .Cast<Rectangle>()
                          .Where(i => Grid.GetRow(i) == row && Grid.GetColumn(i) == col).FirstOrDefault();

            HighlightItem(rect);
        }

        private void HighlightItem(Rectangle rect)
        {
            rect.StrokeThickness = 2;
            rect.Stroke = new SolidColorBrush(Colors.GreenYellow);
        }

        private void RemoveHighlightedItems()
        {
            var shapes = planogram.Children
                          .Cast<Rectangle>();

            foreach (var shape in shapes)
            {
                shape.StrokeThickness = 1;
                shape.Stroke = new SolidColorBrush(Colors.Black);
                //shape.Tag = null;
            }
        }

        private void FillGrid(Grid planogramGrid, IEnumerable<Shelf> result, bool usePerfColor = false, MouseEventHandler mouseEnterHandler = null, MouseEventHandler mouseLeaveHandler = null, IEnumerable<Shelf> comparisonResult = null, bool isRLM = true)
        {
            if (result == null || planogramGrid == null)
                return;

            planogramGrid.Children.Clear();

            int slotNumber = 0;
            int row = 0;
            foreach (var shelf in result)
            {
                int col = 0;
                //bool highlight = false;
                foreach (var item in shelf.Items)
                {
                    slotNumber++;
                    Rectangle itemAttributes = new Rectangle();
                    itemAttributes.Fill = new SolidColorBrush(usePerfColor ? item.PerfColor : item.Color);
                    itemAttributes.Stroke = new SolidColorBrush(Colors.Black);
                    itemAttributes.Tag = item;

                    if (isRLM)
                    {
                        itemAttributes.InputBindings.Add(new MouseBinding()
                        {
                            Gesture = new MouseGesture(MouseAction.LeftClick),
                            Command = new ItemClickedCommand(() =>
                            {
                                OnSelectedItem(itemAttributes, null);
                            })
                        });
                    }

                    
                    if (comparisonResult == null)
                    {
                        if (mouseEnterHandler != null)
                            itemAttributes.MouseEnter += mouseEnterHandler;
                        if (mouseLeaveHandler != null)
                            itemAttributes.MouseLeave += mouseLeaveHandler;
                    }
                    else
                    {
                        var compItem = comparisonResult.ElementAt(row).Items.ElementAt(col);
                        
                        if (compItem.ItemID != item.ItemID)
                        {
                            itemAttributes.Stroke = new SolidColorBrush(Colors.White);
                            itemAttributes.StrokeThickness = 2;
                            itemAttributes.Tag = new KeyValuePair<ShelfItem, ShelfItem>(item, compItem);

                            //if (mouseEnterHandler != null)
                            //    itemAttributes.MouseEnter += mouseEnterHandler;
                            //if (mouseLeaveHandler != null)
                            //    itemAttributes.MouseLeave += mouseLeaveHandler;

                            itemAttributes.MouseEnter += (sender, e) =>
                            {
                                var itemPair = (KeyValuePair<ShelfItem, ShelfItem>)((Rectangle)sender).Tag;
                                //itemCompPanel.SetItems(itemPair.Key, itemPair.Value);

                                //if (!itemCompPanel.IsVisible)
                                //{
                                //    itemCompPanel.Show();
                                //    itemCompPanel.Left = this.Left + this.Width;
                                //    itemCompPanel.Top = this.Top;
                                //}

                                comparisonGrid.Visibility = Visibility.Visible;
                                setComparisonData(itemPair.Key, itemPair.Value);
                                itemScoreTxt.Text = itemPair.Value.Score.ToString("#,###.##");
                            };

                            itemAttributes.MouseLeave += (sender, e) =>
                            {
                                itemScoreTxt.Text = " ";
                                comparisonGrid.Visibility = Visibility.Hidden;
                                //if (itemCompPanel.IsVisible)
                                //{
                                //    itemCompPanel.Hide();
                                //}
                            };
                            //highlight = true;
                        }
                    }

                    if (slotNumber - 1 == selectedSlotIndex)
                    {
                        HighlightItem(itemAttributes);
                    }
                    
                    Grid.SetColumn(itemAttributes, col);
                    Grid.SetRow(itemAttributes, row);
                    planogramGrid.Children.Add(itemAttributes);

                    //if (highlight)
                        //HighlightItem(row, col);

                    col++;
                }
                row++;
            }
        }

        private ShelfItem currentItem;
        private ShelfItem prevItem;
        private void setComparisonData(ShelfItem current, ShelfItem prev)
        {
            currentItem = current;
            prevItem = prev;

            rectCurr.Fill = new SolidColorBrush(current.Color);
            txtCurrName.Text = current.Name;
            txtCurrScore.Text = current.Score.ToString("#,###.##");

            rectPrev.Fill = new SolidColorBrush(prev.Color);
            txtPrevName.Text = prev.Name;
            txtPrevScore.Text = prev.Score.ToString("#,###.##");
        }

        private void ItemAttributes_Compare_MouseEnter(object sender, MouseEventArgs e)
        {
            //var itemPair = (KeyValuePair<ShelfItem, ShelfItem>)((Rectangle)sender).Tag;
            //itemCompPanel.SetItems(itemPair.Key, itemPair.Value);

            //if (!itemCompPanel.IsVisible)
            //{
            //    itemCompPanel.Show();
            //    itemCompPanel.Left = this.Left + this.Width;
            //    itemCompPanel.Top = this.Top;
            //}

            //itemScoreTxt.Text =  itemPair.Value.Score.ToString("#,###.##");
        }
        
        private void ItemAttributes_MouseEnter(object sender, MouseEventArgs e)
        {           
            itemScoreTxt.Text = ((ShelfItem)((Rectangle)sender).Tag).Score.ToString("#,###.##");
        }
        private void ItemAttributes_MouseLeave(object sender, MouseEventArgs e)
        {
            //itemScoreTxt.Text = " ";
            //if (itemCompPanel.IsVisible)
            //{
            //    itemCompPanel.Hide();
            //}
        }

        private void ItemAttrbutes_MouseEnter_Tensorflow(object sender, MouseEventArgs e)
        {
            itemScoreTxtTensor.Text = ((ShelfItem)((Rectangle)sender).Tag).Score.ToString("#,###.##");
        }

        private void ItemAttrbutes_MouseLeave_Tensorflow(object sender, MouseEventArgs e)
        {
            itemScoreTxtTensor.Text = " ";
        }

        private void toggleColorBtn_Click(object sender, RoutedEventArgs e)
        {
            usePerfColor = !usePerfColor;
            FillGrid(planogram, currentResults.Shelves, usePerfColor, ItemAttributes_MouseEnter, ItemAttributes_MouseLeave);
            if (headToHead)
            {
                FillGrid(planogramTensorflow, currentResultsTensor.Shelves, usePerfColor, ItemAttrbutes_MouseEnter_Tensorflow, ItemAttrbutes_MouseLeave_Tensorflow, isRLM:false);
            }
        }

        public void UpdateTensorflowResults(PlanogramOptResults results, bool enableSimDisplay = false)
        {
            Dispatcher.Invoke(() =>
            {
                string scoreText = (simSettings.SimType == SimulationType.Score) ? $"{results.Score.ToString("#,###.##")} ({results.NumScoreHits})" : results.Score.ToString("#,###.##");
                scoreTxtTensor.Text = scoreText;
                sessionRunTxtTensor.Text = results.CurrentSession.ToString();
                timElapseTxtTensor.Text = results.TimeElapsed.ToString();//string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}", results.TimeElapsed.Hours, results.TimeElapsed.Minutes, results.TimeElapsed.Seconds, results.TimeElapsed.Milliseconds);
                minScoreTxtTensor.Text = results.MinScore.ToString("#,###.##");
                maxScoreTxtTensor.Text = results.MaxScore.ToString("#,###.##");
                //engineTxtTensor.Text = "Tensorflow";
                averageScoreTxtTensor.Text = results.AvgScore.ToString("#,###.##");
                averageScoreOf10TxtTensor.Text = results.AvgLastTen.ToString("#,###.##");

                if (enableSimDisplay)
                {
                    currentResultsTensor = results;
                    FillGrid(planogramTensorflow, results.Shelves, false, ItemAttrbutes_MouseEnter_Tensorflow, ItemAttrbutes_MouseLeave_Tensorflow, isRLM:false);
                }
            });
        }

        public void UpdateTensorflowStatus(string statusMsg, bool isDone = false)
        {
            Dispatcher.Invoke(() =>
            {
                statusTxtTensor.Text = statusMsg;
                isTensorflowDone = isDone;

                if (isTensorflowDone && isRLMDone)
                    EnableControlButtons(true);
            });
        }

        public void AddLogDataTensorflow(int session, double score, double seconds)
        {
            Dispatcher.Invoke(() =>
            {
                SimulationData logData = new Models.SimulationData();

                logData.Score = score;
                logData.Session = session;
                logData.Elapse = TimeSpan.FromSeconds(seconds);

                Logger.Add(logData, true);
            });
        }

        private void CbPlanogramSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var a = sender as ComboBox;

            var s = a.SelectedItem as ComboBoxItem;
            var scale = a.SelectedItem as Scale;
            var i = scale.Name;

            if (i == "Small")
            {
                this.simSettings.NumItems = SMALL_ITEMS_COUNT;
                this.simSettings.NumShelves = 1;
                this.simSettings.NumSlots = 24;

                this.dataPanel.NumItems = SMALL_ITEMS_COUNT;
                this.dataPanel.NumShelves = 1;
                this.dataPanel.NumSlots = 24;
            }
            else if (i == "Large")
            {
                this.simSettings.NumItems = LARGE_ITEMS_COUNT;
                this.simSettings.NumShelves = 12;
                this.simSettings.NumSlots = 24;

                this.dataPanel.NumItems = LARGE_ITEMS_COUNT;
                this.dataPanel.NumShelves = 12;
                this.dataPanel.NumSlots = 24;
            }

            if (headToHead)
            {
                FillGrid(planogram, Colors.LightGray);
                FillGrid(planogramTensorflow, Colors.LightGray);
            }
            else
            {
                FillGrid(planogram, Colors.LightGray);
            }

            int count;
            using (PlanogramContext ctx = new PlanogramContext())
            {
                MockData data = new MockData(ctx);
                count = data.GetItemsCount();
            }

            if (count != simSettings.NumItems)
            {
                showDataPanelUntil();
            }
        }

        private void rlmCsvBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saver = new SaveFileDialog();
            saver.Filter = "Csv file (*.csv)|*.csv";
            saver.FileName = "RLM_Results";
            if (saver.ShowDialog() == true)
            {
                var pathToFile = saver.FileName;

                Logger.ToCsv(pathToFile);
            }
        }

        private void tfCsvBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saver = new SaveFileDialog();
            saver.Filter = "Csv file (*.csv)|*.csv";
            saver.FileName = "TF_Results";
            if (saver.ShowDialog() == true)
            {
                var pathToFile = saver.FileName;

                Logger.ToCsv(pathToFile, true);
            }
        }

        private TempRLVContainerPanel tmpPanel;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.CbPlanogramSize.Items.Clear();
            this.CbPlanogramSize.ItemsSource = new List<Scale>
            {
                new Scale { ID = 1, Name = "Small", Description = "Test Scale Only (100 items, 24 slots)" },
                new Scale { ID = 2, Name = "Large", Description = "Production Scale (5000 items, 288 slots)" }
            };
            this.CbPlanogramSize.DisplayMemberPath = "NameDescription";
            setSelectedPlanogramSize();
            
            // TEMPORARY, for now we get the items at start up for debugging purposes
            using (PlanogramContext ctx = new PlanogramContext())
            {
                MockData mock = new MockData(ctx);
                itemsCache = mock.GetItemsWithAttr();
                simSettings.ItemMetricMin = mock.GetItemMinimumScore(simSettings);
                simSettings.ItemMetricMax = mock.GetItemMaximumScore(simSettings);
            }
        }
        private int itemRow = -1;
        private int itemCol = -1;
        private RlmLearnedSessionDetails tmpSelectedData;
        private RlmLearnedSessionDetails tmpComparisonData;
        public void DisplayLearningComparisonResults(RlmLearnedSessionDetails selectedData, RlmLearnedSessionDetails comparisonData)
        {
            if (enableSimulation)
            {
                tmpSelectedData = selectedData;
                tmpComparisonData = comparisonData;
            }
            comparisonOverlay.Visibility = Visibility.Visible;
            enableSimulation = false;

            IEnumerable<Shelf> selectedPlanogram = GetShelfData(selectedData);
            IEnumerable<Shelf> comparisonPlanogram = null;
            if (comparisonData != null)
            {
                comparisonPlanogram = GetShelfData(comparisonData);
            }

            FillGrid(planogram, selectedPlanogram, false, mouseEnterHandler: ItemAttributes_Compare_MouseEnter, mouseLeaveHandler: ItemAttributes_MouseLeave, comparisonResult: comparisonPlanogram);

            if(!btnComparisonClose.IsEnabled == true)
            {
                itemRow = _row;
                itemCol = _col;
            }

            //btnComparisonClose.Visibility = Visibility.Visible;
            btnComparisonClose.IsEnabled = true;

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


        protected override void OnClosed(EventArgs e)
        {
            if(tmpPanel != null)
            {
                tmpPanel.Close();
            }

            base.OnClosed(e);
            Application.Current.Shutdown();
        }

        private void imgBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Process wordProcess = new Process();
            wordProcess.StartInfo.FileName = $"{HelpPath}Retail POC How to.docx";
            wordProcess.StartInfo.UseShellExecute = true;
            wordProcess.Start();
        }

        private void btnComparisonClose_Click(object sender, RoutedEventArgs e)
        {
            comparisonOverlay.Visibility = Visibility.Hidden;
            enableSimulation = true;
            visualizer.CloseComparisonMode();
            FillGrid(planogram, currentResults.Shelves, false, ItemAttributes_MouseEnter, ItemAttributes_MouseLeave);
            //FillGrid(planogram, Colors.Gray);

            //btnComparisonClose.Visibility = Visibility.Hidden;
            btnComparisonClose.IsEnabled = false;

            if (itemCompPanel.IsVisible)
            {
                itemCompPanel.Hide();
            }

            RemoveHighlightedItems();

            // row and col now correspond Grid's RowDefinition and ColumnDefinition mouse was 
            // over when double clicked!            
            var rect = planogram.Children
                .Cast<Rectangle>()
                .Where(i => Grid.GetRow(i) == itemRow && Grid.GetColumn(i) == itemCol)
                .FirstOrDefault();

            HighlightItem(rect);
            OnSelectedItem(rect, null);
        }

        private void MetroWindow_LocationChanged(object sender, EventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("Hello World");
        }

        private void MetroWindow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //if (tmpPanel != null)
            //{
            //    //tmpPanel.detailsControl.LoadConfigurationPanel(this);
            //    RLVConfigurationPanel vsConfig = new RLVConfigurationPanel(tmpPanel.progressionChartControl, tmpPanel.detailsControl);
            //    vsConfig.Show();
            //    vsConfig.Top = this.Top;
            //    vsConfig.Left = this.Left + this.Width;
            //}
        }

        private void closeComparisonLink_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            btnComparisonClose_Click(null, null);
        }
    }
}

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
using System.Threading;
using PoCTools.Settings;

namespace RetailPoC
{
    public delegate void SimulationStart(Item[] items, RPOCSimulationSettings simSettings, CancellationToken token);

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        const int SMALL_ITEMS_COUNT = 100;
        const int LARGE_ITEMS_COUNT = 5000;
        const int SHELVES = 12;
        const int SLOTS_PER_SHELF = 24;
        readonly PlanogramSize[] planogramSizes;
        private PlanogramSize currPlanogramSize;

        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        private DataFactory dataFactory = new DataFactory();
        
        public string ENCOG_ACTIVATION_FUNCTION = "Elliot Symmetric";
        public string ENCOG_NETWORK_PATTERN = "Feed Forward";
        public int ENCOG_HIDDEN_LAYERS = 1;
        public int ENCOG_HIDDEN_LAYER_NEURONS = 200;
        public string ENCOG_TRAINING_METHOD = "Genetic Algorithm";

        public string TF_ACTIVATION_FUNCTION = "Sigmoid";
        public int TF_HIDDEN_LAYERS = 3;
        public int TF_HIDDEN_LAYER_NEURONS = 500;
        public string TF_OPTIMIZER = "Gradient Descent";

        private RPOCSimulationSettings simSettings = new RPOCSimulationSettings()
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
        public PlanogramOptResults CurrentResults { get; set; }
        private bool isRLMDone = false;
        private int selectedSlotIndex = -1;
        private int preCompareSelectedSlotIndex = -1;

        private ItemComparisonPanel itemCompPanel = new ItemComparisonPanel();
        
        private IRLVCore core = new RLVCore("RLV_small");
        private IRLVPlangoramOutputVisualizer visualizer = null;

        private IDictionary<Color, SolidColorBrush> coloredBrushesDict = new Dictionary<Color, SolidColorBrush>();
        private PlanogramOptimizer optimizer;

        private NoDataNotificationWindow noData = null;
        private int previousSelectedSlotIndex = -1;
        private Border previousSelectedRect = null;

        private TempRLVContainerPanel rlvPanel;
        private RLVConfigurationPanel vsConfig = null;

        private ShelfItem currentItem;
        private ShelfItem prevItem;
        
        private RlmLearnedSessionDetails tmpSelectedData;
        private RlmLearnedSessionDetails tmpComparisonData;

        public event SimulationStart OnSimulationStart;

        public SimulationCsvLogger Logger { get; set; } = new SimulationCsvLogger();
        public string HelpPath { get; set; } = AppDomain.CurrentDomain.BaseDirectory;
        public RPOCSimulationSettings SimulationSettings { get { return simSettings; } }
        public Item[] ItemsCache { get { return itemsCache; } }
        public bool NoData { get; set; } = true;
        private EngineTrainingWindow trainingWindow;
        private bool enableTensorflowTraining = false;

        public MainWindow(bool enableTensorflow = false, bool htoh = false)
        {
            startupWin = new StartupWindow();
            ShowStartupWindow(true, "Connecting to SQL Server...");

            InitializeComponent();
            headToHead = htoh;
            enableTensorflowTraining = enableTensorflow;

            // set planogram sizes
            planogramSizes = new PlanogramSize[10];
            double defaultScorePercentage = 85;
            for (int shelf = 10 - 1; shelf >= 0; shelf--)
            {
                simSettings.DefaultScorePercentage = defaultScorePercentage;
                var curr = planogramSizes[9 - shelf] = new PlanogramSize() { Name = $"Planogram {shelf + 1}", Shelves = shelf + 3, SlotsPerShelf = SLOTS_PER_SHELF, ItemsCount = 500 * (shelf + 1), Metrics = shelf + 1, BaseScoringPercentage =  simSettings.DefaultScorePercentage};
                cmbPlanogramSize.Items.Add(curr.ToString());

                if (shelf == SHELVES - 1)
                {
                    currPlanogramSize = curr;
                }

                defaultScorePercentage -= 1;
            }

            if (enableTensorflow)
            {
                rdbTensorflow.ToolTip = "";
                rdbTensorflow.IsChecked = true;
                grpMLSettings.Visibility = Visibility.Hidden;
                grpTFSettings.Visibility = Visibility.Visible;
                RLMsettingGroup.Visibility = Visibility.Hidden;
                rdbRLM.IsEnabled = false;
                rdbEncog.IsEnabled = false;
            }
            else
            {
                rdbTensorflow.ToolTip = "Tensorflow can only be run from a separate project folder '/ExampleApps/CompetitorComparableApps/RetailPoCIBM/RetailPoCIBM_Tensorflow'";
                rdbTensorflow.IsEnabled = false;                
            }

            if (CheckProducts() == false)
            {
                ProductCheck.Visibility = Visibility.Hidden;
                RandomMetricCheck.Visibility = Visibility.Hidden;
                ConstraintsCheck.Visibility = Visibility.Hidden;
            }
            else
            {
                ProductCheck.Visibility = Visibility.Visible;
                RandomMetricCheck.Visibility = Visibility.Visible;
                ConstraintsCheck.Visibility = Visibility.Visible;
            }

            CompleteCheck.Visibility = Visibility.Hidden;
            StartTrainingWarning.Visibility = Visibility.Visible;
            StartTrainingCheck.Visibility = Visibility.Hidden;

            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            tokenSource.Cancel();
        }

        private void dataGenerationBtn_Click(object sender, RoutedEventArgs e)
        {
            showDataPanelDlg();
        }

        private void showDataPanelDlg()
        {
            var dataPanel = new DataPanel();

            dataPanel.GenerateDataEvent += DataPanel_GenerateDataEvent;
            
            dataPanel.NumItems = currPlanogramSize.ItemsCount;
            dataPanel.NumShelves = currPlanogramSize.Shelves;
            dataPanel.NumSlots = currPlanogramSize.SlotsPerShelf;

            dataPanel.Closed += (s, ee) => {
                dataPanel = new DataPanel();
                setSelectedPlanogramSize();
            };

            dataPanel.NumItems = simSettings.NumItems;
            dataPanel.NumShelves = simSettings.NumShelves;
            dataPanel.NumSlots = simSettings.NumSlots;

            dataPanel.dataGrid.Loaded += (ss, evv) => {

                bool exist = CheckProducts();

                if (exist)
                {
                    dataPanel.dataGrid.ItemsSource = dataFactory.Items; // Setting data source for the DataPanel's datagrid
                    dataPanel.Items = dataFactory.Items; // Store the items to a new List for searching purposes
                    dataPanel.dataGrid.AutoGenerateColumns = false;

                    // Binding and settings of datagrid columns
                    dataPanel.dataGrid.Columns.Add(new DataGridTextColumn() { Header = "ID", Binding = new Binding("ID"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), IsReadOnly = true });
                    dataPanel.dataGrid.Columns.Add(new DataGridTextColumn() { Header = "SKU", Binding = new Binding("SKU"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), IsReadOnly = true });
                    dataPanel.dataGrid.Columns.Add(new DataGridTextColumn() { Header = "Name", Binding = new Binding("Name"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), IsReadOnly = true });

                    ProductCheck.Visibility = Visibility.Visible;
                    RandomMetricCheck.Visibility = Visibility.Visible;
                    ConstraintsCheck.Visibility = Visibility.Visible;
                }
            };

           dataPanel.ShowDialog();

        }

        private void DataPanel_GenerateDataEvent(string msg)
        {
            startupWin.startupWindowMsg.Document.Blocks.Clear();
            startupWin.startupWindowMsg.AppendText(msg);
        }

        private bool CheckProducts()
        {
            var retVal = false;
            using (PlanogramContext ctx = new PlanogramContext())
            {
                retVal = ctx.Database.Exists();
            }

            return retVal;
        }

        private void metricsBtn_Click(object sender, RoutedEventArgs e)
        {
            metricsBtnFunction();
        }


        private MetricPanel metricPanel;
        private void metricsBtnFunction()
        {
            var selectedMetrics = metricPanel.SelectedMetrics;
            metricPanel = new MetricPanel(selectedMetrics);
            //metricPanel.SelectedMetrics = selectedMetrics;
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

        private void runSlmBtn_Click(object sender, RoutedEventArgs e)
        {
            runSlmBtnFunction();
        }

        private void runSlmBtnFunction()
        {
            selectedSlotIndex = -1;            

            SimulationPanel simPanel = new SimulationPanel();
            simPanel.SetSimSettings(simSettings);
            bool? result = simPanel.ShowDialog();

            CompleteCheck.Visibility = Visibility.Hidden;

            if (result.HasValue && result.Value == true)
            {
                // disable control buttons
                EnableControlButtons(false);

                // set simulation settings
                simSettings.SimType = simPanel.SimType;
                simSettings.Sessions = simPanel.Sessions;
                simSettings.Hours = simPanel.Hours;
                simSettings.Score = simPanel.Score;
                simSettings.EnableSimDisplay = simPanel.EnableSimDisplay;
                simSettings.DefaultScorePercentage = simPanel.simScoreSlider.Value;

                simSettings.HiddenLayers = ENCOG_HIDDEN_LAYERS;
                //simSettings.HiddenLayers = simPanel.HiddenLayers;

                simSettings.HiddenLayerNeurons = ENCOG_HIDDEN_LAYER_NEURONS;
                //simSettings.HiddenLayerNeurons = simPanel.HiddenLayerNeurons;

                targetScoreTxt.Text = "";
                if (simSettings.SimType == SimulationType.Score)
                {
                    targetScoreLbl.Visibility = Visibility.Visible;
                    targetScoreTxt.Visibility = Visibility.Visible;
                    targetScoreTxt.Text = simSettings.Score.Value.ToString("n");
                }
                else
                {
                    targetScoreLbl.Visibility = Visibility.Hidden;
                    targetScoreTxt.Visibility = Visibility.Hidden;
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

                if (rlvPanel != null)
                {
                    rlvPanel.Close();
                }

                rlvPanel = new TempRLVContainerPanel(core, visualizer);
                OpenTrainingWindow();

                bool doRlm = rdbRLM.IsChecked.HasValue && rdbRLM.IsChecked.Value;
                bool doEncog = rdbEncog.IsChecked.HasValue && rdbEncog.IsChecked.Value;

                Task.Run(() =>
                {
                    try
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

                        if (doRlm)
                        {
                            // initialize and start RLM training
                            optimizer = new PlanogramOptimizer(items, simSettings, this.UpdateRLMResults, this.UpdateRLMStatus, Logger, dbIdentifier);
                            optimizer.StartOptimization(tokenSource.Token);
                        }
                        else if (doEncog)
                        {
                            var encogOpt = new PlanogramOptimizerEncog(itemsCache, simSettings, UpdateTensorflowResults, UpdateTensorflowStatus, Logger, false);
                            encogOpt.StartOptimization(tokenSource.Token);
                        }
                        else // do tensorflow
                        {
                            // let's tensorflow (or other listeners) know that it should start training
                            OnSimulationStart?.Invoke(items, simSettings, tokenSource.Token);
                        }
                    }
                    catch(Exception ex)
                    {
                        ShowStartupWindow(true, $"ERROR: {ex.Message}");
                    }
                });

                StartTrainingCheck.Visibility = Visibility.Visible;
                StartTrainingWarning.Visibility = Visibility.Hidden;
            }
        }

        private void EnableControlButtons(bool value)
        {
            rdbEncog.IsEnabled = value;
            rdbRLM.IsEnabled = value;
            StartFlowChart.IsEnabled = value;
            CreateRetailConstraints.IsEnabled = value;
            GenerateRandomProducts.IsEnabled = value;

            if (enableTensorflowTraining)
            {
                rdbTensorflow.IsEnabled = value;
            }
        }

        private void UpdateRLMResults(PlanogramOptResultsSettings results, bool enableSimDisplay)
        {
            if (tokenSource.IsCancellationRequested) return;
            if (enableSimDisplay)
                Task.Delay(1).Wait();

            Dispatcher.Invoke(() =>
            {
                CurrentResults = results;

                if (RLMsettingGroup.Visibility != Visibility.Visible)
                {
                    RLMsettingGroup.Visibility = Visibility.Visible;
                    grpMLSettings.Visibility = Visibility.Hidden;
                }

                if (enableSimDisplay && !core.IsComparisonModeOn)
                {
                    FillGrid(trainingWindow.planogram, results.Shelves, false, ItemAttributes_MouseEnter, ItemAttributes_MouseLeave);
                }

                if (core.IsComparisonModeOn)
                {
                    DisplayLearningComparisonResults(tmpSelectedData, tmpComparisonData);
                }
                else
                {
                    string scoreText = (simSettings.SimType == SimulationType.Score) ? $"{results.Score.ToString("#,##0.##")} ({results.NumScoreHits})" : results.Score.ToString("#,##0.##");
                    scoreTxt.Text = scoreText;
                    sessionRunTxt.Text = results.CurrentSession.ToString();
                    timElapseTxt.Text = results.TimeElapsed.ToString();
                    minScoretxt.Text = results.MinScore.ToString("#,##0.##");
                    maxScoreTxt.Text = results.MaxScore.ToString("#,##0.##");
                    averageScoreTxt.Text = results.AvgScore.ToString("#,##0.##");
                    averageScoreOf10Txt.Text = results.AvgLastTen.ToString("#,##0.##");

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
            if (tokenSource.IsCancellationRequested) return;

            Dispatcher.Invoke(() =>
            {
                statusTxt.Text = statusMsg;
                isRLMDone = isDone;

                if (isRLMDone)
                {
                    CompleteCheck.Visibility = Visibility.Visible;
                    EnableControlButtons(true);
                }
            });
        }

        private PlanogramSize prevPlanogramSize;
        private void SetSelectedMetrics(int planogramSizeIndex)
        {
            metricPanel.SelectedMetrics = new List<string>();
            int TEMP = 10;
            switch (planogramSizeIndex)
            {
                case 9:
                    for(var i = 0; i < /*1*/TEMP; i++)
                    {
                        metricPanel.SelectedMetrics.Add($"Metric{i + 1}");
                    }
                    break;
                case 8:
                    for (var i = 0; i < /*2*/TEMP; i++)
                    {
                        metricPanel.SelectedMetrics.Add($"Metric{i + 1}");
                    }
                    break;
                case 7:
                    for (var i = 0; i < /*3*/TEMP; i++)
                    {
                        metricPanel.SelectedMetrics.Add($"Metric{i + 1}");
                    }
                    break;
                case 6:
                    for (var i = 0; i < /*4*/TEMP; i++)
                    {
                        metricPanel.SelectedMetrics.Add($"Metric{i + 1}");
                    }
                    break;
                case 5:
                    for (var i = 0; i < /*5*/TEMP; i++)
                    {
                        metricPanel.SelectedMetrics.Add($"Metric{i + 1}");
                    }
                    break;
                case 4:
                    for (var i = 0; i < /*6*/TEMP; i++)
                    {
                        metricPanel.SelectedMetrics.Add($"Metric{i + 1}");
                    }
                    break;
                case 3:
                    for (var i = 0; i < /*7*/TEMP; i++)
                    {
                        metricPanel.SelectedMetrics.Add($"Metric{i + 1}");
                    }
                    break;
                case 2:
                    for (var i = 0; i < /*8*/TEMP; i++)
                    {
                        metricPanel.SelectedMetrics.Add($"Metric{i + 1}");
                    }
                    break;
                case 1:
                    for (var i = 0; i < /*9*/TEMP; i++)
                    {
                        metricPanel.SelectedMetrics.Add($"Metric{i + 1}");
                    }
                    break;
                case 0:
                    for (var i = 0; i < 10; i++)
                    {
                        metricPanel.SelectedMetrics.Add($"Metric{i + 1}");
                    }
                    break;
            }

            SetMetricValues();
        }

        private void SetMetricValues()
        {
            ResetMetricValues();

            double percentageTotal = 100;
            double metricCount = metricPanel.SelectedMetrics.Count;
            double metricVal = percentageTotal / metricCount;
            foreach (var m in metricPanel.SelectedMetrics)
            {
                switch (m)
                {
                    case "Metric1":
                        simSettings.Metric1 = metricVal;
                        break;
                    case "Metric2":
                        simSettings.Metric2 = metricVal;
                        break;
                    case "Metric3":
                        simSettings.Metric3 = metricVal;
                        break;
                    case "Metric4":
                        simSettings.Metric4 = metricVal;
                        break;
                    case "Metric5":
                        simSettings.Metric5 = metricVal;
                        break;
                    case "Metric6":
                        simSettings.Metric6 = metricVal;
                        break;
                    case "Metric7":
                        simSettings.Metric7 = metricVal;
                        break;
                    case "Metric8":
                        simSettings.Metric8 = metricVal;
                        break;
                    case "Metric9":
                        simSettings.Metric9 = metricVal;
                        break;
                    case "Metric10":
                        simSettings.Metric10 = metricVal;
                        break;
                }
            }
        }

        private void ResetMetricValues()
        {
            simSettings.Metric1 = 0;
            simSettings.Metric2 = 0;
            simSettings.Metric3 = 0;
            simSettings.Metric4 = 0;
            simSettings.Metric5 = 0;
            simSettings.Metric6 = 0;
            simSettings.Metric7 = 0;
            simSettings.Metric8 = 0;
            simSettings.Metric9 = 0;
            simSettings.Metric10 = 0;
        }

        public void setSelectedPlanogramSize()
        {
            int count;
            using (PlanogramContext ctx = new PlanogramContext())
            {
                MockData data = new MockData(ctx);
                count = data.GetItemsCount();
            }

            if (currPlanogramSize == null)
            {
                int index = -1;
                PlanogramSize planogramSize = null;
                for (int i = 0; i < planogramSizes.Length; i++)
                {
                    if (planogramSizes[i].ItemsCount == count)
                    {
                        index = i;
                        planogramSize = planogramSizes[i];
                        break;
                    }
                }

                if (planogramSize != null)
                {
                    currPlanogramSize = planogramSize;
                    cmbPlanogramSize.SelectedIndex = index;
                    prevPlanogramSize = currPlanogramSize;
                }
                else
                {
                    cmbPlanogramSize.SelectedIndex = 0;
                }
            }
            else
            {
                if (count != currPlanogramSize.ItemsCount && currPlanogramSize == prevPlanogramSize)
                {
                    showDataPanelUntil();
                }
                else
                {
                    if (prevPlanogramSize != null && count == prevPlanogramSize.ItemsCount)
                    {
                        currPlanogramSize = prevPlanogramSize;
                        cmbPlanogramSize.SelectedIndex = planogramSizes.ToList().IndexOf(currPlanogramSize);
                    }
                    else
                    {
                        prevPlanogramSize = currPlanogramSize;
                    }
                }
            }

            if(cmbPlanogramSize.SelectedIndex >= 0)
            {
                SetSelectedMetrics(cmbPlanogramSize.SelectedIndex);
                simSettings.DefaultScorePercentage = currPlanogramSize.BaseScoringPercentage;
            }
        }

        private void showDataPanelUntil()
        {
            var confirmation = MessageBox.Show("Warning! Please generate new data.", "Confirmation", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
            if (confirmation == MessageBoxResult.OK)
            {
                showDataPanelDlg();
            }
            else
            {
                if (prevPlanogramSize != null)
                {
                    currPlanogramSize = prevPlanogramSize;
                    cmbPlanogramSize.SelectedIndex = planogramSizes.ToList().IndexOf(currPlanogramSize);
                }
                else
                {
                    Application.Current.Shutdown();
                }
            }
        }
                
        private void OnSelectedItem(Border sender)
        {
            try
            {
                if (rdbRLM.IsChecked.Value == true)
                {
                    var point = Mouse.GetPosition(trainingWindow.planogram);

                    Border txtBorder = sender;

                    RemoveHighlightedItems();
                    HighlightItem(txtBorder);

                    selectedSlotIndex = trainingWindow.planogram.Children.IndexOf(txtBorder);

                    var inputValues = new[]
                    {
                    new RLVIOValues() { IOName = "Slot", Value = (selectedSlotIndex + 1).ToString() }
                };

                    int itemIndex = 0;
                    TextBlock child = txtBorder.Child as TextBlock;

                    if (child.Tag is ShelfItem)
                    {
                        itemIndex = ((ShelfItem)child.Tag).Index - 1;
                    }
                    else if (child.Tag is KeyValuePair<ShelfItem, ShelfItem>)
                    {
                        itemIndex = ((KeyValuePair<ShelfItem, ShelfItem>)child.Tag).Key.Index - 1;
                    }

                    var outputValues = new[]
                    {
                    new RLVIOValues() { IOName = "Item", Value = itemIndex.ToString() }
                };

                    var itemDisplay = new[]
                    {
                    new RLVItemDisplay() { Name = "Item", Value = "Item #" + (selectedSlotIndex + 1), Visibility = RLVVisibility.Visible } // todo pass in actual item name (see: Value)
                };

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

                        previousSelectedSlotIndex = selectedSlotIndex;
                        previousSelectedRect = txtBorder;
                        AlignPanels();
                    }
                    else
                    {
                        if (noData != null)
                        {
                            noData.Close();
                        }
                        noData = new NoDataNotificationWindow();
                        noData.Show();
                        noData.Top = rlvPanel.Top;
                        noData.Left = rlvPanel.Left + rlvPanel.Width;

                        selectedSlotIndex = previousSelectedSlotIndex;

                        // Highlight the previous item with data
                        if (txtBorder != null)
                        {
                            //txtBorder.BorderBrush = GetBrushFromColor(Colors.Black);
                        }

                        if (previousSelectedRect != null)
                        {
                            HighlightItem(previousSelectedRect);
                        }
                    }

                    visualizer?.SelectNewItem(inputValues, outputValues, itemDisplay);
                }
            }
            catch (Exception ex)
            {
                ShowStartupWindow(true, $"ERROR: {ex.Message}");
            }
        }

        public void AlignPanels()
        {
            if (rlvPanel != null)
            {
                rlvPanel.Top = trainingWindow.Top;
                rlvPanel.Left = trainingWindow.Left - rlvPanel.Width;
            }
        }

        private void HighlightItem(Rectangle rect)
        {
            rect.StrokeThickness = 2;
            rect.Stroke = GetBrushFromColor(Colors.GreenYellow);
        }

        private void HighlightItem(Border txtBorder)
        {
            txtBorder.BorderBrush = new SolidColorBrush(Colors.GreenYellow);
            txtBorder.BorderThickness = new Thickness(2);
        }

        private void RemoveHighlightedItems()
        {
            var shapes = trainingWindow.planogram.Children
                          .Cast<Border>();

            foreach (var shape in shapes)
            {
                shape.BorderBrush = new SolidColorBrush(Colors.White);
                shape.BorderThickness = new Thickness(0);
            }
        }
        
        public void InitializeGrid(Grid planogramGrid, Color color)
        {
            planogramGrid.RowDefinitions.Clear();
            planogramGrid.ColumnDefinitions.Clear();
            planogramGrid.Children.Clear();

            // recreate grid definitions
            for (int row = 0; row < simSettings.NumShelves; row++)
            {
                RowDefinition rowDef = new RowDefinition() { Height = new GridLength(40) };
                planogramGrid.RowDefinitions.Add(rowDef);
            }
            for (int column = 0; column < simSettings.NumSlots; column++)
            {
                ColumnDefinition colDef = new ColumnDefinition() { Width = new GridLength(40) };
                planogramGrid.ColumnDefinitions.Add(colDef);
            }

            // create text and borders
            int slotNumber = 0;
            for (int row = 0; row < simSettings.NumShelves; row++)
            {
                for (int column = 0; column < simSettings.NumSlots; column++)
                {
                    Border txtBorder = new Border();
                    TextBlock txtItem = new TextBlock();
                    txtItem.Tag = null;
                    txtItem.TextAlignment = TextAlignment.Center;
                    txtItem.VerticalAlignment = VerticalAlignment.Center;

                    txtItem.InputBindings.Add(new MouseBinding()
                    {
                        Gesture = new MouseGesture(MouseAction.LeftClick),
                        Command = new ItemClickedCommand(() =>
                        {
                            OnSelectedItem(txtBorder);
                        })
                    });

                    //txtBorder.BorderBrush = GetBrushFromColor(Colors.Black);
                    //txtBorder.BorderThickness = new Thickness(1);
                    txtBorder.Child = txtItem;

                    if (slotNumber == selectedSlotIndex)
                    {
                        HighlightItem(txtBorder);
                    }

                    Grid.SetColumn(txtBorder, column);
                    Grid.SetRow(txtBorder, row);
                    planogramGrid.Children.Add(txtBorder);

                    slotNumber++;
                }
            }
        }

        private void FillGrid(Grid planogramGrid, Color color)
        {
            int slotNumber = 0;
            foreach (var txtBorder in planogramGrid.Children.Cast<Border>())
            {
                txtBorder.BorderBrush = GetBrushFromColor(color);
                TextBlock txtItem = txtBorder.Child as TextBlock;
                txtItem.Tag = null;

                txtItem.MouseEnter -= ItemAttributes_Compare_MouseEnter;
                txtItem.MouseEnter -= ItemAttributes_MouseEnter;

                txtItem.MouseLeave -= ItemAttributes_MouseLeave;

                if (slotNumber == selectedSlotIndex)
                {
                    HighlightItem(txtBorder);
                }

                slotNumber++;
            }
        }
        
        public void FillGrid(Grid planogramGrid, IEnumerable<Shelf> result, bool usePerfColor = false, MouseEventHandler mouseEnterHandler = null, MouseEventHandler mouseLeaveHandler = null, IEnumerable<Shelf> comparisonResult = null, bool isRLM = true)
        {
            if (result == null || planogramGrid == null)
                return;

            var itemRectangles = planogramGrid.Children.Cast<Border>();

            int slotNumber = 0;
            int row = 0;
            foreach (var shelf in result)
            {
                int col = 0;
                foreach (var item in shelf.Items)
                {
                    Border txtBorder = itemRectangles.ElementAt(slotNumber);
                    TextBlock txtItem = txtBorder.Child as TextBlock;

                    txtItem.Text = item.SKU;
                    txtItem.Tag = item;

                    txtItem.MouseEnter -= ItemAttributes_Compare_MouseEnter;
                    txtItem.MouseEnter -= ItemAttributes_MouseEnter;

                    txtItem.MouseLeave -= ItemAttributes_Compare_MouseLeave;
                    txtItem.MouseLeave -= ItemAttributes_MouseLeave;

                    if (comparisonResult == null)
                    {
                        if (mouseEnterHandler != null)
                            txtItem.MouseEnter += mouseEnterHandler;
                        if (mouseLeaveHandler != null)
                            txtItem.MouseLeave += mouseLeaveHandler;
                    }
                    else
                    {
                        var compItem = comparisonResult.ElementAt(row).Items.ElementAt(col);
                        
                        if (compItem.ItemID != item.ItemID)
                        {
                            txtBorder.BorderBrush = GetBrushFromColor(Colors.DarkOrange);
                            txtBorder.BorderThickness = new Thickness(2);
                            txtItem.Tag = new KeyValuePair<ShelfItem, ShelfItem>(item, compItem);

                            if (mouseEnterHandler != null)
                                txtItem.MouseEnter += mouseEnterHandler;
                            if (mouseLeaveHandler != null)
                                txtItem.MouseLeave += mouseLeaveHandler;
                        }
                    }

                    if (slotNumber == selectedSlotIndex)
                    {
                        HighlightItem(txtBorder);
                    }
                    
                    col++;
                    slotNumber++;
                }
                row++;
            }
        }

        private void setComparisonData(ShelfItem current, ShelfItem prev)
        {
            currentItem = current;
            prevItem = prev;

            trainingWindow.rectCurr.Fill = GetBrushFromColor(current.Color);
            trainingWindow.txtCurrName.Text = current.Name;
            trainingWindow.txtCurrScore.Text = current.Score.ToString("#,##0.##");

            trainingWindow.rectPrev.Fill = GetBrushFromColor(prev.Color);
            trainingWindow.txtPrevName.Text = prev.Name;
            trainingWindow.txtPrevScore.Text = prev.Score.ToString("#,##0.##");
        }
        private void ItemAttributes_Compare_MouseEnter(object sender, MouseEventArgs e)
        {
            var itemPair = (KeyValuePair<ShelfItem, ShelfItem>)((TextBlock)sender).Tag;

            trainingWindow.comparisonGrid.Visibility = Visibility.Visible;
            setComparisonData(itemPair.Key, itemPair.Value);
            itemScoreTxt.Text = itemPair.Value.Score.ToString("#,##0.##");
        }

        private void ItemAttributes_Compare_MouseLeave(object sender, MouseEventArgs e)
        {
            itemScoreTxt.Text = " ";
            trainingWindow.comparisonGrid.Visibility = Visibility.Hidden;
        }

        private void ItemAttributes_MouseEnter(object sender, MouseEventArgs e)
        {           
            itemScoreTxt.Text = ((ShelfItem)((TextBlock)sender).Tag).Score.ToString("#,##0.##");
        }
        private void ItemAttributes_MouseLeave(object sender, MouseEventArgs e)
        {
            itemScoreTxt.Text = " ";
        }

        public void UpdateTensorflowResults(PlanogramOptResults results, bool enableSimDisplay = false)
        {
            if (tokenSource.IsCancellationRequested) return;
            if (enableSimDisplay)
                Task.Delay(1).Wait();

            Dispatcher.Invoke(() =>
            {
                string scoreText = (simSettings.SimType == SimulationType.Score) ? $"{results.Score.ToString("#,##0.##")} ({results.NumScoreHits})" : results.Score.ToString("#,##0.##");
                scoreTxt.Text = scoreText;
                sessionRunTxt.Text = results.CurrentSession.ToString();
                timElapseTxt.Text = results.TimeElapsed.ToString();
                minScoretxt.Text = results.MinScore.ToString("#,##0.##");
                maxScoreTxt.Text = results.MaxScore.ToString("#,##0.##");
                //engineTxtTensor.Text = "Tensorflow";
                averageScoreTxt.Text = results.AvgScore.ToString("#,##0.##");
                averageScoreOf10Txt.Text = results.AvgLastTen.ToString("#,##0.##");

                if (grpMLSettings.Visibility != Visibility.Visible)
                {
                    RLMsettingGroup.Visibility = Visibility.Hidden;
                    if (rdbEncog.IsChecked == true)
                    {
                        grpMLSettings.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        grpTFSettings.Visibility = Visibility.Visible;
                    }
                }

                // for encog settings
                if (rdbEncog.IsChecked.HasValue && rdbEncog.IsChecked.Value)
                {
                    txtTrainingMethod.Text = ENCOG_TRAINING_METHOD;
                    txtActFunc.Text = ENCOG_ACTIVATION_FUNCTION;
                    txtHiddenLayers.Text = simSettings.HiddenLayers.ToString();
                    txtHLNeurons.Text = simSettings.HiddenLayerNeurons.ToString();
                }
                else // for tensorflow
                {
                    //txtOptimizer_tf.Text = TF_OPTIMIZER;
                    //txtActFunc_tf.Text = TF_ACTIVATION_FUNCTION;
                    //txtHiddenLayers_tf.Text = TF_HIDDEN_LAYERS.ToString();
                    //txtHLNeurons_tf.Text = TF_HIDDEN_LAYER_NEURONS.ToString();
                }

                if (enableSimDisplay)
                {
                    CurrentResults = results;                    
                    FillGrid(trainingWindow.planogram, results.Shelves, false, ItemAttributes_MouseEnter, ItemAttributes_MouseLeave, isRLM:false);
                }
            });
        }

        public void UpdateTensorflowStatus(string statusMsg, bool isDone = false)
        {
            if (tokenSource.IsCancellationRequested) return;

            Dispatcher.Invoke(() =>
            {
                statusTxt.Text = statusMsg;

                if (isDone)
                    EnableControlButtons(true);
            });
        }

        public void TFSettings(string optimizer, string activation, int hiddenLayers, int hiddenNeurons, double learnRate)
        {
            Dispatcher.Invoke(()=> {
                txtOptimizer_tf.Text = optimizer;
                txtActFunc_tf.Text = activation;
                txtHiddenLayers_tf.Text = hiddenLayers.ToString();
                txtHLNeurons_tf.Text = hiddenNeurons.ToString();
                txtLearningRate_tf.Text = learnRate.ToString();
            });
        }

        public void AddLogDataTensorflow(int session, double score, double seconds)
        {
            if (tokenSource.IsCancellationRequested) return;

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

            var selectedIndex = cmbPlanogramSize.SelectedIndex;
            currPlanogramSize = planogramSizes[selectedIndex];

            this.simSettings.NumItems = currPlanogramSize.ItemsCount;
            this.simSettings.NumShelves = currPlanogramSize.Shelves;
            this.simSettings.NumSlots = currPlanogramSize.SlotsPerShelf;

            if (trainingWindow != null)
            {
                InitializeGrid(trainingWindow.planogram, Colors.Gray);
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
            bool isEncog = false;
            SaveFileDialog saver = new SaveFileDialog();
            saver.Filter = "Csv file (*.csv)|*.csv";
            saver.FileName = (isEncog) ? "ENCOG_Results" : "TF_Results";
            if (saver.ShowDialog() == true)
            {
                var pathToFile = saver.FileName;

                Logger.ToCsv(pathToFile, (isEncog) ? false : true);
            }
        }

        private StartupWindow startupWin = null;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //startupWin = new StartupWindow();
            //ShowStartupWindow(true, "Connecting to SQL Server...");

            try
            {
                metricPanel = new MetricPanel(null);
                setSelectedPlanogramSize();

                using (PlanogramContext ctx = new PlanogramContext())
                {
                    MockData mock = new MockData(ctx);
                    itemsCache = mock.GetItemsWithAttr();
                    simSettings.ItemMetricMin = mock.GetItemMinimumScore(simSettings);
                    simSettings.ItemMetricMax = mock.GetItemMaximumScore(simSettings);
                }

                ShowStartupWindow(false);
            }
            catch (Exception ex)
            {
                ShowStartupWindow(true, $"ERROR: {ex.Message}");
            }
        }

        private void ShowStartupWindow(bool show, string msg = "")
        {
            Dispatcher.Invoke(()=> {
                if (show)
                {
                    startupWin.startupWindowMsg.Document.Blocks.Clear();
                    startupWin.startupWindowMsg.AppendText(msg);
                    startupWin.Show();
                }
                else
                {
                    startupWin.Hide();
                }
            });
        }
        
        public void DisplayLearningComparisonResults(RlmLearnedSessionDetails selectedData, RlmLearnedSessionDetails comparisonData)
        {
            try
            {
                if (core.IsComparisonModeOn)
                {
                    tmpSelectedData = selectedData;
                    tmpComparisonData = comparisonData;
                }

                trainingWindow.comparisonOverlay.Visibility = Visibility.Visible;
                trainingWindow.comparisonLinkGrid.Visibility = Visibility.Visible;

                IEnumerable<Shelf> selectedPlanogram = GetShelfData(selectedData);
                IEnumerable<Shelf> comparisonPlanogram = null;
                if (comparisonData != null)
                {
                    comparisonPlanogram = GetShelfData(comparisonData);
                }

                RemoveHighlightedItems();
                FillGrid(trainingWindow.planogram, selectedPlanogram, false, mouseEnterHandler: ItemAttributes_Compare_MouseEnter, mouseLeaveHandler: ItemAttributes_Compare_MouseLeave, comparisonResult: comparisonPlanogram);

                if (!btnComparisonClose.IsEnabled)
                {
                    preCompareSelectedSlotIndex = selectedSlotIndex;
                }

                btnComparisonClose.IsEnabled = true;
            }
            catch (Exception ex)
            {
                ShowStartupWindow(true, $"ERROR: {ex.Message}");
            }
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
            if(rlvPanel != null)
            {
                rlvPanel.Close();
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
            selectedSlotIndex = preCompareSelectedSlotIndex;

            trainingWindow.comparisonOverlay.Visibility = Visibility.Hidden;
            trainingWindow.comparisonLinkGrid.Visibility = Visibility.Collapsed;
            visualizer.CloseComparisonMode();
            FillGrid(trainingWindow.planogram, CurrentResults.Shelves.ToList(), false, ItemAttributes_MouseEnter, ItemAttributes_MouseLeave);
            btnComparisonClose.IsEnabled = false;

            if (itemCompPanel.IsVisible)
            {
                itemCompPanel.Hide();
            }

            //RemoveHighlightedItems();

            // row and col now correspond Grid's RowDefinition and ColumnDefinition mouse was 
            // over when double clicked!            
            var txtBorder = trainingWindow.planogram.Children
                .Cast<Border>().ElementAt(selectedSlotIndex);

            //HighlightItem(txtBorder);
            OnSelectedItem(txtBorder);
        }

        private void MetroWindow_LocationChanged(object sender, EventArgs e)
        {
            AlignPanels();
        }

        private void showRLVConfigurationPanel()
        {
            vsConfig = new RLVConfigurationPanel(rlvPanel.rlv.ChartControl, rlvPanel.rlv.DetailsControl);
            vsConfig.Show();
            vsConfig.Top = this.Top;
            vsConfig.Left = this.Left + this.Width;
        }

        public void CloseComparisonLink_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            btnComparisonClose_Click(null, null);
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

        private void GenerateRandomProducts_MouseDown(object sender, MouseButtonEventArgs e)
        {
            showDataPanelDlg();
        }

        private void CreateRetailConstraints_MouseDown(object sender, MouseButtonEventArgs e)
        {
            metricsBtnFunction();
        }

        private void StartFlowChart_MouseDown(object sender, MouseButtonEventArgs e)
        {
            runSlmBtnFunction();
        }

        private void CreateShelfSpace_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OpenTrainingWindow();
        }

        private void OpenTrainingWindow()
        {
            if(trainingWindow == null)
            {
                trainingWindow = new EngineTrainingWindow(this);
            }

            trainingWindow.Show();
        }
    }
}

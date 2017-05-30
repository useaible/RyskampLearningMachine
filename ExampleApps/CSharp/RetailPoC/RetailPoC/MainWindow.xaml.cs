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

namespace RetailPoC
{
    public delegate void SimulationStart(Item[] items, SimulationSettings simSettings);

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int SMALL_ITEMS_COUNT = 100;
        const int LARGE_ITEMS_COUNT = 5000;
        const int SHELVES = 12;
        const int SLOTS_PER_SHELF = 24;
        private DataFactory dataFactory = new DataFactory();
        private DataPanel dataPanel = new DataPanel();

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

        public event SimulationStart OnSimulationStart;

        public SimulationCsvLogger Logger { get; set; } = new SimulationCsvLogger();
        public string HelpPath { get; set; } = AppDomain.CurrentDomain.BaseDirectory;

        public MainWindow()
        {
            InitializeComponent();
            headToHead = false;
            Width = 620;
            toggleColorBtn.Margin = new Thickness(422, 14, 0, 0); //new Thickness(62, 10, 0, 0);
            dataGenerationBtn.Margin = new Thickness(52, 14, 0, 0); //new Thickness(182, 10, 0, 0);
            metricsBtn.Margin = new Thickness(174, 14, 0, 0); //new Thickness(302, 10, 0, 0);
            runSlmBtn.Margin = new Thickness(294, 14, 0, 0); //new Thickness(422, 10, 0, 0);

            planogramTensorflow.Visibility = Visibility.Hidden;
            rectTensorflow.Visibility = Visibility.Hidden;

            tensorFlowPlanogramScore.Visibility = Visibility.Hidden;
            GrdTensorFlowsetting.Visibility = Visibility.Hidden;
            grpBox_Tensorflow.Visibility = Visibility.Hidden;

            FillGrid(planogram, Colors.LightGray);


        }

        public MainWindow(bool headtohead)
        {
            InitializeComponent();
            headToHead = headtohead;
            Title += " - Head to Head";
            FillGrid(planogram, Colors.LightGray);
            FillGrid(planogramTensorflow, Colors.LightGray);

            targetScoreTxt.Margin = new Thickness(522, 128, 0, 0);
            targetScoreLbl.Margin = new Thickness(497, 98, 0, 0);
            
            
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
                    targetScoreTxt.Text = simSettings.Score.ToString();
                }
                else
                {
                    targetScoreLbl.Visibility = Visibility.Hidden;
                    targetScoreTxt.Visibility = Visibility.Hidden;
                }

                if(simSettings.SimType == SimulationType.Sessions)
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

                Task.Run(() =>
                {
                    // get items from db as well as the min and max metric scores as we need that for the calculation later on
                    Item[] items;
                    using (PlanogramContext ctx = new PlanogramContext())
                    {
                        MockData mock = new MockData(ctx);
                        items = mock.GetItemsWithAttr();
                        simSettings.ItemMetricMin = mock.GetItemMinimumScore(simSettings);
                        simSettings.ItemMetricMax = mock.GetItemMaximumScore(simSettings);
                    }

                    // let's tensorflow (or other listeners) know that it should start training
                    //OnSimulationStart?.Invoke(items, simSettings); //return;

                    // initialize and start RLM training
                    PlanogramOptimizer optimizer = new PlanogramOptimizer(items, simSettings, this.UpdateRLMResults, this.UpdateRLMStatus, Logger);
                    //optimizer.OnSessionDone += Optimizer_OnSessionDone;
                    optimizer.StartOptimization();
                });
            }
        }

        private void EnableControlButtons(bool value)
        {
            toggleColorBtn.IsEnabled = value;
            dataGenerationBtn.IsEnabled = value;
            metricsBtn.IsEnabled = value;
            runSlmBtn.IsEnabled = value;
            rlmCsvBtn.IsEnabled = value;
            tfCsvBtn.IsEnabled = value;
            CbPlanogramSize.IsEnabled = value;
        }

        private void UpdateRLMResults(PlanogramOptResultsSettings results, bool enableSimDisplay)
        {
            Dispatcher.Invoke(() =>
            {
                string scoreText = (simSettings.SimType == SimulationType.Score) ? $"{results.Score.ToString("#,###.##")} ({results.NumScoreHits})" : results.Score.ToString("#,###.##");
                scoreTxt.Text = scoreText;
                sessionRunTxt.Text = results.CurrentSession.ToString();
                timElapseTxt.Text = string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}", results.TimeElapsed.Hours, results.TimeElapsed.Minutes, results.TimeElapsed.Seconds, results.TimeElapsed.Milliseconds);
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

                if (enableSimDisplay)
                {
                    currentResults = results;
                    FillGrid(planogram, results, false, ItemAttributes_MouseEnter, ItemAttributes_MouseLeave);
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

            for (int row = 0; row < simSettings.NumShelves; row++)
            {
                for (int column = 0; column < simSettings.NumSlots; column++)
                {
                    Rectangle itemAttributes = new Rectangle();
                    itemAttributes.Fill = new SolidColorBrush(color);
                    itemAttributes.Stroke = new SolidColorBrush(Colors.Black);
                    //itemAttributes.HorizontalAlignment = HorizontalAlignment.Center;

                    Grid.SetColumn(itemAttributes, column);
                    Grid.SetRow(itemAttributes, row);
                    planogramGrid.Children.Add(itemAttributes);
                }
            }
        }

        private void FillGrid(Grid planogramGrid, PlanogramOptResults result, bool usePerfColor = false, MouseEventHandler handler = null, MouseEventHandler handler2 = null)
        {
            if (result == null || planogramGrid == null)
                return;

            planogramGrid.Children.Clear();

            int row = 0;
            foreach (var shelf in result.Shelves)
            {
                int col = 0;
                foreach (var item in shelf.Items)
                {
                    Rectangle itemAttributes = new Rectangle();
                    itemAttributes.Fill = new SolidColorBrush(usePerfColor ? item.PerfColor : item.Color);
                    itemAttributes.Stroke = new SolidColorBrush(Colors.Black);
                    if (handler != null)
                        itemAttributes.MouseEnter += handler;
                        itemAttributes.MouseLeave += handler2;
                    itemAttributes.Tag = item.ScoreDetailed;

                    Grid.SetColumn(itemAttributes, col);
                    Grid.SetRow(itemAttributes, row);
                    planogramGrid.Children.Add(itemAttributes);
                    col++;
                }
                row++;
            }
        }

        private void ItemAttributes_MouseEnter(object sender, MouseEventArgs e)
        {
            itemScoreTxt.Text = ((Rectangle)sender).Tag.ToString();
        }
        private void ItemAttributes_MouseLeave(object sender, MouseEventArgs e)
        {
            itemScoreTxt.Text = " ";
        }

        private void ItemAttrbutes_MouseEnter_Tensorflow(object sender, MouseEventArgs e)
        {
            itemScoreTxtTensor.Text = ((Rectangle)sender).Tag.ToString();
        }

        private void ItemAttrbutes_MouseLeave_Tensorflow(object sender, MouseEventArgs e)
        {
            itemScoreTxtTensor.Text = " ";
        }

        private void toggleColorBtn_Click(object sender, RoutedEventArgs e)
        {
            usePerfColor = !usePerfColor;
            FillGrid(planogram, currentResults, usePerfColor, ItemAttributes_MouseEnter, ItemAttributes_MouseLeave);
            if (headToHead)
            {
                FillGrid(planogramTensorflow, currentResultsTensor, usePerfColor, ItemAttrbutes_MouseEnter_Tensorflow, ItemAttrbutes_MouseLeave_Tensorflow);
            }
        }

        public void UpdateTensorflowResults(PlanogramOptResults results, bool enableSimDisplay = false)
        {
            Dispatcher.Invoke(() =>
            {
                string scoreText = (simSettings.SimType == SimulationType.Score) ? $"{results.Score.ToString("#,###.##")} ({results.NumScoreHits})" : results.Score.ToString("#,###.##");
                scoreTxtTensor.Text = scoreText;
                sessionRunTxtTensor.Text = results.CurrentSession.ToString();
                timElapseTxtTensor.Text = string.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}", results.TimeElapsed.Hours, results.TimeElapsed.Minutes, results.TimeElapsed.Seconds, results.TimeElapsed.Milliseconds);
                minScoreTxtTensor.Text = results.MinScore.ToString("#,###.##");
                maxScoreTxtTensor.Text = results.MaxScore.ToString("#,###.##");
                //engineTxtTensor.Text = "Tensorflow";
                averageScoreTxtTensor.Text = results.AvgScore.ToString("#,###.##");
                averageScoreOf10TxtTensor.Text = results.AvgLastTen.ToString("#,###.##");

                if (enableSimDisplay)
                {
                    currentResultsTensor = results;
                    FillGrid(planogramTensorflow, results, false, ItemAttrbutes_MouseEnter_Tensorflow, ItemAttrbutes_MouseLeave_Tensorflow);
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
        }

        protected override void OnClosed(EventArgs e)
        {
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
    }
}

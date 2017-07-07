using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Helpers;
using LiveCharts.Wpf;
using RLM.Models;
using RLV.Core.Interfaces;
using RLV.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
using OxyPlot;
using LiveCharts.Defaults;
using Newtonsoft.Json;
using System.IO;
using RLV.Core.Converters;

namespace WPFVisualizer
{
    /// <summary>
    /// Interaction logic for RLVProgressionChartPanel.xaml
    /// </summary>
    public partial class RLVProgressionChartPanel : UserControl, IRLVProgressionChartPanel
    {
        public event SelectedCaseChangedDelegate SelectedCaseChangedEvent;
        public event SelectedCaseScaleChangedDelegate SelectedCaseScaleChangedEvent;

        private List<RlmLearnedCase> learnedCases = new List<RlmLearnedCase>();
        private List<IRLVItemDisplay> displayList = new List<IRLVItemDisplay>();
        private long selectedCaseId;
        private Rectangle prevRect = new Rectangle();
        private RLVChartData previousSelected = new RLVChartData();
        private ChartValues<RLVChartData> chartValues = new ChartValues<RLVChartData>();

        public RLVProgressionChartPanel()
        {
            InitializeComponent();
            getViewModelFromConfig();
            DataContext = ViewModel;
            initChartData();
            ScalePanel = scaleSelectionControl;
            setLabelBindings();
            setValueBindings();
        }

        public object ViewModel { get; private set; } = new RLVProgressionChartVM();
        public IRLVScaleSelectionPanel ScalePanel { get; private set; }

        private bool nextPrevClicked = false;
        public void IRLVCore_NextPrevCaseChangedResultsHandler(long caseId)
        {
            nextPrevClicked = true;
            selectedCaseId = caseId;
            progressionChart_DataClick(null, null); //Auto click the chart point
        }

        public void IRLVCore_RealTimeUpdateHandler(IEnumerable<RlmLearnedCase> data)
        {
            loadChart(data, null);
        }

        public void IRLVCore_ScaleChangedResultsHandler(IEnumerable<RlmLearnedCase> data)
        {
            loadChart(data, null);
        }

        public void IRLVCore_SelectedCaseScaleChangedResultsHandler(IEnumerable<RlmLearnedCase> data, long selectedCaseId)
        {
            this.selectedCaseId = selectedCaseId;
            loadChart(data, null, selectedCaseId);
        }

        private void loadChart(IEnumerable<RlmLearnedCase> data, IEnumerable<IRLVItemDisplay> itemDisplay, long? caseId = null)
        {
            learnedCases = data.ToList();
            displayList = itemDisplay?.ToList();

            if (learnedCases.Count == 0)
                return;

            chartValues.Clear();

            int inc = 0;
            foreach (var cv in learnedCases)
            {
                long? prevId = null;
                if (inc < learnedCases.Count - 1)
                    prevId = learnedCases[inc + 1].CaseId;

                chartValues.Add(new RLVChartData { CaseId = cv.CaseId, Score = cv.Score, Time = cv.Time, PrevCaseId = prevId });
                inc++;
            }

            ((SeriesCollection)((IRLVProgressionChartVM)ViewModel).SeriesCollection)[0].Values = chartValues;

            //createAxis();

            var lastCase = data.FirstOrDefault();
            if (caseId.HasValue || lastCase != null)
            {
                selectedCaseId = caseId != null ? caseId.Value : lastCase.CaseId; // Get the last case to be set as the default selection
                nextPrevClicked = false;
                progressionChart_DataClick(null, null); //Auto click the chart point
            }
        }

        private void createAxis()
        {
            //Configuration of X-Axis label
            progressionChart.AxisX.Clear(); //Clear to redraw X-Axis labels
            Axis xAxis = new Axis();
            xAxis.LabelFormatter = (Func<double, string>)((IRLVProgressionChartVM)ViewModel).XLabelFormatter;
            xAxis.Title = ((IRLVProgressionChartVM)ViewModel).XAxisTitle;

            var chartPointsCount = chartValues.Count;

            if (chartPointsCount <= 0)
                return;

            double max = chartValues.Select(a => a.Time).Max();
            double min = chartValues.Select(a => a.Time).Min();
            double max_min = max - min;
            double step = max_min / 20;

            if (step > 0 || chartPointsCount == 1)
            {
                if (chartPointsCount == 1)
                    step = max / 20;

                //var newStep = Math.Ceiling(max / step);
                //if (chartPointsCount <= 10 && chartPointsCount > 1)
                //    newStep = 10;

                step = Math.Ceiling(step);
                xAxis.Separator = new LiveCharts.Wpf.Separator { Step = step, IsEnabled = false };

                xAxis.LabelsRotation = 20;

                progressionChart.AxisX.Add(xAxis); //Add back X-Axis to chart

                //Configuration of Y-Axis label
                progressionChart.AxisY.Clear(); //Clear to redraw Y-Axis labels
                Axis yAxis = new Axis();
                yAxis.LabelFormatter = (Func<double, string>)((IRLVProgressionChartVM)ViewModel).YLabelFormatter;
                yAxis.Title = ((IRLVProgressionChartVM)ViewModel).YAxisTitle;

                progressionChart.AxisY.Add(yAxis); //Add back Y-Axis to chart
                progressionChart.Zoom = ZoomingOptions.Xy;
            }
        }

        private void initChartData()
        {
            var lineSeries = new LineSeries
            {
                Values = new ChartValues<RLVChartData>(),
                StrokeThickness = 1,
                Fill = Brushes.Transparent,
                PointGeometrySize = 10,
                Title = "",
                DataLabels = false,
                PointForeground = new SolidColorBrush(Colors.White),
                LineSmoothness = 0,
                Stroke = new SolidColorBrush(Colors.Orange)
            };

            ((IRLVProgressionChartVM)ViewModel).SeriesCollection = new SeriesCollection() { lineSeries };

            var chartMapper = Mappers.Xy<RLVChartData>()
                .X(a => a.Time)
                .Y(a => a.Score)
                .Fill(a => a.Selected ? Brushes.Red : Brushes.Orange);

            Charting.For<RLVChartData>(chartMapper);

            createAxis();
            progressionChart.LegendLocation = LegendLocation.None;
        }

        public void IRLVCore_SelectedUniqueInputSetChangedResultsHandler(IEnumerable<RlmLearnedCase> data, IEnumerable<IRLVItemDisplay> itemDisplay, bool showComparison = false)
        {
            loadChart(data, itemDisplay);
        }

        private void selectDefaultChartPoint(long caseId)
        {
            var defaultPoint = chartValues.FirstOrDefault(a => a.CaseId == caseId); //Find the chart point from the chart values by caseId
            if (defaultPoint != null)
            {
                previousSelected.Selected = false;
                defaultPoint.Selected = true;
                previousSelected = defaultPoint;
            }
        }

        private bool pointExistInChart(long caseId)
        {
            var point = chartValues.FirstOrDefault(a => a.CaseId == caseId); //Find the chart point from the chart values by caseId
            return point != null;
        }

        private double calcScale(int itemCount)
        {
            const double OFFSET_PERCENTAGE = 10D;
            const double MAX_PERCENTAGE = 100D;
            double retVal = 0;

            double currentScale = ((RLVScaleSelectionVM)ScalePanel.ViewModel).DefaultScale;
            double totalItems = Convert.ToDouble(itemCount) / (currentScale / 100D);
            double tenPercentValue = totalItems * (OFFSET_PERCENTAGE / 100);

            double projectedItemCount = double.NaN;            
            for (int i = 1; i <= Math.Ceiling((MAX_PERCENTAGE - currentScale) / OFFSET_PERCENTAGE); i++)
            {
                double offset = (tenPercentValue * i);
                projectedItemCount = Math.Floor(itemCount + offset);
                if (projectedItemCount > itemCount)
                {
                    retVal = currentScale + OFFSET_PERCENTAGE;
                    if (retVal > MAX_PERCENTAGE)
                        retVal = MAX_PERCENTAGE;
                    break;
                }
            }

            return retVal;
        }

        public void UpdateBindings(RLVItemDisplayVM userVal)
        {
            if (userVal.SelectedValueFromConverter != null && userVal.ConverterType != null)
            {
                if (userVal.ConverterType == 0) //Number
                {
                    userVal.Converter = new RLVNumericConverter((RLV.Core.Enums.RLVFormatters)userVal.SelectedValueFromConverter);
                }
                else //1=Time
                {
                    userVal.Converter = new RLVTimeConverter((RLV.Core.Enums.RLVFormatters)userVal.SelectedValueFromConverter);
                }
            }

            var controls = this.chartHeaderComponentsGrid.Children;
            foreach (var control in controls)
            {
                FrameworkElement con = control as FrameworkElement;

                // Exclude other controls
                if (con.Name.StartsWith("lbl") || string.IsNullOrEmpty(con.Name))
                    continue;

                if (con.Name == userVal.Name)
                {
                    var type = control.GetType();
                    var typeName = type.FullName;
                    var typeAssembly = type.Assembly;

                    Type controlType = Type.GetType($"{typeName}, {typeAssembly}");
                    if (controlType == typeof(TextBlock))
                    {
                        TextBlock textCtrl = control as TextBlock;

                        var visibilityBinding = new Binding { Source = userVal, Path = new PropertyPath("Visibility"), Mode = BindingMode.TwoWay };
                        BindingOperations.SetBinding(textCtrl, TextBlock.VisibilityProperty, visibilityBinding);

                        var valueBinding = new Binding { Source = ViewModel, Path = new PropertyPath(textCtrl.Tag.ToString()) };
                        valueBinding.Converter = userVal.Converter;
                        BindingOperations.SetBinding(textCtrl, TextBlock.TextProperty, valueBinding);
                    }
                }
            }
        }

        private void progressionChart_DataClick(object sender, ChartPoint chartPoint)
        {
            var chart = progressionChart;

            if(chartPoint == null)
            {
                bool pointExistInChart = this.pointExistInChart(selectedCaseId);
                if (nextPrevClicked && !pointExistInChart)
                {
                    double newScale = calcScale(chartValues.Count);
                    SelectedCaseScaleChangedEvent?.Invoke(selectedCaseId, newScale);
                }
                else
                {
                    selectDefaultChartPoint(selectedCaseId);
                    SelectedCaseChangedEvent?.Invoke(selectedCaseId);
                }
            }
            else
            {
                previousSelected.Selected = false; //Deselect the current chart point selection

                RLVChartData data = (RLVChartData)chartPoint.Instance; //Get the current chart point and set as the default selection
                data.Selected = true; 
                previousSelected = data;

                //Invoke event for selecting new case
                SelectedCaseChangedEvent?.Invoke(data.CaseId);
            }

            ((IRLVProgressionChartVM)ViewModel).CurrentTime = previousSelected.Time;
            ((IRLVProgressionChartVM)ViewModel).CurrentScore = previousSelected.Score;

            //progressionChart.Update(true, true);
            createAxis();
            nextPrevClicked = false;
        }

        private void setLabelBindings()
        {
            foreach (var userLabel in ((IRLVProgressionChartVM)ViewModel).Labels)
            {
                var controls = this.chartHeaderComponentsGrid.Children;
                foreach (var control in controls)
                {
                    FrameworkElement con = control as FrameworkElement;
                    if (!con.Name.StartsWith("lbl"))
                        continue;

                    if (con.Name == userLabel.Name)
                    {
                        Label lblCtrl = control as Label;

                        Binding labelBinding = new Binding();
                        labelBinding.Source = userLabel;
                        labelBinding.Path = new PropertyPath("Value");
                        labelBinding.Mode = BindingMode.TwoWay;
                        labelBinding.Converter = userLabel.Converter;

                        var visibilityBinding = new Binding { Source = userLabel, Path = new PropertyPath("Visibility"), Mode = BindingMode.TwoWay };

                        BindingOperations.SetBinding(lblCtrl, Label.ContentProperty, labelBinding);
                        BindingOperations.SetBinding(lblCtrl, Label.VisibilityProperty, visibilityBinding);
                    }
                }
            }
        }

        private void setValueBindings()
        {
            foreach (var userVal in ((IRLVProgressionChartVM)ViewModel).Values)
            {
                this.UpdateBindings(userVal);
            }
        }

        private void progressionChart_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            createAxis(); // Reset chart zooming/panning on double clicked.
        }

        private void getViewModelFromConfig()
        {
            try
            {
                using (StreamReader rdr = File.OpenText("RLVProgressionChartPanel.json"))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.TypeNameHandling = TypeNameHandling.Auto;
                    serializer.Formatting = Formatting.Indented;
                    serializer.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
                    serializer.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
                    dynamic viewModel = (dynamic)serializer.Deserialize(rdr, typeof(object));

                    var labels = viewModel.Labels.ToObject<ObservableCollection<RLVItemDisplayVM>>();
                    var valuesToStr = JsonConvert.SerializeObject(viewModel.Values);
                    var values = JsonConvert.DeserializeObject<ObservableCollection<RLVItemDisplayVM>>(valuesToStr, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

                    ((IRLVProgressionChartVM)ViewModel).Header = viewModel.Header;
                    ((IRLVProgressionChartVM)ViewModel).Labels = labels;
                    ((IRLVProgressionChartVM)ViewModel).Values = values;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("No configuration file found for SelectedDetailsPanel.");
            }
        }

        public void SaveConfiguration()
        {
            try
            {
                FileStream file = File.Open("RLVProgressionChartPanel.json", FileMode.Create);
                using (StreamWriter st = new StreamWriter(file))
                {
                    dynamic obj = new { Header = ((IRLVProgressionChartVM)ViewModel).Header, Labels = ((IRLVProgressionChartVM)ViewModel).Labels, Values = ((IRLVProgressionChartVM)ViewModel).Values };
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.TypeNameHandling = TypeNameHandling.Auto;
                    serializer.Formatting = Formatting.Indented;
                    serializer.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
                    serializer.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
                    serializer.Serialize(st, obj);
                }
            }
            catch(Exception e)
            {
                throw (e);
            }
        }

        public SeriesCollection SeriesCollection
        {
            get { return (SeriesCollection)GetValue(SeriesCollectionProperty); }
            set { SetValue(SeriesCollectionProperty, value); }
        }

        public static readonly DependencyProperty SeriesCollectionProperty =
              DependencyProperty.Register("SeriesCollection", typeof(SeriesCollection),
                typeof(RLVProgressionChartPanel), new PropertyMetadata(null));
    }
}

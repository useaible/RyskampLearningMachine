using RLV.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using RLV.Core.Converters;

namespace RLV.Core.Models
{
    public class RLVProgressionChartVM : IRLVProgressionChartVM
    {
        private double? currentTime;
        private double? currentScore;
        private string xAxisTitle;
        private string yAxisTitle;
        private object xLabelFormatter;
        private object yLabelFormatter;
        private object seriesCollection;
        private string header;
        private ObservableCollection<RLVItemDisplayVM> labels;
        private ObservableCollection<RLVItemDisplayVM> values;

        public RLVProgressionChartVM()
        {
            Func<double, string> yLabelFormatter = value => value.ToString("#,##0.##");
            Func<double, string> xLabelFormatter = value => TimeSpan.FromMilliseconds(value * 1000).ToString();

            this.yLabelFormatter = yLabelFormatter;
            this.xLabelFormatter = xLabelFormatter;

            this.xAxisTitle = "Time";
            this.yAxisTitle = "Score";

            this.header = "Learning Progression Chart";

            this.labels = new ObservableCollection<RLVItemDisplayVM>()
            {
                new RLVItemDisplayVM("lblChartTitleHeader") { Value = "Learning Progression Chart", Visibility = System.Windows.Visibility.Visible },
                new RLVItemDisplayVM("lblCurrTime") { Value = "Time", Visibility = System.Windows.Visibility.Visible },
                new RLVItemDisplayVM("lblCurrScore") { Value = "Session Score", Visibility = System.Windows.Visibility.Visible }
            };

            this.values = new ObservableCollection<RLVItemDisplayVM>()
            {
                new RLVItemDisplayVM("currTimeVal") { Description = "Selected Point Time", Visibility = System.Windows.Visibility.Visible, Converter =  new RLVTimeConverter(Enums.RLVFormatters.Time_Seconds) },
                new RLVItemDisplayVM("currScoreVal") { Description = "Selected Point Score", Visibility = System.Windows.Visibility.Visible, Converter = new RLVNumericConverter(Enums.RLVFormatters.Numeric_Number) }
            };
        }

        public double? CurrentTime
        {
            get { return currentTime; }
            set { currentTime = value;  OnPropertyChanged("CurrentTIme"); }
        }

        public double? CurrentScore
        {
            get { return currentScore; }
            set { currentScore = value; OnPropertyChanged("CurrentScore"); }
        }

        public string XAxisTitle
        {
            get { return xAxisTitle; }
            set { xAxisTitle = value;  OnPropertyChanged("XAxisTitle"); }
        }

        public string YAxisTitle
        {
            get { return yAxisTitle; }
            set { yAxisTitle = value;  OnPropertyChanged("YAxisTitle"); }
        }

        public object XLabelFormatter
        {
            get { return xLabelFormatter; }
            set { xLabelFormatter = value; OnPropertyChanged("XLabelFormatter"); }
        }

        public object YLabelFormatter
        {
            get { return yLabelFormatter; }
            set { yLabelFormatter = value; OnPropertyChanged("YLabelFormatter"); }
        }

        public object SeriesCollection
        {
            get { return seriesCollection; }
            set { seriesCollection = value; OnCollectionChanged("SeriesCollection"); }
        }
        public ObservableCollection<RLVItemDisplayVM> Labels
        {
            get { return labels; }
            set { labels = value; OnCollectionChanged("Labels"); }
        }

        public ObservableCollection<RLVItemDisplayVM> Values
        {
            get { return values; }
            set { values = value; OnCollectionChanged("Values"); }
        }

        public string Header
        {
            get { return header; }
            set { header = value; OnPropertyChanged("Header"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        protected virtual void OnCollectionChanged(string propertyName)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add));
        }
    }
}

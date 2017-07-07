using RLV.Core.Converters;
using RLV.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace RLV.Core.Models
{
    public class RLVSelectedDetailVM : IRLVSelectedDetailVM
    {
        private long? previousSessionId;
        private long? currentSessionId;
        private long? currentSession;
        private double? currentTime;
        private long? currentCase;
        private double? currentScore;
        private long? previousSession;
        private double? previousTime;
        private long? previousCase;
        private double? previousScore;
        private ObservableCollection<RLVIODetailsVM> inputDetails = new ObservableCollection<RLVIODetailsVM>();
        private ObservableCollection<RLVIODetailsVM> outputDetails = new ObservableCollection<RLVIODetailsVM>();
        private string header;
        private ObservableCollection<RLVItemDisplayVM> labels;
        private ObservableCollection<RLVItemDisplayVM> values;

        public RLVSelectedDetailVM()
        {
            this.header = "Selected Details";

            this.labels = new ObservableCollection<RLVItemDisplayVM>()
            {
                new RLVItemDisplayVM("lblSelectedDetailsHeader")
                {
                    Value = "Selected Details",
                    Visibility = System.Windows.Visibility.Visible,
                    Converter = null
                },
                new RLVItemDisplayVM("lblInputDetailsHeader")
                {
                    Value = "Input Details",
                    Visibility = System.Windows.Visibility.Visible,
                    Converter = null
                },
                new RLVItemDisplayVM("lblOutputDetailsHeader")
                {
                    Value = "Output Details",
                    Visibility = System.Windows.Visibility.Visible,
                    Converter = null
                },
                new RLVItemDisplayVM("lblPreviousHeader")
                {
                    Value = "Prior To Learned",
                    Visibility = System.Windows.Visibility.Visible,
                    Converter = null
                },
                new RLVItemDisplayVM("lblCurrentHeader")
                {
                    Value = "Learned",
                    Visibility = System.Windows.Visibility.Visible,
                    Converter = null
                },
                new RLVItemDisplayVM("lblSessionTitle")
                {
                    Value = "Session",
                    Visibility = System.Windows.Visibility.Visible,
                    Converter = null
                },
                new RLVItemDisplayVM("lblTimeTitle")
                {
                    Value = "Time",
                    Visibility = System.Windows.Visibility.Visible,
                    Converter = null
                },
                new RLVItemDisplayVM("lblCaseTitle")
                {
                    Value = "Case",
                    Visibility = System.Windows.Visibility.Visible,
                    Converter = null
                },
                new RLVItemDisplayVM("lblScoreTitle")
                {
                    Value = "Score",
                    Visibility = System.Windows.Visibility.Visible,
                    Converter = null
                },
                new RLVItemDisplayVM("lblBtnPrevious")
                {
                    Value = "Previous",
                    Visibility = System.Windows.Visibility.Visible,
                    Converter = null
                },
                new RLVItemDisplayVM("lblBtnNext")
                {
                    Value = "Next",
                    Visibility = System.Windows.Visibility.Visible,
                    Converter = null
                }
            };

            this.values = new ObservableCollection<RLVItemDisplayVM>()
            {
                new RLVItemDisplayVM("inputDetailsGrid")
                {
                    Description = "Input Details",
                    Value = null,
                    Visibility = System.Windows.Visibility.Visible,
                    Converter = null
                },
                new RLVItemDisplayVM("outputDetailsGrid")
                {
                    Description = "Output Details",
                    Value = null,
                    Visibility = System.Windows.Visibility.Visible,
                    Converter = null
                },
                new RLVItemDisplayVM("prevSessionVal")
                {
                    Description = "Previous Learning Point",
                    Value = null,
                    Visibility = System.Windows.Visibility.Visible,
                    Converter = null
                },
                new RLVItemDisplayVM("prevTimeVal")
                {
                    Description = "Previous Learning Time",
                    Value = null,
                    Visibility = System.Windows.Visibility.Visible,
                    Converter = new RLVTimeConverter(Enums.RLVFormatters.Time_Seconds)
                },
                new RLVItemDisplayVM("prevCaseVal")
                {
                    Description = "Previous Learning Event",
                    Value = null,
                    Visibility = System.Windows.Visibility.Visible,
                    Converter = null
                },
                new RLVItemDisplayVM("prevScoreValContainer")
                {
                    Description = "Previous Learning Score",
                    Value = null,
                    Visibility = System.Windows.Visibility.Visible,
                    Converter = new RLVNumericConverter(Enums.RLVFormatters.Numeric_Number)
                },
                new RLVItemDisplayVM("currSessionVal")
                {
                    Description = "Current Learning Point",
                    Value = null,
                    Visibility = System.Windows.Visibility.Visible,
                    Converter = null
                },
                new RLVItemDisplayVM("currTimeVal")
                {
                    Description = "Current Learning Time",
                    Value = null,
                    Visibility = System.Windows.Visibility.Visible,
                    Converter = new RLVTimeConverter(Enums.RLVFormatters.Time_Seconds)
                },
                new RLVItemDisplayVM("currCaseVal")
                {
                    Description = "Current Learning Event",
                    Value = null,
                    Visibility = System.Windows.Visibility.Visible,
                    Converter = null
                },
                new RLVItemDisplayVM("currScoreValContainer")
                {
                    Description = "Current Learning Score",
                    Value = null,
                    Visibility = System.Windows.Visibility.Visible,
                    Converter = new RLVNumericConverter(Enums.RLVFormatters.Numeric_Number)
                }
            };
        }

        public long? PreviousSessionId
        {
            get { return previousSessionId; }
            set { previousSessionId = value; OnPropertyChanged("PreviousSessionId"); }
        }
        public long? CurrentSessionId
        {
            get { return currentSessionId; }
            set { currentSessionId = value; OnPropertyChanged("CurrentSessionId"); }
        }

        public long? PreviousSession
        {
            get { return previousSession; }
            set { previousSession = value; OnPropertyChanged("PreviousSession"); }
        }
        public double? PreviousTime
        {
            get { return previousTime; }
            set { previousTime = value; OnPropertyChanged("PreviousTime"); }
        }
        public long? PreviousCase
        {
            get { return previousCase; }
            set { previousCase = value; OnPropertyChanged("PreviousCase"); }
        }
        public double? PreviousScore
        {
            get { return previousScore; }
            set { previousScore = value; OnPropertyChanged("PreviousScore"); }
        }

        public long? CurrentSession
        {
            get { return currentSession; }
            set { currentSession = value; OnPropertyChanged("CurrentSession"); }
        }
        public double? CurrentTime
        {
            get { return currentTime; }
            set { currentTime = value; OnPropertyChanged("CurrentTime"); }
        }
        public long? CurrentCase
        {
            get { return currentCase; }
            set { currentCase = value; OnPropertyChanged("CurrentCase"); }
        }
        public double? CurrentScore
        {
            get { return currentScore; }
            set { currentScore = value; OnPropertyChanged("CurrentScore"); }
        }

        public ObservableCollection<RLVIODetailsVM> InputDetails
        {
            get { return inputDetails; }
            set { inputDetails = value; OnPropertyChanged("InputDetails"); }
        }
        public ObservableCollection<RLVIODetailsVM> OutputDetails
        {
            get { return outputDetails; }
            set { outputDetails = value; OnPropertyChanged("OutputDetails"); }
        }

        public ObservableCollection<RLVItemDisplayVM> Labels
        {
            get { return labels; }
            set { labels = value;  OnCollectionChanged("Labels"); }
        }

        public ObservableCollection<RLVItemDisplayVM> Values
        {
            get { return values; }
            set { values = value;  OnCollectionChanged("Values"); }
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

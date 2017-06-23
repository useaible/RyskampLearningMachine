using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLV.Core.Models
{
    public class RLVChartData : INotifyPropertyChanged
    {
        private double score;
        private double time;
        private bool selected;
        private long caseId;
        private long? prevCaseId;
        
        public double Score
        {
            get { return score; }
            set { score = value; OnPropertyChanged("Score"); }
        }

        public double Time
        {
            get { return time; }
            set { time = value; OnPropertyChanged("Time"); }
        }

        public long CaseId
        {
            get { return caseId; }
            set { caseId = value; OnPropertyChanged("CaseID"); }
        }

        public long? PrevCaseId
        {
            get { return prevCaseId; }
            set { prevCaseId = value; OnPropertyChanged("PrevCaseId"); }
        }

        public bool Selected
        {
            get { return selected; }
            set { selected = value; OnPropertyChanged("Selected"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

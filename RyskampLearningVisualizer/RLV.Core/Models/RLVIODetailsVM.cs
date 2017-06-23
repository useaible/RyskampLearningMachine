using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLV.Core.Models
{
    public class RLVIODetailsVM : INotifyPropertyChanged
    {
        private string name;
        private string ioValue;
        public string Name
        {
            get { return name; } set { name = value; OnPropertyChanged("Name"); }
        }
        public string Value
        {
            get { return ioValue; } set { ioValue = value; OnPropertyChanged("Value"); }
        }
        //public bool IsInput
        //{
        //    get { return isInput; } set { isInput = value; OnPropertyChanged("IsInput"); }
        //}

        //// used for learning comparison data
        //public long CaseId
        //{
        //    get { return caseId; } set { caseId = value; OnPropertyChanged("CaseId"); }
        //}
        //public double CycleScore
        //{
        //    get { return cycleScore; } set { cycleScore = value; OnPropertyChanged("CycleScore"); }
        //}
        //public long SessionId
        //{
        //    get { return sessionId; } set { sessionId = value; OnPropertyChanged("SessionId"); }
        //}

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

using RLV.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace RLV.Core.Models
{
    public class RLVScaleSelectionVM : IRLVScaleSelectionVM
    {
        private long currentCaseId;
        private string sliderText;
        private double sliderValue;

        public long CurrentCaseId
        {
            get { return currentCaseId; }
            set { currentCaseId = value; OnPropertyChanged("CurrentCaseId"); }
        }

        public string SliderLabelText
        {
            get { return sliderText; }
            set { sliderText = value;  OnPropertyChanged("SliderLabelText"); }
        }

        public double DefaultScale
        {
            get {  return sliderValue; }
            set { sliderValue = value; OnPropertyChanged("SliderValue"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

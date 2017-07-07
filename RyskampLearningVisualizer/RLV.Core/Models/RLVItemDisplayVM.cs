using RLV.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RLV.Core.Enums;
using System.Windows.Data;
using RLV.Core.Converters;

namespace RLV.Core.Models
{
    public class RLVItemDisplayVM : INotifyPropertyChanged
    {
        private string name;
        private string description;
        private object value;
        private System.Windows.Visibility visibility;
        private IValueConverter converter;
        private int? selectedValueFromConverter;
        private int? converterType;

        public RLVItemDisplayVM(string name)
        {
            this.name = name;
        }

        public string Name { get { return name; } }
        public string Description { get { return description; } set { description = value; OnPropertyChanged("Description"); } }
        public object Value { get { return value; } set { this.value = value; OnPropertyChanged("Value"); } }
        public System.Windows.Visibility Visibility { get { return visibility; } set { visibility = value; OnPropertyChanged("Visibility"); } }
        public IValueConverter Converter { get { return converter; } set { converter = value; OnPropertyChanged("Converter"); } }

        /// <summary>
        /// The following 2 properties (SelectedValueFromConverter and ConverterType) are used
        /// to be able to set on the UI what was the selected converter value that will automatically 
        /// update the RLV display as needed.
        /// </summary>
        public int? SelectedValueFromConverter { get { return selectedValueFromConverter; } set { selectedValueFromConverter = value; OnPropertyChanged("SelectedValueFromConverter"); } }

        //This is to let us know what type of converter it is (0=Number, 1=Time)
        public int? ConverterType { get { return converterType; } set { converterType = value; OnPropertyChanged("ConverterType"); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

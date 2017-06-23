using RLV.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RLV.Core.Enums;
using System.Windows.Data;

namespace RLV.Core.Models
{
    public class RLVItemDisplayVM : INotifyPropertyChanged
    {
        private string name;
        private string description;
        private object value;
        private System.Windows.Visibility visibility;
        private IValueConverter converter;

        public RLVItemDisplayVM(string name)
        {
            this.name = name;
        }

        public string Name { get { return name; } }
        public string Description { get { return description; } set { description = value; OnPropertyChanged("Description"); } }
        public object Value { get { return value; } set { this.value = value; OnPropertyChanged("Value"); } }
        public System.Windows.Visibility Visibility { get { return visibility; } set { visibility = value; OnPropertyChanged("Visibility"); } }
        public IValueConverter Converter { get { return converter; } set { converter = value; OnPropertyChanged("Converter"); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

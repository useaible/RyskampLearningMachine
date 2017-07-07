using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailPoCSimple
{
    public class TestViewModel : INotifyPropertyChanged
    {
        private string name;
        public string TestName { get { return name; } set { name = value; OnPropertyChanged("TestName"); } }

        private double testValue;
        public double TestValue { get { return testValue; } set { testValue = value; OnPropertyChanged("TestValue"); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

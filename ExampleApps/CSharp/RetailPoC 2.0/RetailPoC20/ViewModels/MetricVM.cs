using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailPoC20.ViewModels
{
    public class MetricVM : INotifyPropertyChanged
    {
        private int id;
        public int ID
        {
            get { return id; }
            set { id = value; onPropertyChanged(this, "ID"); }
        }

        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; onPropertyChanged(this, "Name"); }
        }

        private int value = 10;
        public int Value
        {
            get { return value; }
            set { this.value = value; onPropertyChanged(this, "Value"); }
        }

        // Declare the PropertyChanged event
        public event PropertyChangedEventHandler PropertyChanged;

        // OnPropertyChanged will raise the PropertyChanged event passing the
        // source property that is being updated.
        private void onPropertyChanged(object sender, string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                PropertyChanged(sender, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}

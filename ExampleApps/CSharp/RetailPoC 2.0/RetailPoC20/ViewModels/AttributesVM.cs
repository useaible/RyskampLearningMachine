using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailPoC.ViewModels
{
    public class AttributesVM : INotifyPropertyChanged
    {
        private int id;
        public int ID
        {
            get { return id; }
            set { id = value; onPropertyChanged(this, "ID"); }
        }

        private double metric1;
        public double Metric1
        {
            get { return metric1; }
            set { metric1 = value;  onPropertyChanged(this, "Metric1"); }
        }

        private double metric2;
        public double Metric2
        {
            get { return metric2; }
            set { metric2 = value; onPropertyChanged(this, "Metric2"); }
        }

        private double metric3;
        public double Metric3
        {
            get { return metric3; }
            set { metric3 = value; onPropertyChanged(this, "Metric3"); }
        }

        private double metric4;
        public double Metric4
        {
            get { return metric4; }
            set { metric4 = value; onPropertyChanged(this, "Metric4"); }
        }

        private double metric5;
        public double Metric5
        {
            get { return metric5; }
            set { metric5 = value; onPropertyChanged(this, "Metric5"); }
        }

        private double metric6;
        public double Metric6
        {
            get { return metric6; }
            set { metric6 = value; onPropertyChanged(this, "Metric6"); }
        }

        private double metric7;
        public double Metric7
        {
            get { return metric7; }
            set { metric7 = value; onPropertyChanged(this, "Metric7"); }
        }

        private double metric8;
        public double Metric8
        {
            get { return metric8; }
            set { metric8 = value; onPropertyChanged(this, "Metric8"); }
        }

        private double metric9;
        public double Metric9
        {
            get { return metric9; }
            set { metric9 = value; onPropertyChanged(this, "Metric9"); }
        }

        private double metric10;
        public double Metric10
        {
            get { return metric10; }
            set { metric10 = value; onPropertyChanged(this, "Metric10"); }
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

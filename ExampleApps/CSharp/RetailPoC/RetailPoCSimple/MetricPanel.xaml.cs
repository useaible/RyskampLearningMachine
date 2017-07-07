using RetailPoCSimple.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using System.ComponentModel;

namespace RetailPoCSimple
{
    /// <summary>
    /// doubleeraction logic for MetricPanel2.xaml
    /// </summary>
    public partial class MetricPanel : MetroWindow
    {
        public double Metric1 { get; set; } // Storage for Metric1
        public double Metric2 { get; set; } // Storage for Metric2
        public double Metric3 { get; set; } // Storage for Metric3
        public double Metric4 { get; set; } // Storage for Metric4
        public double Metric5 { get; set; } // Storage for Metric5

        public string TotalStr { get { return "Total: " + Total + "%"; } } // The display value for total bound to totalLbl control
        public double Total { get { return (Metric1 + Metric2 + Metric3 + Metric4 + Metric5); } } // The total of all metrics

        public MetricPanel()
        {
            InitializeComponent();

            // Setting of initial values
            setMetricValues(); // Get the current metric values
        }

        // Set values to metric sliders
        public void SetMetricSliderValues(SimulationSettings settings)
        {
            this.metricSlider1.Value = settings.Metric1; // for metric1 slider
            this.metricSlider2.Value = settings.Metric2; // for metric2 slider
            this.metricSlider3.Value = settings.Metric3; // for metric3 slider
            this.metricSlider4.Value = settings.Metric4; // for metric4 slider
            this.metricSlider5.Value = settings.Metric5; // for metric5 slider
            // Storing values
            setMetricValues();
        }

        private void cancelBtn_Click(object sender, RoutedEventArgs e)
        {
            // Closing the Metric Dialog (this)
            this.DialogResult = false;
            this.Close();
        }

        // Ok/Save button click event
        private void saveMetricBtn_Click(object sender, RoutedEventArgs e)
        {
            // Call this function to get the new metric values
            setMetricValues();

            double perfect = Total / 100;
            if(perfect == 1)
            {
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Metrics must be a total of 100%.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Metric1 slider event after setting new value
        private void metricSlider1_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Slider slider = sender as Slider;
            getMetricValue(slider, 1); // Get the current value being set on the slider for metric1
        }

        // Metric2 slider event after setting new value
        private void metricSlider2_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Slider slider = sender as Slider;
            getMetricValue(slider, 2); // Get the current value being set on the slider for metric2
        }

        // Metric3 slider event after setting new value
        private void metricSlider3_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Slider slider = sender as Slider;
            getMetricValue(slider, 3); // Get the current value being set on the slider for metric3
        }

        // Metric4 slider event after setting new value
        private void metricSlider4_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Slider slider = sender as Slider;
            getMetricValue(slider, 4); // Get the current value being set on the slider for metric4
        }

        // Metric5 slider event after setting new value
        private void metricSlider5_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Slider slider = sender as Slider;
            getMetricValue(slider, 5); // Get the current value being set on the slider for metric5
        }

        private void setMetricValues()
        {
            // Storing the values from the sliders to its corresponding storage variables
            Metric1 = this.metricSlider1.Value; // for metric1
            Metric2 = this.metricSlider2.Value; // for metric2
            Metric3 = this.metricSlider3.Value; // for metric3
            Metric4 = this.metricSlider4.Value; // for metric4
            Metric5 = this.metricSlider5.Value; // for metric5

            // Update text at the end of the slider to show how much percentage does each slider set
            this.percent1.Content = Metric1 + "%"; // for metric1
            this.percent2.Content = Metric2 + "%"; // for metric2
            this.percent3.Content = Metric3 + "%"; // for metric3
            this.percent4.Content = Metric4 + "%"; // for metric4
            this.percent5.Content = Metric5 + "%"; // for metric5

            // Update text to show the total of all metrics
            this.totalLbl.Content = TotalStr;
        }

        // Get value of the current slider and store it
        // slider => current slider selected
        // except => excluded slider/metric for calculation
        private void getMetricValue(Slider slider, int except)
        {
            setMetricValues(); // Get the current metric values

            var val = slider.Value;

            if (Total > 100)
            {
                var total = getTotalExcept(except); 
                var newVal = 100 - total; // Get the new value of the current slider/metric

                slider.Value = newVal; // Re-assign the new value of the current slider/metric
            }

            // Storing of values
            setMetricValues();
        }

        // Getting the total metric values of the sliders with the exception of the current one
        // Current slider is excluded from the calculation so that whenever the total of the (other metric values + the current value) 
        // is over the limit which is 100, it will be easy to set the exact amount for the current slider by (100-total) = new current value.
        private double getTotalExcept(int except)
        {
            double total = 0;
            switch (except)
            {
                case 1:
                    total = (Metric2 + Metric3 + Metric4 + Metric5);
                    break;
                case 2:
                    total = (Metric1  + Metric3 + Metric4 + Metric5);
                    break;
                case 3:
                    total = (Metric1 + Metric2 + Metric4 + Metric5);
                    break;
                case 4:
                    total = (Metric1 + Metric2 + Metric3 + Metric5);
                    break;
                case 5:
                    total = (Metric1 + Metric2 + Metric3 + Metric4);
                    break;
            }

            return total;
        }
    }
}

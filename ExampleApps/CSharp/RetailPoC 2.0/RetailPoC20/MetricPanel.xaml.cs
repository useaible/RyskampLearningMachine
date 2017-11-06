using RetailPoC.Models;
using RetailPoC.ViewModels;
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

namespace RetailPoC
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
        public double Metric6 { get; set; } // Storage for Metric6
        public double Metric7 { get; set; } // Storage for Metric7
        public double Metric8 { get; set; } // Storage for Metric8
        public double Metric9 { get; set; } // Storage for Metric9
        public double Metric10 { get; set; } // Storage for Metric10

        public string TotalStr { get { return "Total: " + Total + "%"; } } // The display value for total bound to totalLbl control
        public double Total { get { return (Metric1 + Metric2 + Metric3 + Metric4 + Metric5 + Metric6 + Metric7 + Metric8 + Metric9 + Metric10); } } // The total of all metrics

        public MetricPanel(List<string> metrics)
        {
            InitializeComponent();

            // Setting of initial values
            SelectedMetrics = metrics;
            DrawCheckboxes();
            setMetricValues(); // Get the current metric values
        }

        // Set values to metric sliders
        public void SetMetricSliderValues(RPOCSimulationSettings settings)
        {
            var selectedMetrics = GetIncludedMetrics();

            foreach (var m in selectedMetrics)
            {
                switch (m.Key)
                {
                    case "Metric1":
                        this.metricSlider1.Value = settings.Metric1; // for metric1 slider
                        break;
                    case "Metric2":
                        this.metricSlider2.Value = settings.Metric2; // for metric2 slider
                        break;
                    case "Metric3":
                        this.metricSlider3.Value = settings.Metric3; // for metric3 slider
                        break;
                    case "Metric4":
                        this.metricSlider4.Value = settings.Metric4; // for metric4 slider
                        break;
                    case "Metric5":
                        this.metricSlider5.Value = settings.Metric5; // for metric5 slider
                        break;
                    case "Metric6":
                        this.metricSlider6.Value = settings.Metric6; // for metric6 slider
                        break;
                    case "Metric7":
                        this.metricSlider7.Value = settings.Metric7; // for metric7 slider
                        break;
                    case "Metric8":
                        this.metricSlider8.Value = settings.Metric8; // for metric8 slider
                        break;
                    case "Metric9":
                        this.metricSlider9.Value = settings.Metric9; // for metric9 slider
                        break;
                    case "Metric10":
                        this.metricSlider10.Value = settings.Metric10; // for metric10 slider
                        break;
                }
            }
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
            if (perfect == 1)
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

        // Metric6 slider event after setting new value
        private void metricSlider6_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Slider slider = sender as Slider;
            getMetricValue(slider, 6); // Get the current value being set on the slider for metric6
        }

        // Metric7 slider event after setting new value
        private void metricSlider7_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Slider slider = sender as Slider;
            getMetricValue(slider, 7); // Get the current value being set on the slider for metric7
        }

        // Metric8 slider event after setting new value
        private void metricSlider8_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Slider slider = sender as Slider;
            getMetricValue(slider, 8); // Get the current value being set on the slider for metric8
        }

        // Metric9 slider event after setting new value
        private void metricSlider9_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Slider slider = sender as Slider;
            getMetricValue(slider, 9); // Get the current value being set on the slider for metric9
        }

        // Metric10 slider event after setting new value
        private void metricSlider10_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Slider slider = sender as Slider;
            getMetricValue(slider, 10); // Get the current value being set on the slider for metric10
        }

        private void setMetricValues()
        {

            var metrics = GetIncludedMetrics();

            foreach(var m in metrics)
            {
                switch (m.Key)
                {
                    case "Metric1":
                        Metric1 = this.metricSlider1.Value; // for metric1
                        this.percent1.Content = Metric1 + "%"; // for metric1
                        break;
                    case "Metric2":
                        Metric2 = this.metricSlider2.Value; // for metric2
                        this.percent2.Content = Metric2 + "%"; // for metric2
                        break;
                    case "Metric3":
                        Metric3 = this.metricSlider3.Value; // for metric3
                        this.percent3.Content = Metric3 + "%"; // for metric3
                        break;
                    case "Metric4":
                        Metric4 = this.metricSlider4.Value; // for metric4
                        this.percent4.Content = Metric4 + "%"; // for metric4
                        break;
                    case "Metric5":
                        Metric5 = this.metricSlider5.Value; // for metric5
                        this.percent5.Content = Metric5 + "%"; // for metric5
                        break;
                    case "Metric6":
                        Metric6 = this.metricSlider6.Value; // for metric6
                        this.percent6.Content = Metric6 + "%"; // for metric6
                        break;
                    case "Metric7":
                        Metric7 = this.metricSlider7.Value; // for metric7
                        this.percent7.Content = Metric7 + "%"; // for metric7
                        break;
                    case "Metric8":
                        Metric8 = this.metricSlider8.Value; // for metric8
                        this.percent8.Content = Metric8 + "%"; // for metric8
                        break;
                    case "Metric9":
                        Metric9 = this.metricSlider9.Value; // for metric9
                        this.percent9.Content = Metric9 + "%"; // for metric9
                        break;
                    case "Metric10":
                        Metric10 = this.metricSlider10.Value; // for metric10
                        this.percent10.Content = Metric10 + "%"; // for metric10
                        break;
                }
            }

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
                    //total = (Metric2 + Metric3 + Metric4 + Metric5 + Metric6 + Metric7 + Metric8 + Metric9 + Metric10);


                    var includedList = GetIncludedMetrics().Where(a => a.Key != "Metric1");
                    List<double> includedValues = includedList.Select(a=>a.Value).ToList();
                    total = includedValues.Sum();

                    break;
                case 2:
                    //total = (Metric1  + Metric3 + Metric4 + Metric5 + Metric6 + Metric7 + Metric8 + Metric9 + Metric10);

                    var includedList2 = GetIncludedMetrics().Where(a => a.Key != "Metric2");
                    List<double> includedValues2 = includedList2.Select(a => a.Value).ToList();
                    total = includedValues2.Sum();
                    break;
                case 3:
                    //total = (Metric1 + Metric2 + Metric4 + Metric5 + Metric6 + Metric7 + Metric8 + Metric9 + Metric10);

                    var includedList3 = GetIncludedMetrics().Where(a => a.Key != "Metric3");
                    List<double> includedValues3 = includedList3.Select(a => a.Value).ToList();
                    total = includedValues3.Sum();
                    break;
                case 4:
                    //total = (Metric1 + Metric2 + Metric3 + Metric5 + Metric6 + Metric7 + Metric8 + Metric9 + Metric10);

                    var includedList4 = GetIncludedMetrics().Where(a => a.Key != "Metric4");
                    List<double> includedValues4 = includedList4.Select(a => a.Value).ToList();
                    total = includedValues4.Sum();
                    break;
                case 5:
                    //total = (Metric1 + Metric2 + Metric3 + Metric4 + Metric6 + Metric7 + Metric8 + Metric9 + Metric10);

                    var includedList5 = GetIncludedMetrics().Where(a => a.Key != "Metric5");
                    List<double> includedValues5 = includedList5.Select(a => a.Value).ToList();
                    total = includedValues5.Sum();
                    break;
                case 6:
                    //total = (Metric1 + Metric2 + Metric3 + Metric4 + Metric5 + Metric7 + Metric8 + Metric9 + Metric10);

                    var includedList6 = GetIncludedMetrics().Where(a => a.Key != "Metric6");
                    List<double> includedValues6 = includedList6.Select(a => a.Value).ToList();
                    total = includedValues6.Sum();
                    break;
                case 7:
                    //total = (Metric1 + Metric2 + Metric3 + Metric4 + Metric5 + Metric6 + Metric8 + Metric9 + Metric10);

                    var includedList7 = GetIncludedMetrics().Where(a => a.Key != "Metric7");
                    List<double> includedValues7 = includedList7.Select(a => a.Value).ToList();
                    total = includedValues7.Sum();
                    break;
                case 8:
                    //total = (Metric1 + Metric2 + Metric3 + Metric4 + Metric5 + Metric6 + Metric7 + Metric9 + Metric10);

                    var includedList8 = GetIncludedMetrics().Where(a => a.Key != "Metric8");
                    List<double> includedValues8 = includedList8.Select(a => a.Value).ToList();
                    total = includedValues8.Sum();
                    break;
                case 9:
                    //total = (Metric1 + Metric2 + Metric3 + Metric4 + Metric5 + Metric6 + Metric7 + Metric8 + Metric10);

                    var includedList9 = GetIncludedMetrics().Where(a => a.Key != "Metric9");
                    List<double> includedValues9 = includedList9.Select(a => a.Value).ToList();
                    total = includedValues9.Sum();
                    break;
                case 10:
                    //total = (Metric1 + Metric2 + Metric3 + Metric4 + Metric5 + Metric6 + Metric7 + Metric8 + Metric9);

                    var includedList10 = GetIncludedMetrics().Where(a => a.Key != "Metric10");
                    List<double> includedValues10 = includedList10.Select(a => a.Value).ToList();
                    total = includedValues10.Sum();
                    break;
            }

            return total;
        }

        public List<string> SelectedMetrics { get; set; }
        private void DrawCheckboxes()
        {
            var distance = 21;
            for(int i = 0; i < 10; i++)
            {
                CheckBox cb = new CheckBox();

                cb.HorizontalAlignment = HorizontalAlignment.Left;
                cb.VerticalAlignment = VerticalAlignment.Top;
                cb.Margin = new Thickness(5, distance, 0, 0);

                cb.Name = $"Metric{i + 1}";

                if(SelectedMetrics != null && SelectedMetrics.Count > 0 && SelectedMetrics.IndexOf(cb.Name) >= 0)
                {
                    cb.IsChecked = true;
                }
                else
                {
                    DisableMetricSlider(cb.Name);
                }

                cb.Checked += (sender, evt)=> {
                    CheckBox selected = (CheckBox)sender;
                    SelectedMetrics.Add(selected.Name);
                    EnableMetricSlider(selected.Name);
                };

                cb.Unchecked += (sender, evt) => {
                    CheckBox selected = (CheckBox)sender;
                    SelectedMetrics.Remove(selected.Name);
                    DisableMetricSlider(selected.Name);
                };

                Grid.SetColumn(cb, 4);
                mainGrid.Children.Add(cb);

                distance += 25;
            }
        }

        private Dictionary<string, double> GetIncludedMetrics()
        {
            Dictionary<string, double> includedList = new Dictionary<string, double>();
            if (SelectedMetrics != null && SelectedMetrics.Count > 0)
            {
                foreach (var m in SelectedMetrics)
                {
                    switch (m)
                    {
                        case "Metric1":
                            includedList.Add("Metric1", Metric1);
                            break;
                        case "Metric2":
                            includedList.Add("Metric2", Metric2);
                            break;
                        case "Metric3":
                            includedList.Add("Metric3", Metric3);
                            break;
                        case "Metric4":
                            includedList.Add("Metric4", Metric4);
                            break;
                        case "Metric5":
                            includedList.Add("Metric5", Metric5);
                            break;
                        case "Metric6":
                            includedList.Add("Metric6", Metric6);
                            break;
                        case "Metric7":
                            includedList.Add("Metric7", Metric7);
                            break;
                        case "Metric8":
                            includedList.Add("Metric8", Metric8);
                            break;
                        case "Metric9":
                            includedList.Add("Metric9", Metric9);
                            break;
                        case "Metric10":
                            includedList.Add("Metric10", Metric10);
                            break;
                    }
                }
            }

            return includedList;
        }

        private void DisableMetricSlider(string name)
        {
            switch (name)
            {
                case "Metric1":
                    metricSlider1.IsEnabled = false;
                    break;
                case "Metric2":
                    metricSlider2.IsEnabled = false;
                    break;
                case "Metric3":
                    metricSlider3.IsEnabled = false;
                    break;
                case "Metric4":
                    metricSlider4.IsEnabled = false;
                    break;
                case "Metric5":
                    metricSlider5.IsEnabled = false;
                    break;
                case "Metric6":
                    metricSlider6.IsEnabled = false;
                    break;
                case "Metric7":
                    metricSlider7.IsEnabled = false;
                    break;
                case "Metric8":
                    metricSlider8.IsEnabled = false;
                    break;
                case "Metric9":
                    metricSlider9.IsEnabled = false;
                    break;
                case "Metric10":
                    metricSlider10.IsEnabled = false;
                    break;
            }
        }

        private void EnableMetricSlider(string name)
        {
            switch (name)
            {
                case "Metric1":
                    metricSlider1.IsEnabled = true;
                    break;
                case "Metric2":
                    metricSlider2.IsEnabled = true;
                    break;
                case "Metric3":
                    metricSlider3.IsEnabled = true;
                    break;
                case "Metric4":
                    metricSlider4.IsEnabled = true;
                    break;
                case "Metric5":
                    metricSlider5.IsEnabled = true;
                    break;
                case "Metric6":
                    metricSlider6.IsEnabled = true;
                    break;
                case "Metric7":
                    metricSlider7.IsEnabled = true;
                    break;
                case "Metric8":
                    metricSlider8.IsEnabled = true;
                    break;
                case "Metric9":
                    metricSlider9.IsEnabled = true;
                    break;
                case "Metric10":
                    metricSlider10.IsEnabled = true;
                    break;
            }
        }
    }
}

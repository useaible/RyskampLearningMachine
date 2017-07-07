using RLV.Core.Interfaces;
using RLV.Core.Models;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;

namespace WPFVisualizer
{
    /// <summary>
    /// Interaction logic for MainVisualizerWindow.xaml
    /// </summary>
    public partial class MainVisualizerWindow : UserControl
    {
        public MainVisualizerWindow()
        {
            InitializeComponent();

            BindingOperations.SetBinding(chartHeader, Expander.HeaderProperty, new Binding { Source = progressionChartControl.ViewModel, Path = new PropertyPath("Header"), Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(detailsHeader, Expander.HeaderProperty, new Binding { Source = detailsControl.ViewModel, Path = new PropertyPath("Header"), Mode = BindingMode.TwoWay });
        }

        public IRLVProgressionChartPanel ChartControl { get { return progressionChartControl; } }
        public IRLVSelectedDetailsPanel  DetailsControl { get { return detailsControl; } }


        private void hideBtn_Click(object sender, RoutedEventArgs e)
        {
            if(this.Parent.GetType().BaseType != null)
            {
                if(this.Parent.GetType().BaseType.BaseType != null)
                {
                    ((Window)this.Parent).Visibility = Visibility.Hidden;
                }
                else
                {
                    ((Window)this.Parent).Visibility = Visibility.Hidden;
                }
            }
        }
    }
}

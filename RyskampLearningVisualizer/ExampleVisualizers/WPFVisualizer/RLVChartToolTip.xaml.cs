using LiveCharts.Wpf;
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
using LiveCharts;
using System.ComponentModel;

namespace WPFVisualizer
{
    /// <summary>
    /// Interaction logic for RLVChartToolTip.xaml
    /// </summary>
    public partial class RLVChartToolTip : UserControl, IChartTooltip
    {
        private TooltipData _data;
        public RLVChartToolTip()
        {
            InitializeComponent();

            DataContext = this;
        }

        public TooltipData Data
        {
            get
            {
                return _data;
            }

            set
            {
                _data = value;
                OnPropertyChanged("Data");
            }
        }

        public TooltipSelectionMode? SelectionMode { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

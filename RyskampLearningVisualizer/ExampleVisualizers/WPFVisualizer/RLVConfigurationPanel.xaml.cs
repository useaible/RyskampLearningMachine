using RLV.Core.Interfaces;
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
using System.ComponentModel;

namespace WPFVisualizer
{
    /// <summary>
    /// Interaction logic for RLVConfigurationPanel.xaml
    /// </summary>
    public partial class RLVConfigurationPanel
    {
        private IRLVProgressionChartPanel chartControl = null;
        private IRLVSelectedDetailsPanel detailsControl = null;
        public RLVConfigurationPanel(IRLVProgressionChartPanel chartControl, IRLVSelectedDetailsPanel detailsControl)
        {
            InitializeComponent();

            this.chartControl = chartControl;
            this.detailsControl = detailsControl;

            chartConfig.PopulateControls(this.chartControl);
            detailsConfig.PopulateControls(this.detailsControl);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            this.chartControl.SaveConfiguration();
            this.detailsControl.SaveConfiguration();
            base.OnClosing(e);
        }
    }
}

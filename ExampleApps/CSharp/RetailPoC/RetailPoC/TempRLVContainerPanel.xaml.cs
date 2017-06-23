using RLV.Core;
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
using System.Windows.Shapes;
using WPFVisualizer;

namespace RetailPoC
{
    /// <summary>
    /// Interaction logic for TempRLVContainerPanel.xaml
    /// </summary>
    public partial class TempRLVContainerPanel
    {
        private IRLVCore core;
        private IRLVScaleSelectionPanel detailsScalePanel;
        private IRLVScaleSelectionPanel chartScalePanel;
        private IRLVOutputVisualizer visualizer;

        public TempRLVContainerPanel(IRLVCore core, IRLVOutputVisualizer visualizer)
        {            
            InitializeComponent();

            this.core = core;
            this.visualizer = visualizer;

            IRLVScaleSelectionVM scaleVM = new RLVScaleSelectionVM();

            detailsScalePanel = detailsControl.ScalePanel;
            chartScalePanel = progressionChartControl.ScalePanel;

            detailsScalePanel.SetViewModel(scaleVM);
            chartScalePanel.SetViewModel(scaleVM);

            core.SetupVisualizer(new List<IRLVPanel>
            {
                detailsControl,
                progressionChartControl,
                chartScalePanel,
                detailsScalePanel
            }, visualizer);
        }

        private void hideVBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Hidden;
        }
    }
}

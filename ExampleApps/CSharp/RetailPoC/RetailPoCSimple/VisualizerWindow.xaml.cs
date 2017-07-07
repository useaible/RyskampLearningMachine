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
using System.ComponentModel;

namespace RetailPoCSimple
{
    /// <summary>
    /// Interaction logic for VisualizerWindow.xaml
    /// </summary>
    public partial class VisualizerWindow
    {
        private IRLVCore core;
        private IRLVScaleSelectionPanel detailsScalePanel;
        private IRLVScaleSelectionPanel chartScalePanel;
        private IRLVOutputVisualizer visualizer;
        public VisualizerWindow(IRLVCore core, IRLVOutputVisualizer visualizer)
        {
            InitializeComponent();

            this.core = core;
            this.visualizer = visualizer;

            IRLVScaleSelectionVM scaleVM = new RLVScaleSelectionVM();

            detailsScalePanel = rlv.DetailsControl.ScalePanel;
            chartScalePanel = rlv.ChartControl.ScalePanel;

            detailsScalePanel.SetViewModel(scaleVM);
            chartScalePanel.SetViewModel(scaleVM);
            scaleVM.DefaultScale = 100;

            core.SetupVisualizer(new List<IRLVPanel>
            {
                rlv.DetailsControl,
                rlv.ChartControl,
                chartScalePanel,
                detailsScalePanel
            }, visualizer);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }
    }
}

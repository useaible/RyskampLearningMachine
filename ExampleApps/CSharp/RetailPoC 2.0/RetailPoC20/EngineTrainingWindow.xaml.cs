using RetailPoC.Models;
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

namespace RetailPoC
{
    /// <summary>
    /// Interaction logic for EngineTrainingWindow.xaml
    /// </summary>
    public partial class EngineTrainingWindow
    {
        private MainWindow main;

        public EngineTrainingWindow()
        {
            InitializeComponent();
            comparisonGrid.MouseEnter += ComparisonGrid_MouseEnter;
            comparisonGrid.MouseLeave += ComparisonGrid_MouseLeave;
        }

        private void ComparisonGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            comparisonGrid.Visibility = Visibility.Hidden;
        }

        private void ComparisonGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            comparisonGrid.Visibility = Visibility.Visible;
        }

        public EngineTrainingWindow(MainWindow main) : this()
        {
            this.main = main;
            main.InitializeGrid(planogram, Colors.LightGray);
        }
                
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void closeComparisonLink_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            main.CloseComparisonLink_PreviewMouseDown(sender, e);
        }

        private void MetroWindow_LocationChanged(object sender, EventArgs e)
        {
            main.AlignPanels();
        }
    }
}

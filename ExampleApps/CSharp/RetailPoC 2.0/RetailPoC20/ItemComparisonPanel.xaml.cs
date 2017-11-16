using RetailPoC20.Models;
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

namespace RetailPoC20
{
    /// <summary>
    /// Interaction logic for ItemComparisonPanel.xaml
    /// </summary>
    public partial class ItemComparisonPanel
    {
        private ShelfItem currentItem;
        private ShelfItem prevItem;

        public ItemComparisonPanel()
        {
            InitializeComponent();
        }
        
        public void SetItems(ShelfItem current, ShelfItem prev)
        {
            currentItem = current;
            prevItem = prev;

            rectCurr.Fill = new SolidColorBrush(current.Color);
            txtCurrName.Text = current.Name;
            txtCurrScore.Text = current.Score.ToString("#,###.##");

            rectPrev.Fill = new SolidColorBrush(prev.Color);
            txtPrevName.Text = prev.Name;
            txtPrevScore.Text = prev.Score.ToString("#,###.##");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}

using RLM.Models;
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

namespace WPFVisualizer
{
    /// <summary>
    /// Interaction logic for RLVSessionScoreBreakdown.xaml
    /// </summary>
    public partial class RLVSessionScoreBreakdown
    {
        public RLVSessionScoreBreakdown(IEnumerable<IRLVItemDisplay> breakdown)
        {
            InitializeComponent();

            if (breakdown != null)
            {
                var nameCol = new DataGridTextColumn { Header = "Metric", Binding = new Binding("Name"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), IsReadOnly = true };
                var valueCol = new DataGridTextColumn { Header = "Value", Binding = new Binding("ValueStr"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), IsReadOnly = true };

                var setter = new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right);
                var style = new Style(typeof(TextBlock));
                style.Setters.Add(setter);

                valueCol.ElementStyle = style;

                this.sessionBreakdownDetailsGrid.Columns.Add(nameCol);
                this.sessionBreakdownDetailsGrid.Columns.Add(valueCol);

                var itemSource = breakdown.Select(a =>
                {
                    return new RLVSessionScoreBreakdownVM { Name = a.Name, Value = (double)a.Value };
                }).ToList();

                double total = 0;

                foreach (var t in itemSource)
                {
                    total += Convert.ToDouble(t.Value);
                }

                this.sessionBreakdownTotalLbl.Text = $"Total: {total.ToString("#,##0.##")}";
                this.sessionBreakdownDetailsGrid.ItemsSource = itemSource;
                this.sessionBreakdownDetailsGrid.AutoGenerateColumns = false;
            }
        }
    }
}

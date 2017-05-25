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

namespace RetailPoC
{
    /// <summary>
    /// Interaction logic for DataPanel.xaml
    /// </summary>
    public partial class DataPanel : Window
    {
        private DataFactory dataFactory = new DataFactory(); // This is where items are queried from the database
        public List<ItemVM> Items { get; set; } // Storage for the items loaded on the datagrid
        public DataPanel()
        {
            InitializeComponent();
            DataProgressBar.Visibility = Visibility.Hidden;
        }

        private void textBox_KeyUp(object sender, KeyEventArgs e)
        {
            var searched = this.searchItemBox.Text.ToLower(); // The keyword for searching [SKU or Name]

            if (string.IsNullOrEmpty(searched)) // Check if keyword is empty
            {
                this.dataGrid.ItemsSource = this.Items; // Keyword is empty, so reset the datagrid to show all items
            }
            else
            {
                // Get the new datagrid items
                var newList = this.Items.Where(a => a.SKU.ToLower() == searched || a.Name.ToLower() == searched).ToList();

                this.dataGrid.ItemsSource = newList; // Assign the new items found to the datagrid
            }
        }

        private async void generateDataBtn_Click(object sender, RoutedEventArgs e)
        {
            using (PlanogramContext context = new PlanogramContext())
            {
                MockData data = new MockData(context);

                data.NumItems = this.NumItems;

                generateDataBtn.IsEnabled = false;
                DataProgressBar.Visibility = Visibility.Visible;
                //todo: ask for confirmation
                if (context.Items.Count() > 0)
                {
                    var confirmation = MessageBox.Show("Warning! You currently have an existing database. Generating a new set of data will overwrite the current one. Proceed?", "Confirmation", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                    if (confirmation == MessageBoxResult.OK)
                    {
                        //todo: dropDb and generate a new data set
                        context.Database.Delete();
                        context.Database.CreateIfNotExists();

                        this.dataGrid.ItemsSource = dataFactory.Items;
                        this.Items = dataFactory.Items;

                        data.Progress += dataPanelProgress;
                        data.Refreshdata += refreshDataGrid;
                        await data.Generate();
                    }
                    else
                    {
                        generateDataBtn.IsEnabled = true;
                        DataProgressBar.Visibility = Visibility.Hidden;
                    }
                }
                else
                {
                    data.Progress += dataPanelProgress;
                    data.Refreshdata += refreshDataGrid;
                    await data.Generate();
                }
            }
        }

        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = sender as DataGridRow; // Get the selected row
            ItemVM itemvm = row.Item as ItemVM; // Convert the selected row to the object bound to
            var id = itemvm.ID; // Get the item id

            Item item;
            using (PlanogramContext ctx = new PlanogramContext())
            {
                item = ctx.Items.Include("Attributes").FirstOrDefault(a => a.ID == id); // Get the selected item with attributes
            }
            var attrs = item.Attributes; // Get the item attributes

            ItemAttributesPanel panel = new ItemAttributesPanel(); // Instantiate the dialog to show the attributes

            panel.itemAttributesGrid.AutoGenerateColumns = false;
            panel.itemAttributesGrid.ItemsSource = attrs; // Set the data source for the attributes grid

            // Binding and setting of columns for the datagrid
            panel.itemAttributesGrid.Columns.Add(new DataGridTextColumn() { Header = "ID", Binding = new Binding("ID"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), IsReadOnly = true });
            panel.itemAttributesGrid.Columns.Add(new DataGridTextColumn() { Header = "Metric1", Binding = new Binding("Metric1"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), IsReadOnly = true });
            panel.itemAttributesGrid.Columns.Add(new DataGridTextColumn() { Header = "Metric2", Binding = new Binding("Metric2"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), IsReadOnly = true });
            panel.itemAttributesGrid.Columns.Add(new DataGridTextColumn() { Header = "Metric3", Binding = new Binding("Metric3"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), IsReadOnly = true });
            panel.itemAttributesGrid.Columns.Add(new DataGridTextColumn() { Header = "Metric4", Binding = new Binding("Metric4"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), IsReadOnly = true });
            panel.itemAttributesGrid.Columns.Add(new DataGridTextColumn() { Header = "Metric5", Binding = new Binding("Metric5"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), IsReadOnly = true });
            panel.itemAttributesGrid.Columns.Add(new DataGridTextColumn() { Header = "Metric6", Binding = new Binding("Metric6"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), IsReadOnly = true });
            panel.itemAttributesGrid.Columns.Add(new DataGridTextColumn() { Header = "Metric7", Binding = new Binding("Metric7"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), IsReadOnly = true });
            panel.itemAttributesGrid.Columns.Add(new DataGridTextColumn() { Header = "Metric8", Binding = new Binding("Metric8"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), IsReadOnly = true });
            panel.itemAttributesGrid.Columns.Add(new DataGridTextColumn() { Header = "Metric9", Binding = new Binding("Metric9"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), IsReadOnly = true });
            panel.itemAttributesGrid.Columns.Add(new DataGridTextColumn() { Header = "Metric10", Binding = new Binding("Metric10"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), IsReadOnly = true });

            panel.Width = 1500; // Set dialog width
            panel.ShowDialog();

            // Some operations with this row
        }

        private void refreshBtn_Click(object sender, RoutedEventArgs e)
        {
            // Re-query from the database the list of items and store it to local variables
            refreshDataGrid(true);
        }

        void dataPanelProgress(double value)
        {
            DataProgressBar.Value = value;
        }

        void refreshDataGrid(bool refresh)
        {
            if (refresh)
            {
                this.dataGrid.ItemsSource = dataFactory.Items;
                this.Items = dataFactory.Items;
                generateDataBtn.IsEnabled = true;
                DataProgressBar.Value = 0;
                DataProgressBar.Visibility = Visibility.Hidden;
            }
        }

        public int NumItems { get; set; }
        public int NumShelves { get; set; }
        public int NumSlots { get; set; }
    }
}

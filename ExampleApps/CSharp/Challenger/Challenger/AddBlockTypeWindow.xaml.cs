using ChallengerLib.Models;
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
using static Challenger.Models.Utilities;


namespace Challenger
{
    /// <summary>
    /// Interaction logic for AddBlockTypeWindow.xaml
    /// </summary>
    public partial class AddBlockTypeWindow : Window
    {
        public AddBlockTypeWindow()
        {
            InitializeComponent();

            List<ChallengerIcon> imgFileNames = GetAllIcons(@"Icons");

            RefreshIcons(BlockIconListView);
            BlockIconListView.SelectedIndex = 0;
        }

        private string selectedIconPath;
        public ChallengerLib.Models.BlockTemplate NewBlockTemplate { get; set; }

        private void BlockIconGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (sender as ListView).SelectedItem;
            if (item != null)
            {
                ChallengerIcon iconPath = (ChallengerIcon)item;
                selectedIconPath = iconPath.Name;
            }
        }

        private void OkayButton_Click(object sender, RoutedEventArgs e)
        {
            int n;
            if (NameField.Text == "" || selectedIconPath == null)
            {
                MessageBox.Show("At least one of the required parameters for Block Type Creation is empty.");
            }
            else if (NameField.Text.ToUpper() == "DEFAULT")
            {
                MessageBox.Show($"{NameField.Text} cannot be the Name of the Block Type");
            }

            else
            {
                NewBlockTemplate = new ChallengerLib.Models.BlockTemplate()
                {
                    Icon = selectedIconPath,
                    Name = NameField.Text,

                };

                DialogResult = true;
                this.Close();
            }

            using (ChallengerLib.Data.ChallengerDbEntities dbEnt = new ChallengerLib.Data.ChallengerDbEntities())
            {
                dbEnt.BlockTemplates.Add(NewBlockTemplate);
                dbEnt.SaveChanges();
            }

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Cancel();
        }

        private void Cancel()
        {
            DialogResult = false;
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DialogResult = false;

        }

        private void IconUploadButton_Click(object sender, RoutedEventArgs e)
        {
            Models.Utilities.SelectIconToCopy();
            Models.Utilities.RefreshIcons(BlockIconListView);
            BlockIconListView.SelectedIndex = 0;
        }


    }
}

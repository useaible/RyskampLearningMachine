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
using ChallengerLib.Models;
using System.IO;
using System.Drawing;
using Microsoft.Win32;
using static Challenger.Models.Utilities;

namespace Challenger
{
    /// <summary>
    /// Interaction logic for NewEditBoxWindow.xaml
    /// </summary>
    public partial class NewEditBoxWindow
    {
        public NewEditBoxWindow(IEnumerable<string> simObjectNameList)
        {

            InitializeComponent();

            simObjectCount = simObjectNameList.Count();
            simObjectNames = simObjectNameList;

            RefreshBlockTypes(BlockTypeListView);
            RefreshIcons(BlockIconListView);

            int i = 0;
            foreach (BlockTemplate item in BlockTypeListView.Items)
            {
                // Hard coded default Block Type
                if (item.Name == "Basic")
                {
                    break;
                }
                i++;
            }
            BlockTypeListView.SelectedIndex = i;

            int j = 0;
            foreach (ChallengerIcon b in BlockIconListView.Items)
            {
                if (b.Name == selectedBlockTemplate.Icon)
                {
                    break;
                }
                j++;
            }
            BlockIconListView.SelectedIndex = j;

            if (NewBlock == null)
            {
                NewBlock = true;
                
            }

            // hide Add Block Type
            //BlockTypeAddButton.Visibility = Visibility.Hidden;

            GroupIcons();
        }

        private int simObjectCount;
        private IEnumerable<string> simObjectNames;
        private int countOfBlocks;
        private BlockTemplate selectedBlockTemplate;
        private int selectedBlockTemplateID;
        private ChallengerIcon selectedIconPath;
        public bool? NewBlock;
        private ChallengerLib.Models.Block blockToEdit;
        private string previousName = string.Empty;

        public string BlockGUID { get; set; }

        public ChallengerLib.Models.Block CreatedBlock { get; set; }

        private void GroupIcons()
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(BlockIconListView.ItemsSource);
            PropertyGroupDescription groupDescription = new PropertyGroupDescription("Category");
            view.GroupDescriptions.Add(groupDescription);
        }

        private void BlockTypeAddButton_Click(object sender, RoutedEventArgs e)
        {
            AddBlockTypeWindow addBlockTypeWindow = new AddBlockTypeWindow();
            addBlockTypeWindow.ShowDialog();

            if ((bool)addBlockTypeWindow.DialogResult)
            {
                Models.Utilities.RefreshBlockTypes(BlockTypeListView);
            }
        }
        
        private void BlockTypeListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (sender as ListView).SelectedItem;
            if (item != null && item is BlockTemplate)
            {
                BlockTemplate bT = (BlockTemplate)item;
                selectedBlockTemplate = bT;
                selectedBlockTemplateID = bT.ID;
            }

            
        }

        private void BlockIconGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (sender as ListView).SelectedItem;
            if (item != null)
            {
                selectedIconPath = (ChallengerIcon)item;
            }
        }

        private void OkayButton_Click(object sender, RoutedEventArgs e)
        {
            int n;
            if (NameField.Text == "" || ScoreField.Text == "" || selectedIconPath == null)
            {
                MessageBox.Show("At least one of the required parameters for Block Creation is empty.");
            }
            else if (NameField.Text.ToUpper() == "DEFAULT")
            {
                MessageBox.Show($"{NameField.Text} cannot be the Name of the Block");
            }
            else if (simObjectNames.Except(new List<string> { previousName }).Contains(NameField.Text))
            {
                MessageBox.Show($"{NameField.Text} already exists. Please choose a new name.");
            }
            else if (!int.TryParse(ScoreField.Text, out n))
            {
                MessageBox.Show($"{ScoreField.Text} is not a numeric value");
            }
            else if (NewBlock == false && blockToEdit != null)
            {
                blockToEdit.Name = NameField.Text;
                blockToEdit.Score = double.Parse(ScoreField.Text);
                blockToEdit.Icon = selectedIconPath.Name;
                blockToEdit.ID = selectedBlockTemplate.ID;
                blockToEdit.IsEndSimulation = blockToEdit.ID == 2;

                CreatedBlock = blockToEdit;

                DialogResult = true;
                this.Close();
            }
            else
            {
                CreatedBlock = new ChallengerLib.Models.Block()
                {
                    Icon = selectedIconPath.Name,
                    Name = NameField.Text,
                    Score = int.Parse(ScoreField.Text),
                    ID = selectedBlockTemplateID,
                    BlockID = Guid.NewGuid().ToString("N")
                };

                //BlockGUID = Guid.NewGuid().ToString();

                CreatedBlock.IsEndSimulation = CreatedBlock.ID == 2;

                DialogResult = true;
                this.Close();
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
            //Models.Utilities.SelectIconToCopy();
            //Models.Utilities.RefreshIcons(BlockIconListView);
            //BlockIconListView.SelectedIndex = 0;

            AddIcon addIcon = new AddIcon();
            bool? result = addIcon.ShowDialog();
            if (result.HasValue && result.Value == true)
            {
                RefreshIcons(BlockIconListView);
                GroupIcons();
            }
        }

        public void SetEdit(ChallengerLib.Models.Block tmpl)
        {
            blockToEdit = tmpl;

            NewEditBoxWindow1.Title = "Edit Simulation Object Window";
            NameField.Text = tmpl.Name;
            ScoreField.Text = tmpl.Score.ToString();
            previousName = tmpl.Name;

            int blockTemplateIndex = 0;
            foreach (BlockTemplate item in BlockTypeListView.Items)
            {
                if (item.ID == tmpl.ID)
                {
                    break;
                }
                blockTemplateIndex++;
            }
            BlockTypeListView.SelectedIndex = blockTemplateIndex;
            SetSelectedIcon(tmpl, BlockIconListView);

            NewBlock = false;
            

            
        }

        private void NewEditBoxWindow1_Loaded(object sender, RoutedEventArgs e)
        {
            if (NewBlock == true)
            {
                NameField.Text = $"Object {simObjectCount + 1}";
                ScoreField.Text = "0";
            }

            
        }
    }

  
}

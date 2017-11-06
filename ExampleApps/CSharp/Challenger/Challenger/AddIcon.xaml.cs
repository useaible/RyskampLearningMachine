using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
using Challenger.Models;
using static Challenger.Models.Utilities;
using ChallengerLib.Models;

namespace Challenger
{
    /// <summary>
    /// Interaction logic for AddIcon.xaml
    /// </summary>
    public partial class AddIcon
    {
        public AddIcon()
        {
            InitializeComponent();

            iconFolder = @"Icons";
            string[] directories = Directory.GetDirectories(iconFolder);
            List<string> directoryNamesOnly = new List<string>();
            foreach (string s in directories)
            {

                string newS = s.Remove(0, iconFolder.Length + 1);
                directoryNamesOnly.Add(newS);
                
            }

            categoryDropdown.ItemsSource = directoryNamesOnly;

            // set default for Category
            foreach (string item in categoryDropdown.Items)
            {
                if (item == "Miscellaneous")
                {
                    categoryDropdown.SelectedItem = item;
                }
            }

            

        }

        string iconFolder;
        private string category;
        string iconFileName;
        string iconNameOnly;
        string targetDirectory;
        bool iconLoaded;

        public ChallengerIcon NewIcon { get; set; }

        private void browseButton_Click(object sender, RoutedEventArgs e)
        {
            var open = new OpenFileDialog();
            open.Filter = "PNG Files (*.png)|*.png";

            if ((bool)open.ShowDialog())
            {
                iconFileName = open.FileName;
                iconNameOnly = open.SafeFileName;
                
                imagePreview.Source = new BitmapImage(new Uri(iconFileName));
                iconNameLabel.Text = iconNameOnly;
                filePathLabel.Content = $"Path: {iconFileName}";

                iconLoaded = true;
            }

        }

        private void okayButton_Click(object sender, RoutedEventArgs e)
        {
            string newIconName = iconNameLabel.Text;

            if (iconLoaded == false)
            {
                MessageBox.Show("Please select an Icon to upload.");
            }
            else if (iconLoaded == true && string.IsNullOrEmpty(categoryDropdown.SelectedItem.ToString()))
            {
                MessageBox.Show("Please select a category before upload.");
            }
            else if (iconLoaded == true && !string.IsNullOrEmpty(categoryDropdown.SelectedItem.ToString()) && !string.IsNullOrEmpty(iconNameLabel.Text))
            {
                try
                {
                    category = categoryDropdown.SelectedItem.ToString();
                    string pathWithCategory = System.IO.Path.Combine(iconFolder, category);
                    targetDirectory = System.IO.Path.Combine(pathWithCategory, newIconName);

                    File.Copy(iconFileName, targetDirectory, false);

                    NewIcon = new ChallengerIcon()
                    {
                        Name = newIconName,
                        Path = targetDirectory,
                        Category = category



                    };

                    DialogResult = true;
                    this.Close();
                }
                catch (IOException)
                {

                    MessageBox.Show("Icon Name already exists. Try changing the Icon File name.");
                }



                
            }
            

            

            
        }
    }
}

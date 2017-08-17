using ChallengerLib.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Challenger.Models
{
    public static class Utilities
    {

        public static List<ChallengerIcon> GetAllIcons(string path)
        {
            DirectoryInfo d = new DirectoryInfo(path);
            List<string> dirs = new List<string>(Directory.EnumerateDirectories(d.FullName));
            //FileInfo[] Files = d.GetFiles("*.png");

            List<ChallengerIcon> fileNames = new List<ChallengerIcon>();
            foreach (var a in d.EnumerateDirectories())
            {
                FileInfo[] Files = a.GetFiles("*.png");
                foreach (FileInfo file in Files)
                {
                    fileNames.Add(new ChallengerIcon { Name = $"Icons\\{a.Name}\\{file.Name}", Path = file.FullName, Category = a.Name });
                }
            }

            return fileNames;
        }

        public static string IconPath
        {
            get
            {
                return @"Icons\\";
            }
        }

        public static void SelectIconToCopy()
        {
            var open = new OpenFileDialog();
            open.Filter = "PNG Files (*.png)|*.png";

            if ((bool)open.ShowDialog())
            {
                string iconFileName = open.FileName;
                string iconNameOnly = open.SafeFileName;
                string targetDirectory = IconPath + iconNameOnly;

                try
                {
                    File.Copy(iconFileName, targetDirectory, false);
                }
                catch (IOException)
                {

                    MessageBox.Show("Icon Name already exists. Try changing the Icon File name.");
                }

            }
        }

        public static void RefreshIcons(ListView listview)
        {
            List<ChallengerIcon> imgFileNames = GetAllIcons(IconPath);


            listview.ItemsSource = imgFileNames;
        }

        public static void RefreshBlockTypes(ListView listview)
        {
            //using (ChallengerLib.Data.ChallengerDbEntities dbEnt = new ChallengerLib.Data.ChallengerDbEntities())
            //{
            //    listview.ItemsSource = dbEnt.BlockTemplates.ToList();
            //}

            var blockTemplates = new List<BlockTemplate>()
            {
                new BlockTemplate() { ID = 1, Name = "Start Simulation", Icon = System.IO.Path.Combine("Icons", "Miscellaneous", "robot.png") },
                new BlockTemplate() { ID = 2, Name = "End Simulation", Icon = System.IO.Path.Combine("Icons", "Miscellaneous", "finish.png") },
                new BlockTemplate() { ID = 3, Name = "Basic", Icon = System.IO.Path.Combine("Icons", "Miscellaneous", "money.png") }
            };

            listview.ItemsSource = blockTemplates;
        }

        public static void SetSelectedIcon(ChallengerLib.Models.Block tmpl, ListView listView)
        {
            listView.SelectedItem = tmpl;
            List<ChallengerIcon> icons = listView.Items.Cast<ChallengerIcon>().Select(item => item).ToList();
            int index = 0;
            foreach (var a in icons)
            {
                if (tmpl.Icon == a.Name)
                {
                    break;
                }
                index++;
            }

            listView.SelectedIndex = index;
        }

        public static Array ResizeArray(Array arr, int[] newSizes)
        {
            if (newSizes.Length != arr.Rank)
                throw new ArgumentException("arr must have the same number of dimensions " +
                                            "as there are elements in newSizes", "newSizes");

            var temp = Array.CreateInstance(arr.GetType().GetElementType(), newSizes);
            int length = arr.Length <= temp.Length ? arr.Length : temp.Length;
            Array.ConstrainedCopy(arr, 0, temp, 0, length);
            return temp;
        }
    }
}

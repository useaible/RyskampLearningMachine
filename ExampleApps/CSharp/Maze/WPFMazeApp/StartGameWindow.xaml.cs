using MazeGameLib;
using RLM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;

namespace WPFMazeApp
{
    /// <summary>
    /// Interaction logic for StartGameWindow.xaml
    /// </summary>
    public partial class StartGameWindow : Window
    {
        private MainWindow win;
        private WindowLessTraining windowless;
        private MazeCreator mazeDesigner;
        private MazeRepo mazeRepo;
        private IDictionary<int, string> mazes;
        private int MazeId;
        private int errorCount;

        public StartGameWindow()
        {
            InitializeComponent();

            this.Closed += StartGameWindow_Closed;

            // create Mazes database if not exists yet
            mazeRepo = new MazeRepo();
            mazeRepo.CreateDBSchemaIfNotExist();
            mazes = mazeRepo.GetIDNameDictionary();

            ControlButtonVisible();
            errorCount = 0;
        }

        private void StartGameWindow_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void btnCreateMaze_Click(object sender, RoutedEventArgs e)
        {
            mazeDesigner = new MazeCreator();
            mazeDesigner.Closed += (s, ev) => {
                mazes = mazeRepo.GetIDNameDictionary();
                listBoxMazes.ItemsSource = mazes;
                listBoxMazes.SelectedIndex = 0;
                ControlButtonVisible();
                this.Visibility = Visibility.Visible;
            };
            mazeDesigner.Show();
            this.Visibility = System.Windows.Visibility.Hidden;
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            // Human
            if (cmbPlayer.SelectedIndex == 0)
            {
                win = new MainWindow(MazeId, PlayerType.Human);
                win.Closed += (s, ev) => { this.Visibility = Visibility.Visible; };
                win.Show();
                this.Visibility = System.Windows.Visibility.Hidden;
            }
            else if (cmbPlayer.SelectedIndex == 1) // RNN AI
            {
                var selectedMaze = mazeRepo.GetByID(MazeId);
            
                if (selectedMaze != null)
                {
                    var hasLearned = true;//rnn_utils.NeuralNetworkExisting(selectedMaze.Name);
                    if (hasLearned)
                    {
                        win = new MainWindow(MazeId, PlayerType.RNN, false);
                        win.Closed += (s, ev) => { this.Visibility = Visibility.Visible; };
                        win.Show();
                        this.Visibility = System.Windows.Visibility.Hidden;
                    }
                    else
                        Xceed.Wpf.Toolkit.MessageBox.Show("You must first train the AI with the selected maze.", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                }
                else
                    Xceed.Wpf.Toolkit.MessageBox.Show("You must select a maze.", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else // Encog AI
            {
                win = new MainWindow(MazeId, PlayerType.Encog);
                win.Closed += (s, ev) => { this.Visibility = Visibility.Visible; };
                win.Show();
                this.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        private async void btnLearn_Click(object sender, RoutedEventArgs e)
        {
            int totalIterations = txtSessions.Value.Value;
            //int.TryParse(txtSessions.Text, out totalIterations);

            int randomSessions = RfactorOverSessions.Value.Value;
            //int.TryParse(txtRandomSessions.Text, out randomSessions);

            // reset static data
            MainWindow.ResetStaticData();

            //if (totalIterations < 1 || randomSessions < 1)
            //{
            //    MessageBox.Show("Total iterations or number of sessions must be greater or equal to 1.", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            //    return;
            //}
            // window less
            if (chkWindowless.IsChecked.Value)
            {
                windowless = new WindowLessTraining(MazeId,  true, totalIterations, randomSessions, Convert.ToInt32(slRfactorStartValue.Value), Convert.ToInt32(slRfactorEndValue.Value));
                windowless.Closed += (s, ev) => { this.Visibility = Visibility.Visible; };
                windowless.Show();
                this.Visibility = System.Windows.Visibility.Hidden;
            }
            else // with window
            {
                this.Visibility = System.Windows.Visibility.Hidden;
                for (int i = 0; i < totalIterations; i++)
                {
                    if (i != 0)
                    {
                        GC.Collect();
                        System.Threading.Thread.Sleep(100);
                    }

                    using (var win = new MainWindow(MazeId, PlayerType.RNN, true, randomSessions, i + 1, totalIterations, Convert.ToInt32(slRfactorStartValue.Value), Convert.ToInt32(slRfactorEndValue.Value)))
                    {
                        win.Show();
                        await win.WhenClosed();
                        if (!win.ClosedDueToGameOver)
                        {
                            break;
                        }
                    }
                }
                this.Visibility = Visibility.Visible;
            }
        }

        private void comboBoxMazePlay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;

            // ... Set SelectedItem as Window Title.
            var value = comboBox.SelectedValue;
            if (value != null)
                MazeId = Int32.Parse(value.ToString());
            //this.Title = "Selected: " + value;
        }

        private void comboBoxMazePlay_Loaded(object sender, RoutedEventArgs e)
        {
           
            var comboBox = sender as ComboBox;
            //comboBox.Items.Clear();
            comboBox.SelectedValuePath = "Key";
            comboBox.DisplayMemberPath = "Value";
            comboBox.ItemsSource = mazes;
            
            comboBox.SelectedIndex = 0;
        }

        private void comboBox_Copy1_Loaded(object sender, RoutedEventArgs e)
        {
           
            var comboBox = sender as ComboBox;
            //comboBox.Items.Clear();
            comboBox.SelectedValuePath = "Key";
            comboBox.DisplayMemberPath = "Value";
            comboBox.ItemsSource = mazes;

            comboBox.SelectedIndex = 0;
        }

        private void comboBox_Copy1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;

            // ... Set SelectedItem as Window Title.
            var value = comboBox.SelectedValue;
            if(value != null)
            {
                KeyValuePair<int, string> mazeKV = (KeyValuePair<int, string>)comboBox.SelectedItem;
               
                MazeId = Int32.Parse(value.ToString());
                //var completedSessions = rnn_utils.CountSessionsForNetwork(mazeKV.Value);
                //txtBlockCompletedSessions.Text = $"Sessions completed for this maze: {completedSessions}";
            }
                
        }

        private void listBoxMazes_Loaded(object sender, RoutedEventArgs e)
        {
           
            ListBox lstBox = sender as ListBox;
            lstBox.DisplayMemberPath = "Value";
            lstBox.SelectedValuePath = "Key";
            lstBox.ItemsSource = mazes;
        }

        private void btnDeleteMaze_Click(object sender, RoutedEventArgs e)
        {
            var value = listBoxMazes.SelectedValue.ToString();
            int Id = Int32.Parse(value);

            if (mazeRepo.Delete(Id))
            {
                mazes = mazeRepo.GetIDNameDictionary();
                ControlButtonVisible();
                listBoxMazes.ItemsSource = mazes;
                listBoxMazes.SelectedIndex = 0;

            }

        }

        private void ControlButtonVisible()
        {
            if (mazeRepo.Count() > 0)
            {
                btnEditMaze.IsEnabled = true;
                btnDeleteMaze.IsEnabled = true;
                btnLearn.IsEnabled = true;
                btnPlay.IsEnabled = true;
            }
            else
            {
                btnEditMaze.IsEnabled = false;
                btnDeleteMaze.IsEnabled = false;
                btnLearn.IsEnabled = false;
                btnPlay.IsEnabled = false;

            }
        }

        private void btnEditMaze_Click(object sender, RoutedEventArgs e)
        {
            var value = listBoxMazes.SelectedValue.ToString();
            int Id = Int32.Parse(value);

            mazeDesigner = new MazeCreator(Id);
            mazeDesigner.Closed += (s, ev) => {
                mazes = mazeRepo.GetIDNameDictionary();
                listBoxMazes.ItemsSource = mazes;
                listBoxMazes.SelectedIndex = 0;
                ControlButtonVisible();
                this.Visibility = Visibility.Visible;
            };
            mazeDesigner.Show();
            this.Visibility = System.Windows.Visibility.Hidden;
        }

        private void txtBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

            Regex regex = new Regex("[^0-9]+$");
            e.Handled = regex.IsMatch(e.Text);

        }
        private void OnErrorEvent(object sender, RoutedEventArgs e)
        {
            var validationEventArgs = e as ValidationErrorEventArgs;
            if (validationEventArgs == null)
                throw new Exception("Unexpected event args");
            switch (validationEventArgs.Action)
            {
                case ValidationErrorEventAction.Added:
                    {
                        errorCount++; break;
                    }
                case ValidationErrorEventAction.Removed:
                    {
                        errorCount--; break;
                    }
                default:
                    {
                        throw new Exception("Unknown action");
                    }
            }
            btnLearn.IsEnabled = errorCount == 0;
        }

        private void btnLearnEncog_Click(object sender, RoutedEventArgs e)
        {
            int totalIterations = 1;
            int.TryParse(txtSessions.Text, out totalIterations);

            int maxTemp = 10;
            int.TryParse(txtEncMaxTemp.Text, out maxTemp);

            int minTemp = 10;
            int.TryParse(txtEncMinTemp.Text, out minTemp);

            int cycle = 100;
            int.TryParse(txtEncCyclesPerIteration.Text, out cycle);

            if (maxTemp < minTemp)
            {
                Xceed.Wpf.Toolkit.MessageBox.Show("Max Temp must be greater than or equal to Min Temp", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (totalIterations < 1 || maxTemp < 1 || minTemp < 1 || cycle < 1)
            {
                Xceed.Wpf.Toolkit.MessageBox.Show("The inputs must be greater or equal to 1.", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            
            windowless = new WindowLessTraining(MazeId, totalIterations, maxTemp, minTemp, cycle);
            windowless.Closed += (s, ev) => { this.Visibility = Visibility.Visible; };
            windowless.Show();
            this.Visibility = System.Windows.Visibility.Hidden;
        }

        private void On_Error(object sender, ValidationErrorEventArgs e)
        {
            var validationEventArgs = e;
            if (validationEventArgs == null)
                throw new Exception("Unexpected event args");
            switch (validationEventArgs.Action)
            {
                case ValidationErrorEventAction.Added:
                    {
                        errorCount++; break;
                    }
                case ValidationErrorEventAction.Removed:
                    {
                        errorCount--; break;
                    }
                default:
                    {
                        throw new Exception("Unknown action");
                    }
            }
            btnLearn.IsEnabled = errorCount == 0;
        }

        private void On_Error(object sender, Xceed.Wpf.Toolkit.Core.Input.InputValidationErrorEventArgs e)
        {
            
        }
    }
}

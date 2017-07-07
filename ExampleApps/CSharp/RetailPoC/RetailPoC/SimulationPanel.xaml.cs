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
using MahApps.Metro.Controls;
using System.Text.RegularExpressions;

namespace RetailPoC
{
    /// <summary>
    /// Interaction logic for SimulationPanel.xaml
    /// </summary>
    public partial class SimulationPanel : MetroWindow
    {
        public SimulationType SimType { get; private set; }
        public int? Sessions { get; private set; }
        public double? Hours { get; private set; }
        public double? Score { get; private set; }
        public bool EnableSimDisplay
        {
            get
            {
                return chkSimDisplay.IsChecked.Value;
            }
        }
        public int HiddenLayers { get; private set; }
        public int HiddenLayerNeurons { get; private set; }

        private SimulationSettings simSettings;
        private double maxScore = -1;
        private double MAX_SCORE = 0;
        private Regex regexNumbersOnly = new Regex("[^0-9]+");
        //private const int DEFAULT_SCORE_PERCENTAGE = 85;

        public SimulationPanel()
        {
            InitializeComponent();
        }

        public void SetSimSettings(SimulationSettings simSettings)
        {
            if (simSettings != null)
            {
                this.simSettings = simSettings;
                SimType = simSettings.SimType;
                Sessions = simSettings.Sessions;
                Hours = simSettings.Hours;
                Score = simSettings.Score;
                chkSimDisplay.IsChecked = simSettings.EnableSimDisplay;

                if (!simSettings.EncogSelected)
                {
                    grpEncogSettings.Visibility = Visibility.Collapsed;
                    this.Height -= grpEncogSettings.Height;
                }
                else
                {
                    HiddenLayers = simSettings.HiddenLayers;
                    txtHiddenLayers.Text = HiddenLayers.ToString();

                    HiddenLayerNeurons = (simSettings.HiddenLayerNeurons <= 0) ? simSettings.NumSlots * simSettings.NumShelves : simSettings.HiddenLayerNeurons;
                    txtHiddenLayerNeurons.Text = HiddenLayerNeurons.ToString();
                }

                calculateMaxScore();
                simScoreSlider.Value = simSettings.DefaultScorePercentage;
                simScoreSliderLbl.Content = simScoreSlider.Value + "%";

                switch (SimType)
                {
                    case SimulationType.Sessions:
                        rdbSessions.IsChecked = true;
                        txtSimInput.Text = Sessions.ToString();
                        showScoreSlider(false);
                        break;
                    case SimulationType.Time:
                        rdbTime.IsChecked = true;
                        txtSimInput.Text = Hours.ToString();
                        showScoreSlider(false);
                        break;
                    default:
                        rdbScore.IsChecked = true;
                        txtSimInput.Text = Score.ToString();
                        showScoreSlider(true);
                        break;
                }
            }
        }

        private void showScoreSlider(bool val)
        {
            simScoreSlider.Visibility = val == false ? Visibility.Hidden : Visibility.Visible;
            simScoreSliderLbl.Visibility = val == false ? Visibility.Hidden : Visibility.Visible;
        }

        private void radioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (txtSimInput == null) return;

            // depending on what was checked, we set the simulation display and change the label text to suit what was selected
            string value = txtSimInput.Text;
            if (rdbSessions.IsChecked.Value)
            {
                SimType = SimulationType.Sessions;
                lblSimInput.Content = "Number of sessions:";
                SetBtnMaxEnable(false);
                txtSimInput.Text = Sessions.ToString();
                showScoreSlider(false);
            }

            if (rdbTime.IsChecked.Value)
            {
                SimType = SimulationType.Time;
                lblSimInput.Content = "Number of hours:";
                SetBtnMaxEnable(false);
                txtSimInput.Text = Hours.ToString();
                showScoreSlider(false);
            }

            if (rdbScore.IsChecked.Value)
            {
                SimType = SimulationType.Score;
                lblSimInput.Content = "Must achieve at least this score 10 consecutive times:";
                //SetBtnMaxEnable(true);
                showScoreSlider(true);
                btnMax_Click(null, null);
            }
        }

        private void SetBtnMaxEnable(bool value)
        {
            btnMax.IsEnabled = value;
            btnMax.Visibility = (value) ? Visibility.Visible : Visibility.Hidden;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            var simInput = txtSimInput.Text;

            //validates the inputed number to see if it was a valid integer or double before proceeding
            if (SimType == SimulationType.Sessions)
            {
                int sessions;
                if (string.IsNullOrWhiteSpace(simInput) || !int.TryParse(simInput, out sessions))
                {
                    MessageBox.Show("Please input a valid integer value.", "Invalid input", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    txtSimInput.Focus();
                    return;
                }
                else
                {
                    Sessions = sessions;
                }
            }
            else
            {
                double value;
                if (string.IsNullOrWhiteSpace(simInput) || !double.TryParse(simInput, out value))
                {
                    MessageBox.Show("Please input a valid double value.", "Invalid input", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    txtSimInput.Focus();
                    return;
                }
                else
                {
                    if (SimType == SimulationType.Time)
                    {
                        Hours = value;
                    }
                    else
                    {
                        Score = value;
                    }
                }
            }

            HiddenLayers = Convert.ToInt32(txtHiddenLayers.Text);
            HiddenLayerNeurons = Convert.ToInt32(txtHiddenLayerNeurons.Text);

            DialogResult = true;
            Close();
        }

        private void btnMax_Click(object sender, RoutedEventArgs e)
        {
            //if (maxScore < 0)
            {
                double maxPercentage = simScoreSlider.Value / 100;

                maxScore = Math.Round(MAX_SCORE * maxPercentage, 2); // todo 90% must be from slider value
                simSettings.Score = maxScore;
                Score = simSettings.Score;
            }

            txtSimInput.Text = maxScore.ToString();
        }

        private void calculateMaxScore()
        {
            using (PlanogramContext ctx = new PlanogramContext())
            {
                MockData mock = new MockData(ctx);
                MAX_SCORE = mock.GetItemMaxScoreForTop(simSettings);
            }
        }

        private void simScoreSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            simScoreSliderLbl.Content = simScoreSlider.Value + "%";
            btnMax_Click(null, null);
        }

        private void NumberValidationEncog(object sender, TextCompositionEventArgs e)
        {
            e.Handled = regexNumbersOnly.IsMatch(e.Text);
        }

        private void NumberValidationScore(object sender, TextCompositionEventArgs e)
        {
            if (SimType == SimulationType.Sessions)
            {
                e.Handled = regexNumbersOnly.IsMatch(e.Text);
            }
        }
    }
}

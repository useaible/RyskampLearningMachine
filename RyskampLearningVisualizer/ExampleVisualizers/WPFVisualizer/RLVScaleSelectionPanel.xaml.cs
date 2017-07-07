using RLV.Core.Interfaces;
using RLV.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;

namespace WPFVisualizer
{
    /// <summary>
    /// Interaction logic for RLVScaleSelectionPanel.xaml
    /// </summary>
    public partial class RLVScaleSelectionPanel : UserControl, IRLVScaleSelectionPanel
    {
        public const double DEFAULT_SCALE = 50;

        public event ScaleChangedDelegate ScaleChangedEvent;
        public object ViewModel { get; set; } = new RLVScaleSelectionVM();
        public RLVScaleSelectionPanel()
        {
            InitializeComponent();
        }

        public void SetCurrentCaseId(int caseId)
        {
            ((RLVScaleSelectionVM)ViewModel).CurrentCaseId = caseId;
        }

        public void SetDefaultScale(double scale)
        {
            ((RLVScaleSelectionVM)ViewModel).DefaultScale = scale;
            //((RLVScaleSelectionVM)ViewModel).SliderLabelText = $"Percent of Improvement: {((RLVScaleSelectionVM)ViewModel).DefaultScale}%";
            ((RLVScaleSelectionVM)ViewModel).SliderLabelText = $"{((RLVScaleSelectionVM)ViewModel).DefaultScale}%";

            double percentage = ((RLVScaleSelectionVM)ViewModel).DefaultScale;
            ScaleChangedEvent?.Invoke(percentage);
        }

        private void scaleSlider_PreviewMouseUp_1(object sender, MouseButtonEventArgs e)
        {
            double percentage = ((RLVScaleSelectionVM)ViewModel).DefaultScale;
            ScaleChangedEvent?.Invoke(percentage);
        }

        private void scaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider s = (Slider)sender;
            ((RLVScaleSelectionVM)ViewModel).DefaultScale = s.Value;
            //((RLVScaleSelectionVM)ViewModel).SliderLabelText = $"Percent of Improvement: {((RLVScaleSelectionVM)ViewModel).DefaultScale}%";
            ((RLVScaleSelectionVM)ViewModel).SliderLabelText = $"{((RLVScaleSelectionVM)ViewModel).DefaultScale}%";
        }

        public void SetViewModel(IRLVScaleSelectionVM vm)
        {
            DataContext = ViewModel = vm;
            ((RLVScaleSelectionVM)ViewModel).DefaultScale = DEFAULT_SCALE;
            //((RLVScaleSelectionVM)ViewModel).SliderLabelText = $"Percent of Improvement: {((RLVScaleSelectionVM)ViewModel).DefaultScale}%";
            ((RLVScaleSelectionVM)ViewModel).SliderLabelText = $"{((RLVScaleSelectionVM)ViewModel).DefaultScale}%";
        }

        public double DefaultScale
        {
            get { return (double)GetValue(DefaultScaleProperty); }
            set { SetValue(DefaultScaleProperty, value); }
        }

        public static readonly DependencyProperty DefaultScaleProperty =
              DependencyProperty.Register("DefaultScale", typeof(double),
                typeof(RLVScaleSelectionPanel), new PropertyMetadata(null));

        public string SliderLabelText
        {
            get { return (string)GetValue(SliderLabelTextProperty); }
            set { SetValue(SliderLabelTextProperty, value); }
        }

        public static readonly DependencyProperty SliderLabelTextProperty =
              DependencyProperty.Register("SliderLabelText", typeof(string),
                typeof(RLVScaleSelectionPanel), new PropertyMetadata(null));

        public void SetScaleValueManual(double scale)
        {
            if (scale != scaleSlider.Value)
                scaleSlider.Value = scale;
        }

        private void label_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            SetScaleValueManual(scaleSlider.Value - 1);
            scaleSlider_PreviewMouseUp_1(null, null);
        }

        private void label_Copy_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            SetScaleValueManual(scaleSlider.Value + 1);
            scaleSlider_PreviewMouseUp_1(null, null);
        }

        public void UpdateBindings(RLVItemDisplayVM userVal)
        {
            throw new NotImplementedException();
        }

        public void SaveConfiguration()
        {
            throw new NotImplementedException();
        }
    }
}

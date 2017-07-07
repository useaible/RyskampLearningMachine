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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RetailPoCSimple
{
    /// <summary>
    /// Interaction logic for TestControl.xaml
    /// </summary>
    public partial class TestControl : UserControl
    {
        public TestControl()
        {
            InitializeComponent();
        }

        public void SetViewModel()
        {
            this.DataContext = ViewModel;
            setBindings();
        }

        private void setBindings()
        {
            IValueConverter converter = new SampleValueConverter();
            IValueConverter converter2 = new SampleValueConverter2();

            Binding binding = new Binding();
            binding.Source = txtSample.Text;
            binding.Mode = BindingMode.OneWay;
            binding.Converter = converter2;

            txtSample.SetBinding(TextBlock.TextProperty, binding);

            Binding binding2 = new Binding();
            binding2.Source = txtBoxSample.Text;
            binding2.Mode = BindingMode.OneWay;
            binding2.Converter = converter2;

            txtBoxSample.SetBinding(TextBox.TextProperty, binding2);
        }

        public TestViewModel ViewModel { get; set; } = new TestViewModel();

        //public string TestName
        //{
        //    get { return (string)GetValue(TestNameProperty); }
        //    set { SetValue(TestNameProperty, value); }
        //}

        //public static readonly DependencyProperty TestNameProperty =
        //      DependencyProperty.Register("TestName", typeof(string),
        //        typeof(TestControl), new PropertyMetadata(null));
    }
}

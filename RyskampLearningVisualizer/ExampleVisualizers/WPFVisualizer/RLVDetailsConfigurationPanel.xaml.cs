using RLV.Core.Converters;
using RLV.Core.Enums;
using RLV.Core.Interfaces;
using RLV.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// Interaction logic for RLVDetailsConfigurationPanel.xaml
    /// </summary>
    public partial class RLVDetailsConfigurationPanel : UserControl
    {
        public RLVDetailsConfigurationPanel()
        {
            InitializeComponent();
        }

        public void PopulateControls(IRLVSelectedDetailsPanel detailsControl)
        {

            // *** The following will create a mapping of elements from the RLVSelectedDetailsPanel to be defined by the user. ***

            // Clear for repopulation of controls
            labelsGrid.Children.Clear();
            valuesGrid.Children.Clear();

            // == Mapping for the Labels/Text displayed in RLVSelectedDetailsPanel. ==

            double gridHorizontalGap = 10; // Gap between each child elements.
            Grid grid1 = new Grid();
            grid1.Margin = new Thickness(10, gridHorizontalGap, 10, 0);

            TextBlock panelHeaderLbl = new TextBlock();
            panelHeaderLbl.TextAlignment = TextAlignment.Right;
            panelHeaderLbl.HorizontalAlignment = HorizontalAlignment.Left;
            panelHeaderLbl.Margin = new Thickness(0, 4, 0, 0);
            panelHeaderLbl.VerticalAlignment = VerticalAlignment.Top;
            panelHeaderLbl.RenderTransformOrigin = new Point(0.433, -0.385);
            panelHeaderLbl.Width = 133;

            TextBox panelHeaderTxtVal = new TextBox();
            panelHeaderTxtVal.HorizontalAlignment = HorizontalAlignment.Left;
            panelHeaderTxtVal.Height = 23;
            panelHeaderTxtVal.Margin = new Thickness(138, 0, 0, 0);
            panelHeaderTxtVal.VerticalAlignment = VerticalAlignment.Top;
            panelHeaderTxtVal.Width = 155;

            Binding panelHeaderLblBinding = new Binding() { Source = "Panel Header", Mode = BindingMode.OneTime };
            Binding panelHeaderLblValueBinding = new Binding() { Source = ((IRLVSelectedDetailVM)detailsControl.ViewModel), Path = new PropertyPath("Header"), Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };

            BindingOperations.SetBinding(panelHeaderLbl, TextBlock.TextProperty, panelHeaderLblBinding);
            BindingOperations.SetBinding(panelHeaderTxtVal, TextBox.TextProperty, panelHeaderLblValueBinding);

            grid1.Children.Add(panelHeaderLbl);
            grid1.Children.Add(panelHeaderTxtVal);

            labelsGrid.Children.Add(grid1);

            gridHorizontalGap += 31; // Increase gap between grid after each iteration.
            foreach (var element in ((IRLVSelectedDetailVM)detailsControl.ViewModel).Labels)
            {
                // The grid that will contain the following elements, need this to align them side by side.
                Grid grid = new Grid();
                grid.Margin = new Thickness(10, gridHorizontalGap, 10, 0);

                TextBlock lbl = new TextBlock(); // The default text.
                TextBox txtVal = new TextBox(); // The custom text that will be specified by the user.
                CheckBox chk = new CheckBox(); // To set the visibility of the element from the RLVSelectedDetailsPanel

                // Configure the positioning of the the above elements.
                // For the default text.
                lbl.TextAlignment = TextAlignment.Right;
                lbl.HorizontalAlignment = HorizontalAlignment.Left;
                lbl.Margin = new Thickness(0, 4, 0, 0);
                lbl.VerticalAlignment = VerticalAlignment.Top;
                lbl.RenderTransformOrigin = new Point(0.433, -0.385);
                lbl.Width = 133;

                // For the user defined text.
                txtVal.HorizontalAlignment = HorizontalAlignment.Left;
                txtVal.Height = 23;
                txtVal.Margin = new Thickness(138, 0, 0, 0);
                txtVal.VerticalAlignment = VerticalAlignment.Top;
                txtVal.Width = 155;

                // For the checkbox that sets the visibility of the element in RLVSelectedDetailsPanel.
                chk.HorizontalAlignment = HorizontalAlignment.Left;
                chk.Margin = new Thickness(298, 4, 0, 0);
                chk.VerticalAlignment = VerticalAlignment.Top;
                chk.ToolTip = "Hide this control";

                // Element bindings
                Binding lblBinding = new Binding() { Source = element, Path = new PropertyPath("Value"), Mode = BindingMode.OneTime };
                Binding valueBinding = new Binding() { Source = element, Path = new PropertyPath("Value"), Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };

                Binding checkBinding = new Binding() { Source = element, Path = new PropertyPath("Visibility"), Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
                checkBinding.Converter = new CheckConverter();

                BindingOperations.SetBinding(lbl, TextBlock.TextProperty, lblBinding);
                BindingOperations.SetBinding(txtVal, TextBox.TextProperty, valueBinding);
                BindingOperations.SetBinding(chk, CheckBox.IsCheckedProperty, checkBinding);

                // Add the elements to its container grid.
                grid.Children.Add(txtVal);
                grid.Children.Add(lbl);
                grid.Children.Add(chk);

                // Add them to the parent grid
                labelsGrid.Children.Add(grid);

                gridHorizontalGap += 31; // Increase gap between grid after each iteration.
            }

            // == Mapping for the Values displayed in RLVSelectedDetailsPanel. ==

            gridHorizontalGap = 10; // Reset the gap between element grid container for the new set.
            foreach (var ctrl in ((IRLVSelectedDetailVM)detailsControl.ViewModel).Values)
            {
                // The grid that will contain the following elements.
                Grid grid = new Grid();
                grid.Margin = new Thickness(10, gridHorizontalGap, 10, 0);

                TextBlock lbl = new TextBlock(); // The description of the values.
                ComboBox formatterDropDown = new ComboBox(); // The list of formatters to be defined by the user 
                                                             // to set the format of the selected element value 
                                                             // in RLVSelectedDetailsPanel 
                CheckBox chk = new CheckBox(); // To set the visibility of the element.

                // Positioning of elements.

                // For the description text.
                lbl.TextAlignment = TextAlignment.Right;
                lbl.HorizontalAlignment = HorizontalAlignment.Left;
                lbl.Margin = new Thickness(0, 4, 0, 0);
                lbl.VerticalAlignment = VerticalAlignment.Top;
                lbl.RenderTransformOrigin = new Point(0.433, -0.385);
                lbl.Width = 133;

                // For the list of available formats.
                formatterDropDown.HorizontalAlignment = HorizontalAlignment.Left;
                formatterDropDown.Height = 23;
                formatterDropDown.Margin = new Thickness(138, 0, 0, 0);
                formatterDropDown.VerticalAlignment = VerticalAlignment.Top;
                formatterDropDown.Width = 155;
                formatterDropDown.ToolTip = "Configure formatting for this value.";
                formatterDropDown.Text = "Select Format";
                formatterDropDown.ItemsSource = Enum.GetValues(typeof(RLVFormatters));
                formatterDropDown.SelectedIndex = 0;

                if (ctrl.SelectedValueFromConverter != null)
                {
                    formatterDropDown.SelectedIndex = ctrl.SelectedValueFromConverter.Value;
                }

                // An event to get the choosen format.
                IValueConverter converter = null;
                formatterDropDown.SelectionChanged += (a, b) =>
                {
                    var selected = (RLVFormatters)b.AddedItems[0];
                    switch (selected)
                    {
                        case RLVFormatters.Text:
                            converter = null;
                            break;
                        case RLVFormatters.Numeric_General:
                            converter = new RLVNumericConverter(selected);
                            ctrl.ConverterType = 0;
                            break;
                        case RLVFormatters.Numeric_Currency:
                            converter = new RLVNumericConverter(selected);
                            ctrl.ConverterType = 0;
                            break;
                        case RLVFormatters.Numeric_FixedPoint:
                            converter = new RLVNumericConverter(selected);
                            ctrl.ConverterType = 0;
                            break;
                        case RLVFormatters.Numeric_Number:
                            converter = new RLVNumericConverter(selected);
                            ctrl.ConverterType = 0;
                            break;
                        case RLVFormatters.Numeric_Percent:
                            converter = new RLVNumericConverter(selected);
                            ctrl.ConverterType = 0;
                            break;
                        case RLVFormatters.Time_Days:
                            converter = new RLVTimeConverter(selected);
                            ctrl.ConverterType = 1;
                            break;
                        case RLVFormatters.Time_Hours:
                            converter = new RLVTimeConverter(selected);
                            ctrl.ConverterType = 1;
                            break;
                        case RLVFormatters.Time_Minutes:
                            converter = new RLVTimeConverter(selected);
                            ctrl.ConverterType = 1;
                            break;
                        case RLVFormatters.Time_Seconds:
                            converter = new RLVTimeConverter(selected);
                            ctrl.ConverterType = 1;
                            break;
                        case RLVFormatters.Time_Milliseconds:
                            converter = new RLVTimeConverter(selected);
                            ctrl.ConverterType = 1;
                            break;
                    }

                    ctrl.Converter = converter;
                    ctrl.SelectedValueFromConverter = (int)selected;
                    detailsControl.UpdateBindings(ctrl); // Call this to apply the formatting.
                };


                // For the checkbox that sets the visibility of the element in the RLVSelectedDetailsPanel.
                chk.HorizontalAlignment = HorizontalAlignment.Left;
                chk.Margin = new Thickness(298, 4, 0, 0);
                chk.VerticalAlignment = VerticalAlignment.Top;
                chk.ToolTip = "Hide this control";

                // The bindings
                Binding lblBinding = new Binding() { Source = ctrl, Path = new PropertyPath("Description"), Mode = BindingMode.OneTime };
                Binding checkBinding = new Binding() { Source = ctrl, Path = new PropertyPath("Visibility"), Mode = BindingMode.TwoWay };
                checkBinding.Converter = new CheckConverter();

                BindingOperations.SetBinding(lbl, TextBlock.TextProperty, lblBinding);
                BindingOperations.SetBinding(chk, CheckBox.IsCheckedProperty, checkBinding);

                // Add to its container grid.
                grid.Children.Add(formatterDropDown);
                grid.Children.Add(lbl);
                grid.Children.Add(chk);

                // Add to the parent container grid.
                valuesGrid.Children.Add(grid);

                gridHorizontalGap += 31; // Increase gap between grid after each iteration.
            }
        }

        // The converted used to set the visibility of the element.
        // This is needed to convert the value provided from the viewmodel which is Visible and Hidden to true and false that
        // can be applied to the checkbox.
        public class CheckConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value.ToString() == "Visible")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                bool val = System.Convert.ToBoolean(value);
                if (val == true)
                {
                    return System.Windows.Visibility.Visible;
                }
                else
                {
                    return System.Windows.Visibility.Hidden;
                }
            }
        }
    }
}

using RLM.Models;
using RLV.Core.Interfaces;
using RLV.Core.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using System.IO;
using RLV.Core.Converters;

namespace WPFVisualizer
{
    /// <summary>
    /// Interaction logic for RLVSelectedDetailsPanel.xaml
    /// </summary>
    public partial class RLVSelectedDetailsPanel : UserControl, IRLVSelectedDetailsPanel
    {
        public event LearningComparisonDelegate LearningComparisonEvent;
        public event NextPrevCaseChangedDelegate NextPrevCaseChangedEvent;
        public event SessionBreakdownClickDelegate SessionBreakdownClickEvent;

        private List<RlmLearnedCaseDetails> learnedCaseDetails = new List<RlmLearnedCaseDetails>();
        private RlmLearnedCaseDetails current = new RlmLearnedCaseDetails();
        private RlmLearnedCaseDetails previous = new RlmLearnedCaseDetails();

        public RLVSelectedDetailsPanel()
        {
            InitializeComponent();
            getViewModelFromConfig();
            DataContext = ViewModel;
            ScalePanel = scaleSelectionControl;
            setLabelBindings();
            setValueBindings();
        }

        public object ViewModel { get; private set; } = new RLVSelectedDetailVM();
        public IRLVScaleSelectionPanel ScalePanel { get; private set; }
        public IRLVOutputVisualizer OutputVisualizer { get; set; }

        public void IRLVCore_SelectedCaseDetailResultsHandler(IEnumerable<RlmLearnedCaseDetails> data, bool showComparison = false)
        {
            learnedCaseDetails = data.ToList();

            var inputs = learnedCaseDetails.Where(a=>a.IsCurrent).SelectMany(a => a.Inputs);
            var outputs = learnedCaseDetails.Where(a => a.IsCurrent).SelectMany(a => a.Outputs);

            current = data.First(a => a.IsCurrent);
            previous = data.FirstOrDefault(a => !a.IsCurrent);

            if (previous != null)
            {
                ((IRLVSelectedDetailVM)ViewModel).PreviousSessionId = previous.SessionId;
                ((IRLVSelectedDetailVM)ViewModel).PreviousSession = previous.SessionNum;
                ((IRLVSelectedDetailVM)ViewModel).PreviousTime = previous.SessionTime;
                ((IRLVSelectedDetailVM)ViewModel).PreviousCase = previous.CycleNum;
                ((IRLVSelectedDetailVM)ViewModel).PreviousScore = previous.SessionScore;
            }
            else
            {
                ((IRLVSelectedDetailVM)ViewModel).PreviousSession = null;
                ((IRLVSelectedDetailVM)ViewModel).PreviousTime = null;
                ((IRLVSelectedDetailVM)ViewModel).PreviousCase = null;
                ((IRLVSelectedDetailVM)ViewModel).PreviousScore = null;
            }

            ((IRLVSelectedDetailVM)ViewModel).CurrentSessionId = current.SessionId;
            ((IRLVSelectedDetailVM)ViewModel).CurrentSession = current.SessionNum;
            ((IRLVSelectedDetailVM)ViewModel).CurrentTime = current.SessionTime;
            ((IRLVSelectedDetailVM)ViewModel).CurrentCase = current.CycleNum;
            ((IRLVSelectedDetailVM)ViewModel).CurrentScore = current.SessionScore;

            //Make input details an observable collection for binding to its grid
            ObservableCollection<RLVIODetailsVM> inputCollection = new ObservableCollection<RLVIODetailsVM>();
            foreach(var a in inputs)
            {
                RLVIODetailsVM inputVm = new RLVIODetailsVM
                {
                    Name = a.Name,
                    Value = a.Value
                };

                inputCollection.Add(inputVm);
            }

            //Make output details an observable collection for binding to its grid
            ObservableCollection<RLVIODetailsVM> outputCollection = new ObservableCollection<RLVIODetailsVM>();
            foreach (var a in outputs)
            {
                RLVIODetailsVM outputVm = new RLVIODetailsVM
                {
                    Name = a.Name,
                    Value = a.Value
                };

                outputCollection.Add(outputVm);
            }

            ((IRLVSelectedDetailVM)ViewModel).InputDetails = inputCollection;
            ((IRLVSelectedDetailVM)ViewModel).OutputDetails = outputCollection;

            if (showComparison)
            {
                if (previous != null)
                {
                    LearningComparisonEvent?.Invoke(current.SessionId, previous.SessionId);
                }
            }
        }

        public void IRLVCore_SessionBreakdownClickResultsHandler(RlmLearnedSessionDetails details)
        {
            if(OutputVisualizer != null)
            {
                var breakdown = OutputVisualizer.SessionScoreBreakdown(details);
                showSessionBreakdown(breakdown);
            }
        }

        private void showSessionBreakdown(IEnumerable<IRLVItemDisplay> breakdown)
        {
            if (breakdown != null)
            {
                RLVSessionScoreBreakdown bd = new RLVSessionScoreBreakdown(breakdown);
                bd.ShowDialog();
            }
        }

        public void LoadScalePanel(ref IRLVScaleSelectionPanel panel)
        {
            panel = scaleSelectionControl;
        }

        public void LoadConfigurationPanel(object parentWindow)
        {
            //Window parent = parentWindow as Window;
            //RLVDetailsConfigurationPanel vsConfig = new RLVDetailsConfigurationPanel(this);
            //vsConfig.Show();
            //vsConfig.Top = parent.Top;
            //vsConfig.Left = parent.Left + parent.Width;
        }

        public void UpdateBindings(RLVItemDisplayVM userVal)
        {

            if (userVal.SelectedValueFromConverter != null && userVal.ConverterType != null)
            {
                if (userVal.ConverterType == 0) //Number
                {
                    userVal.Converter = new RLVNumericConverter((RLV.Core.Enums.RLVFormatters)userVal.SelectedValueFromConverter);
                }
                else //1=Time
                {
                    userVal.Converter = new RLVTimeConverter((RLV.Core.Enums.RLVFormatters)userVal.SelectedValueFromConverter);
                }
            }

            var controls = this.mainGrid.Children;
            foreach (var control in controls)
            {
                FrameworkElement con = control as FrameworkElement;

                // Exclude other controls
                if (con.Name.StartsWith("scaleSelection") || con.Name.StartsWith("btn") || con.Name.StartsWith("lbl") || string.IsNullOrEmpty(con.Name))
                    continue;

                if (con.Name == userVal.Name)
                {
                    var type = control.GetType();
                    var typeName = type.FullName;
                    var typeAssembly = type.Assembly;

                    Type controlType = Type.GetType($"{typeName}, {typeAssembly}");
                    if (controlType == typeof(DataGrid))
                    {
                        DataGrid gridCtrl = control as DataGrid;

                        var itemSourceBinding = new Binding { Source = ViewModel, Path = new PropertyPath(gridCtrl.Tag.ToString()) };
                        var visibilityBinding = new Binding { Source = userVal, Path = new PropertyPath("Visibility"), Mode = BindingMode.TwoWay };

                        BindingOperations.SetBinding(gridCtrl, DataGrid.ItemsSourceProperty, itemSourceBinding);
                        BindingOperations.SetBinding(gridCtrl, DataGrid.VisibilityProperty, visibilityBinding);
                    }
                    else if (controlType == typeof(TextBox))
                    {
                        TextBox textCtrl = control as TextBox;

                        var valueBinding = new Binding { Source = ViewModel, Path = new PropertyPath(textCtrl.Tag.ToString()) };
                        valueBinding.Converter = userVal.Converter;

                        var visibilityBinding = new Binding { Source = userVal, Path = new PropertyPath("Visibility"), Mode = BindingMode.TwoWay };

                        BindingOperations.SetBinding(textCtrl, TextBox.TextProperty, valueBinding);
                        BindingOperations.SetBinding(textCtrl, TextBox.VisibilityProperty, visibilityBinding);
                    }
                    else if (controlType == typeof(TextBlock))
                    {
                        TextBlock textCtrl = control as TextBlock;

                        var visibilityBinding = new Binding { Source = userVal, Path = new PropertyPath("Visibility"), Mode = BindingMode.TwoWay };
                        BindingOperations.SetBinding(textCtrl, TextBlock.VisibilityProperty, visibilityBinding);

                        bool foundHyperlink = false;
                        if (textCtrl.Inlines.Count > 0)
                        {
                            Hyperlink link = textCtrl.Inlines.First() as Hyperlink;
                            InlineUIContainer linkChild = link.Inlines.First() as InlineUIContainer;
                            TextBlock scoreLinkText = linkChild.Child as TextBlock;

                            Binding linkTextBinding = new Binding { Source = ViewModel, Path = new PropertyPath(scoreLinkText.Tag.ToString()) };
                            linkTextBinding.Converter = userVal.Converter;

                            // Bind the text to be displayed
                            BindingOperations.SetBinding(scoreLinkText, TextBlock.TextProperty, linkTextBinding);

                            Binding linkUidBinding = new Binding { Source = ViewModel, Path = new PropertyPath(scoreLinkText.ToolTip.ToString()) };

                            // Bind the session id to the UID property: This value will be used as a parameter in getting the score breakdown.
                            BindingOperations.SetBinding(scoreLinkText, TextBlock.UidProperty, linkUidBinding);

                            foundHyperlink = true;
                        }

                        if (foundHyperlink)
                            continue;

                        var valueBinding = new Binding { Source = ViewModel, Path = new PropertyPath(textCtrl.Tag.ToString()) };
                        valueBinding.Converter = userVal.Converter;
                        BindingOperations.SetBinding(textCtrl, TextBlock.TextProperty, valueBinding);

                    }
                    else if (controlType == typeof(Label))
                    {
                        Label labelCtrl = control as Label;

                        var valueBinding = new Binding { Source = ViewModel, Path = new PropertyPath(labelCtrl.Tag.ToString()) };
                        valueBinding.Converter = userVal.Converter;

                        var visibilityBinding = new Binding { Source = userVal, Path = new PropertyPath("Visibility"), Mode = BindingMode.TwoWay };

                        BindingOperations.SetBinding(labelCtrl, Label.ContentProperty, valueBinding);
                        BindingOperations.SetBinding(labelCtrl, Label.VisibilityProperty, visibilityBinding);
                    }
                }
            }
        }

        //Private methods
        private void btnShowLearningComparison_Click(object sender, RoutedEventArgs e)
        {
            if (previous != null)
            {
                LearningComparisonEvent?.Invoke(current.SessionId, previous.SessionId);
            }
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            NextPrevCaseChangedEvent?.Invoke(current.CaseId, true);
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            NextPrevCaseChangedEvent?.Invoke(current.CaseId, false);
        }

        private void score_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            long sessionId = Convert.ToInt64(((TextBlock)sender).Uid);
            SessionBreakdownClickEvent?.Invoke(sessionId);
        }

        private void setLabelBindings()
        {
            foreach (var userLabel in ((IRLVSelectedDetailVM)ViewModel).Labels)
            {
                var controls = this.mainGrid.Children;
                foreach (var control in controls)
                {
                    FrameworkElement con = control as FrameworkElement;
                    if (!con.Name.StartsWith("lbl"))
                        continue;

                    if(con.Name == userLabel.Name)
                    {
                        dynamic lblCtrl = control as Label;
                        if (lblCtrl == null)
                        {
                            lblCtrl = control as Button;
                        }

                        Binding labelBinding = new Binding();
                        labelBinding.Source = userLabel;
                        labelBinding.Path = new PropertyPath("Value");
                        labelBinding.Mode = BindingMode.TwoWay;
                        labelBinding.Converter = userLabel.Converter;

                        var visibilityBinding = new Binding { Source = userLabel, Path = new PropertyPath("Visibility"), Mode = BindingMode.TwoWay };
                        if (lblCtrl.Tag != null)
                        {
                            BindingOperations.SetBinding(lblCtrl, Button.ContentProperty, labelBinding);
                        }
                        else
                        {
                            BindingOperations.SetBinding(lblCtrl, Label.ContentProperty, labelBinding);
                        }
                        BindingOperations.SetBinding(lblCtrl, Label.VisibilityProperty, visibilityBinding);
                    }
                }
            }
        }

        private void setValueBindings()
        {
            foreach(var userVal in ((IRLVSelectedDetailVM)ViewModel).Values)
            {
                this.UpdateBindings(userVal);
            }
        }
        private void getViewModelFromConfig()
        {
            try
            {
                using (StreamReader rdr = File.OpenText("RLVSelectedDetailsPanel.json"))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.TypeNameHandling = TypeNameHandling.Auto;
                    serializer.Formatting = Formatting.Indented;
                    dynamic viewModel = (dynamic)serializer.Deserialize(rdr, typeof(object));

                    var labels = viewModel.Labels.ToObject<ObservableCollection<RLVItemDisplayVM>>();
                    var valuesToStr = JsonConvert.SerializeObject(viewModel.Values);
                    var values = JsonConvert.DeserializeObject<ObservableCollection<RLVItemDisplayVM>>(valuesToStr, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

                    ((IRLVSelectedDetailVM)ViewModel).Header = viewModel.Header;
                    ((IRLVSelectedDetailVM)ViewModel).Labels = labels;
                    ((IRLVSelectedDetailVM)ViewModel).Values = values;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("No configuration file found for SelectedDetailsPanel.");
            }
        }

        public void SaveConfiguration()
        {
            try
            {
                FileStream file = File.Open("RLVSelectedDetailsPanel.json", FileMode.Create);
                dynamic obj = new { Header = ((IRLVSelectedDetailVM)ViewModel).Header, Labels = ((IRLVSelectedDetailVM)ViewModel).Labels, Values = ((IRLVSelectedDetailVM)ViewModel).Values };
                using (StreamWriter st = new StreamWriter(file))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.TypeNameHandling = TypeNameHandling.Auto;
                    serializer.Formatting = Formatting.Indented;
                    serializer.Serialize(st, obj);
                }
            }
            catch(Exception e)
            {
                throw (e);
            }
        }
    }
}

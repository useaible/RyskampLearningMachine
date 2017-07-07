using RLV.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RLM.Models;
using RLV.Core.Models;
using RetailPoCSimple.Models;

namespace RetailPoCSimple
{
    public interface IRLVPlangoramOutputVisualizer : IRLVOutputVisualizer
    {
        MainWindow Parent { get; }

        // utility methods
        Item[] GetItemsFromSession(RlmLearnedSessionDetails sessionDetails);
        IEnumerable<IRLVItemDisplay> GetMetricSum(Item[] items);
    }

    public class RLVOutputVisualizer : IRLVPlangoramOutputVisualizer
    {
        public RLVOutputVisualizer(MainWindow parent)
        {
            Parent = parent;
        }

        public MainWindow Parent { get; private set; }

        public event ComparisonModeClosedDelegate ComparisonModeClosedEvent;
        public event SelectedUniqueInputSetChangedDelegate SelectedUniqueInputSetChangedEvent;
        //public event LearningComparisonDisplayResultsDelegate LearningComparisonDisplayResultsEvent;

        public void IRLVCore_LearningComparisonResultsHandler(RlmLearnedSessionDetails selectedData, RlmLearnedSessionDetails comparisonData)
        {
            //LearningComparisonDisplayResultsEvent?.Invoke(selectedData, comparisonData);
            Parent.DisplayLearningComparisonResults(selectedData, comparisonData);
        }

        public void IRLVCore_DataNotAvailableHandler(Exception e)
        {
            // todo log exception?
            Parent.NoData = true;
            //System.Windows.MessageBox.Show("Data not yet available.", "Warning", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }

        public IEnumerable<IRLVItemDisplay> SessionScoreBreakdown(RlmLearnedSessionDetails sessionDetails)
        {
            IEnumerable<IRLVItemDisplay> retVal = new List<IRLVItemDisplay>();

            if (sessionDetails != null)
            {
                Item[] items = GetItemsFromSession(sessionDetails);
                retVal = GetMetricSum(items);
            }

            return retVal;
        }

        public void SelectNewItem(IEnumerable<IRLVIOValues> inputValues, IEnumerable<IRLVIOValues> outputValues, IEnumerable<RLVItemDisplay> itemDisplay)
        {
            Parent.NoData = false;
            SelectedUniqueInputSetChangedEvent?.Invoke(inputValues, outputValues, itemDisplay);
        }

        public void CloseComparisonMode()
        {
            ComparisonModeClosedEvent?.Invoke();
        }

        public Item[] GetItemsFromSession(RlmLearnedSessionDetails sessionDetails)
        {
            var retVal = new List<Item>();
            foreach (var item in sessionDetails.Outputs.Where(a => a.CycleScore == 1))
            {
                int itemIndex = Convert.ToInt32(item.Value);
                Item itemReference = Parent.ItemsCache[itemIndex];
                retVal.Add(itemReference);
            }

            return retVal.ToArray();
        }

        public IEnumerable<IRLVItemDisplay> GetMetricSum(Item[] items)
        {
            var retVal = new List<IRLVItemDisplay>();

            double[] metrics = new double[5];

            foreach (var item in items)
            {
                var itemMetrics = PlanogramOptimizer.GetCalculatedWeightedMetricArray(item, Parent.SimulationSettings);
                for (int i = 0; i < itemMetrics.Length; i++)
                {
                    metrics[i] += itemMetrics[i];
                }
            }

            retVal.Add(new RLVItemDisplay() { Name = "Revenue", Value = metrics[0] });
            retVal.Add(new RLVItemDisplay() { Name = "Sales", Value = metrics[1] });
            retVal.Add(new RLVItemDisplay() { Name = "Customer Satisfaction", Value = metrics[2] });
            retVal.Add(new RLVItemDisplay() { Name = "Sales Per Day", Value = metrics[3] });
            retVal.Add(new RLVItemDisplay() { Name = "Reviews Per Day", Value = metrics[4] });
            
            return retVal;
        }
    }
}

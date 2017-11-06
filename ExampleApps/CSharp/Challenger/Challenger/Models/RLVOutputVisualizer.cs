using ChallengerLib.Models;
using RLM.Models;
using RLV.Core.Interfaces;
using RLV.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToastNotifications.Messages;

namespace Challenger.Models
{
    public interface IRLVChallengerOutputVisualizer : IRLVOutputVisualizer
    {
        VisualizerWindow Parent { get; }
    }

    public class RLVOutputVisualizer : IRLVChallengerOutputVisualizer
    {
        public RLVOutputVisualizer(VisualizerWindow parent)
        {
            Parent = parent;
        }

        public VisualizerWindow Parent { get; private set; }

        public event ComparisonModeClosedDelegate ComparisonModeClosedEvent;
        public event SelectedUniqueInputSetChangedDelegate SelectedUniqueInputSetChangedEvent;
        //public event LearningComparisonDisplayResultsDelegate LearningComparisonDisplayResultsEvent;

        public void IRLVCore_LearningComparisonResultsHandler(RlmLearnedSessionDetails selectedData, RlmLearnedSessionDetails comparisonData)
        {
            //LearningComparisonDisplayResultsEvent?.Invoke(selectedData, comparisonData);
            //Parent.DisplayLearningComparisonResults(selectedData, comparisonData);

            var currentMoves = new List<MoveDetails>();
            var comprisonMoves = new List<MoveDetails>();

            if (selectedData != null && selectedData.Outputs != null && selectedData.Outputs.Count() > 0 && selectedData.Inputs != null && selectedData.Inputs.Count() > 0)
            {
                for (int i = 0; i < selectedData.Inputs.Count(); i++)
                {
                    currentMoves.Add(new MoveDetails()
                    {
                        MoveNumber = Convert.ToInt32(selectedData.Inputs.ElementAt(i).Value),
                        Direction = Convert.ToInt16(selectedData.Outputs.ElementAt(i).Value)
                    });
                }
            }

            if (comparisonData != null && comparisonData.Outputs != null && comparisonData.Outputs.Count() > 0 && comparisonData.Inputs != null && comparisonData.Inputs.Count() > 0)
            {
                for (int i = 0; i < comparisonData.Inputs.Count(); i++)
                {
                    comprisonMoves.Add(new MoveDetails()
                    {
                        MoveNumber = Convert.ToInt32(comparisonData.Inputs.ElementAt(i).Value),
                        Direction = Convert.ToInt16(comparisonData.Outputs.ElementAt(i).Value)
                    });
                }
            }

            Parent.ShowComparisonData(currentMoves, comprisonMoves);
        }

        public void IRLVCore_DataNotAvailableHandler(Exception e)
        {
            // todo log exception?
            //Parent.NoData = true;
            //System.Windows.MessageBox.Show("Data not available.", "Warning", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            Parent.Notification.ShowError("Data not available.");
        }

        public IEnumerable<IRLVItemDisplay> SessionScoreBreakdown(RlmLearnedSessionDetails sessionDetails)
        {
            //IEnumerable<IRLVItemDisplay> retVal = new List<IRLVItemDisplay>();

            //if (sessionDetails != null)
            //{
            //    Item[] items = GetItemsFromSession(sessionDetails);
            //    retVal = GetMetricSum(items);
            //}

            //return retVal;
            return null;
        }

        public void SelectNewItem(IEnumerable<IRLVIOValues> inputValues, IEnumerable<IRLVIOValues> outputValues, IEnumerable<RLVItemDisplay> itemDisplay)
        {
            //Parent.NoData = false;
            SelectedUniqueInputSetChangedEvent?.Invoke(inputValues, outputValues, itemDisplay);
        }

        public void CloseComparisonMode()
        {
            ComparisonModeClosedEvent?.Invoke();
        }
    }
}


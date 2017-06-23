using RLM.Models;
using RLV.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLV.Core.Interfaces
{
    public interface IRLVOutputVisualizer
    {
        // Events
        event SelectedUniqueInputSetChangedDelegate SelectedUniqueInputSetChangedEvent;        
        event ComparisonModeClosedDelegate ComparisonModeClosedEvent;
        //event LearningComparisonDisplayResultsDelegate LearningComparisonDisplayResultsEvent;

        void IRLVCore_LearningComparisonResultsHandler(RlmLearnedSessionDetails selectedData, RlmLearnedSessionDetails comparisonData);
        void IRLVCore_DataNotAvailableHandler(Exception e);
        IEnumerable<IRLVItemDisplay> SessionScoreBreakdown(RlmLearnedSessionDetails sessionDetails);
        void SelectNewItem(IEnumerable<IRLVIOValues> inputValues, IEnumerable<IRLVIOValues> outputValues, IEnumerable<RLVItemDisplay> itemDisplay);
        void CloseComparisonMode();
    }
}

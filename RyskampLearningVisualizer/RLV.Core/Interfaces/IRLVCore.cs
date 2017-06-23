using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLV.Core.Interfaces
{
    public interface IRLVCore
    {
        event SelectedUniqueInputSetChangedResultsDelegate SelectedUniqueInputSetChangedResultsEvent;
        event SelectedCaseDetailResultsDelegate SelectedCaseDetailResultsEvent;
        event LearningComparisonResultsDelegate LearningComparisonResultsEvent;
        event ScaleChangedResultsDelegate ScaleChangedResultsEvent;
        event NextPrevCaseChangedResultsDelegate NextPrevCaseChangedResultsEvent;
        event RealTimeUpdateDelegate RealTimeUpdateEvent;
        event SelectedCaseScaleChangedResultsDelegate SelectedCaseScaleChangedResultsEvent;
        event SessionBreakdownClickResultsDelegate SessionBreakdownClickResultsEvent;

        bool IsComparisonModeOn { get; set; }

        void IRLVOutputVisualizer_ComparisonModeClosedHandler();
        void IRLVOutputVisualizer_SelectedUniqueInputSetChangedHandler(IEnumerable<IRLVIOValues> inputValues, IEnumerable<IRLVIOValues> outputValues, IEnumerable<IRLVItemDisplay> displayItems);
        void IRLVProgressionChartPanel_SelectedCaseChangedHandler(long caseId);
        void IRLVSelectedDetailsPanel_LearningComparisonHandler(long sessionId, long prevSessId);
        void IRLVScaleSelectionPanel_ScaleChangedHandler(double scale);
        void IRLVSelectedDetailsPanel_NextPrevCaseChangedHandler(long caseId, bool next);
        void IRLVProgressionChartPanel_SelectedCaseScaleChangedHandler(long selectedCaseId, double scale);
        void IRLVSelectedDetailsPanel_SessionBreakdownClickHandler(long sessionId);

        void SetupVisualizer(IEnumerable<IRLVPanel> panels, IRLVOutputVisualizer visualizer = null);
    }
}

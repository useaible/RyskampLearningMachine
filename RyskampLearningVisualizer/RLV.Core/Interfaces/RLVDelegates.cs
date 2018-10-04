using RLM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLV.Core.Interfaces
{
    public delegate void SelectedUniqueInputSetChangedDelegate(IEnumerable<IRLVIOValues> inputValues, IEnumerable<IRLVIOValues> outpuValues, IEnumerable<IRLVItemDisplay> itemDisplay);
    public delegate void SelectedUniqueInputSetChangedResultsDelegate(IEnumerable<RlmLearnedCase> data, IEnumerable<IRLVItemDisplay> itemDisplay, bool showComparison = false);

    public delegate void SelectedCaseChangedDelegate(long caseId);
    public delegate void SelectedCaseDetailResultsDelegate(IEnumerable<RlmLearnedCaseDetails> data, bool showComparison = false);

    public delegate void LearningComparisonDelegate(long sessionId, long prevSessId);
    public delegate void LearningComparisonResultsDelegate(RlmLearnedSessionDetails selectedData, RlmLearnedSessionDetails comparisonData);
    public delegate void ComparisonModeClosedDelegate();
    //public delegate void LearningComparisonDisplayResultsDelegate(RlmLearnedSessionDetails selectedData, RlmLearnedSessionDetails comparisonData);

    public delegate void ScaleChangedDelegate(double scale);
    public delegate void ScaleChangedResultsDelegate(IEnumerable<RlmLearnedCase> data);
    
    public delegate void SelectedCaseScaleChangedDelegate(long selectedCaseId, double scale);
    public delegate void SelectedCaseScaleChangedResultsDelegate(IEnumerable<RlmLearnedCase> data, long selectedCaseId);
    public delegate void SelectChartDataPointDelegate(long selectedCaseId);

    public delegate void NextPrevCaseChangedDelegate(long caseId, bool next);
    public delegate void NextPrevCaseChangedResultsDelegate(long caseId);

    public delegate void SessionBreakdownClickDelegate(long sessionId);
    public delegate void SessionBreakdownClickResultsDelegate(RlmLearnedSessionDetails details);
    
    public delegate void RealTimeUpdateDelegate(IEnumerable<RlmLearnedCase> data);

    public delegate void DataNotAvailableDelegate(Exception e);
}

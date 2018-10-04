using RLM.Models;
using RLV.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLV.Core.Interfaces
{
    public interface IRLVSelectedDetailsPanel : IRLVPanel
    {
        IRLVScaleSelectionPanel ScalePanel { get; }
        IRLVOutputVisualizer OutputVisualizer { get; set; }

        event LearningComparisonDelegate LearningComparisonEvent;
        event NextPrevCaseChangedDelegate NextPrevCaseChangedEvent;
        event SessionBreakdownClickDelegate SessionBreakdownClickEvent;

        void IRLVCore_SelectedCaseDetailResultsHandler(IEnumerable<RlmLearnedCaseDetails> data, bool showComparison = false);
        void IRLVCore_SessionBreakdownClickResultsHandler(RlmLearnedSessionDetails details);
        void LoadScalePanel(ref IRLVScaleSelectionPanel panel);
        void IRLVSelectedDetailsNavigatePrevNext(long caseId, bool isNext);
        void IRLVSelectedDetailsShowLearningComparison(long currentSessionId, long previousSessionId);
    }
}

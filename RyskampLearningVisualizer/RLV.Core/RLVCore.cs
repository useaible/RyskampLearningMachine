using RLM;
using RLM.Models;
using RLV.Core.Interfaces;
using RLV.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLV.Core
{
    public class RLVCore : IRLVCore
    {
        private string databaseName { get; set; }
        private RlmSessionCaseHistory rlmHistory;
        private IEnumerable<IRLVPanel> panels;
        private IRLVOutputVisualizer visualizer;

        private long? currentRneuronId = null;
        private long? currentSolutionId = null;
        private double currentScale = 100;

        public RLVCore(string databaseName)
        {
            rlmHistory = new RlmSessionCaseHistory(databaseName);
        }

        public event LearningComparisonResultsDelegate LearningComparisonResultsEvent;
        public event NextPrevCaseChangedResultsDelegate NextPrevCaseChangedResultsEvent;
        public event RealTimeUpdateDelegate RealTimeUpdateEvent;
        public event ScaleChangedResultsDelegate ScaleChangedResultsEvent;
        public event SelectedCaseDetailResultsDelegate SelectedCaseDetailResultsEvent;
        public event SelectedUniqueInputSetChangedResultsDelegate SelectedUniqueInputSetChangedResultsEvent;
        public event SelectedCaseScaleChangedResultsDelegate SelectedCaseScaleChangedResultsEvent;
        public event SessionBreakdownClickResultsDelegate SessionBreakdownClickResultsEvent;
        public event DataNotAvailableDelegate DataNotAvailableEvent;

        public bool IsComparisonModeOn { get; set; } = false;

        public void IRLVOutputVisualizer_ComparisonModeClosedHandler()
        {
            IsComparisonModeOn = false;
        }

        public void IRLVOutputVisualizer_SelectedUniqueInputSetChangedHandler(IEnumerable<IRLVIOValues> inputValues, IEnumerable<IRLVIOValues> outputValues, IEnumerable<IRLVItemDisplay> displayItems)
        {
            IEnumerable<RlmLearnedCase> learnedCases = GetCasesLearningEvents(inputValues, outputValues, currentScale);
            if (learnedCases != null)
            {
                SelectedUniqueInputSetChangedResultsEvent?.Invoke(learnedCases, displayItems, IsComparisonModeOn);
            }
        }

        public void IRLVProgressionChartPanel_SelectedCaseChangedHandler(long caseId)
        {
            IEnumerable<RlmLearnedCaseDetails> caseDetails = GetCaseDetails(caseId);
            SelectedCaseDetailResultsEvent?.Invoke(caseDetails, IsComparisonModeOn);
        }

        public void IRLVScaleSelectionPanel_ScaleChangedHandler(double scale)
        {
            currentScale = scale;
            if (currentRneuronId.HasValue)
            {
                IEnumerable<RlmLearnedCase> learnedCases = GetCasesLearningEvents(currentRneuronId.Value, currentSolutionId.Value, scale);
                ScaleChangedResultsEvent?.Invoke(learnedCases);
            }

            foreach(var item in panels)
            {
                if (item is IRLVScaleSelectionPanel)
                {
                    (item as IRLVScaleSelectionPanel).SetScaleValueManual(scale);
                }
            }
        }

        public void IRLVSelectedDetailsPanel_LearningComparisonHandler(long sessionId, long prevSessId)
        {
            IsComparisonModeOn = true;
            IEnumerable<RlmLearnedSessionDetails> results = GetComparisonData(sessionId, prevSessId);
            if (results.Count() > 0)
                LearningComparisonResultsEvent?.Invoke(results.First(a => a.IsCurrent), results.FirstOrDefault(a => !a.IsCurrent));
        }

        public void IRLVSelectedDetailsPanel_NextPrevCaseChangedHandler(long caseId, bool next)
        {
            // get next or previous case id
            long? nextPrevCaseId = null;
            if (next)
            {
                nextPrevCaseId = rlmHistory.GetNextPreviousLearnedCaseId(caseId, next);
            }
            else
            {
                nextPrevCaseId = rlmHistory.GetNextPreviousLearnedCaseId(caseId);
            }

            if (nextPrevCaseId.HasValue)
            {
                NextPrevCaseChangedResultsEvent?.Invoke(nextPrevCaseId.Value);
            }
        }
        
        public void IRLVProgressionChartPanel_SelectedCaseScaleChangedHandler(long selectedCaseId, double scale)
        {
            currentScale = scale;
            if (currentRneuronId.HasValue)
            {
                IEnumerable<RlmLearnedCase> learnedCases = GetCasesLearningEvents(currentRneuronId.Value, currentSolutionId.Value, scale);
                SelectedCaseScaleChangedResultsEvent?.Invoke(learnedCases, selectedCaseId);
            }

            foreach (var item in panels)
            {
                if (item is IRLVScaleSelectionPanel)
                {
                    (item as IRLVScaleSelectionPanel).SetScaleValueManual(scale);
                }
            }
        }

        public void IRLVSelectedDetailsPanel_SessionBreakdownClickHandler(long sessionId)
        {
            IEnumerable<RlmLearnedSessionDetails> results = rlmHistory.GetSessionIODetails(sessionId);
            if (results != null)
            {
                SessionBreakdownClickResultsEvent?.Invoke(results.FirstOrDefault());
            }
        }

        public virtual void SetupVisualizer(IEnumerable<IRLVPanel> panels, IRLVOutputVisualizer visualizer = null)
        {
            if (panels != null)
            {
                // iterate through panels and register each event/handler pair
                this.panels = panels;
                foreach(var panel in panels)
                {
                    if (panel is IRLVProgressionChartPanel)
                    {
                        IRLVProgressionChartPanel progPanel = panel as IRLVProgressionChartPanel;
                        progPanel.SelectedCaseChangedEvent += IRLVProgressionChartPanel_SelectedCaseChangedHandler;
                        progPanel.SelectedCaseScaleChangedEvent += IRLVProgressionChartPanel_SelectedCaseScaleChangedHandler;
                        SelectedUniqueInputSetChangedResultsEvent += progPanel.IRLVCore_SelectedUniqueInputSetChangedResultsHandler;
                        ScaleChangedResultsEvent += progPanel.IRLVCore_ScaleChangedResultsHandler;
                        NextPrevCaseChangedResultsEvent += progPanel.IRLVCore_NextPrevCaseChangedResultsHandler;
                        RealTimeUpdateEvent += progPanel.IRLVCore_RealTimeUpdateHandler;
                        SelectedCaseScaleChangedResultsEvent += progPanel.IRLVCore_SelectedCaseScaleChangedResultsHandler;
                    }

                    if (panel is IRLVSelectedDetailsPanel)
                    {
                        IRLVSelectedDetailsPanel detailsPanel = panel as IRLVSelectedDetailsPanel;
                        detailsPanel.LearningComparisonEvent += IRLVSelectedDetailsPanel_LearningComparisonHandler;
                        detailsPanel.NextPrevCaseChangedEvent += IRLVSelectedDetailsPanel_NextPrevCaseChangedHandler;
                        detailsPanel.SessionBreakdownClickEvent += IRLVSelectedDetailsPanel_SessionBreakdownClickHandler;
                        SessionBreakdownClickResultsEvent += detailsPanel.IRLVCore_SessionBreakdownClickResultsHandler;
                        SelectedCaseDetailResultsEvent += detailsPanel.IRLVCore_SelectedCaseDetailResultsHandler;
                        detailsPanel.OutputVisualizer = visualizer;
                    }

                    if (panel is IRLVScaleSelectionPanel)
                    {
                        IRLVScaleSelectionPanel scalePanel = panel as IRLVScaleSelectionPanel;
                        scalePanel.ScaleChangedEvent += IRLVScaleSelectionPanel_ScaleChangedHandler;
                        currentScale = ((RLVScaleSelectionVM)scalePanel.ViewModel).DefaultScale;
                    }
                }
            }

            // register the event/handler pair for the external app
            this.visualizer = visualizer;
            if (visualizer != null)
            {
                visualizer.ComparisonModeClosedEvent += IRLVOutputVisualizer_ComparisonModeClosedHandler;
                visualizer.SelectedUniqueInputSetChangedEvent += IRLVOutputVisualizer_SelectedUniqueInputSetChangedHandler;
                LearningComparisonResultsEvent += visualizer.IRLVCore_LearningComparisonResultsHandler;
                DataNotAvailableEvent += visualizer.IRLVCore_DataNotAvailableHandler;
            }
        }

        private IEnumerable<RlmLearnedCase> GetCasesLearningEvents(IEnumerable<IRLVIOValues> inputValues, IEnumerable<IRLVIOValues> outputValues, double scale)
        {
            IEnumerable<RlmLearnedCase> retVal = null;

            var inputValuesPair = TransformIOValues(inputValues);
            long? rneuronId = rlmHistory.GetRneuronIdFromInputs(inputValuesPair);
            long? solutionId = null;

            if (!rneuronId.HasValue)
            {
                DataNotAvailableEvent?.Invoke(new Exception("Input values did not find a matching rneuron"));
            }
            else
            {
                var outputValuesPair = TransformIOValues(outputValues);
                solutionId = rlmHistory.GetSolutionIdFromOutputs(outputValuesPair);

                if (!solutionId.HasValue)
                {
                    DataNotAvailableEvent?.Invoke(new Exception("Output values did not find a matching solution"));
                }
            }

            if (rneuronId.HasValue && solutionId.HasValue)
            {
                currentRneuronId = rneuronId.Value;
                currentSolutionId = solutionId.Value;
                retVal = GetCasesLearningEvents(rneuronId.Value, solutionId.Value, scale);
                if(retVal.Count() == 0)
                {
                    DataNotAvailableEvent?.Invoke(new Exception("Cases not yet available for this rneuron and solution combination."));
                }
            }

            return retVal;
        }

        private IEnumerable<RlmLearnedCase> GetCasesLearningEvents(long rneuronId, long solutionId, double scale)
        {
            return rlmHistory.GetLearnedCases(rneuronId, solutionId, scale);
        }

        private IEnumerable<RlmLearnedCaseDetails> GetCaseDetails(long caseId)
        {
            IEnumerable<RlmLearnedCaseDetails> retVal = null;

            long? previousCaseId = rlmHistory.GetNextPreviousLearnedCaseId(caseId);

            var caseIds = new List<long>() { caseId };
            if (previousCaseId.HasValue)
            {
                caseIds.Add(previousCaseId.Value);
            }

            retVal = rlmHistory.GetCaseDetails(caseIds.ToArray());

            foreach(var caseItem in retVal)
            {
                if (caseItem.CaseId == caseId)
                {
                    caseItem.IsCurrent = true;
                    var ioDetails = rlmHistory.GetCaseIODetails(caseId);
                    caseItem.Inputs = ioDetails[0];
                    caseItem.Outputs = ioDetails[1];
                    break;
                }
            }

            return retVal;
        }

        private IEnumerable<RlmLearnedSessionDetails> GetComparisonData(long sessionId, long prevSessId)
        {
            IEnumerable<RlmLearnedSessionDetails> retVal = rlmHistory.GetSessionIODetails(sessionId, prevSessId);

            foreach(var item in retVal)
            {
                if (item.SessionId == sessionId)
                {
                    item.IsCurrent = true;
                    break;
                }
            }

            return retVal;
        }

        private void StartRealTimeUpdate(long caseId)
        {
            throw new NotImplementedException();
        }

        private KeyValuePair<string, string>[] TransformIOValues(IEnumerable<IRLVIOValues> ioValues)
        {
            var retVal = new KeyValuePair<string, string>[ioValues.Count()];

            int index = 0;
            foreach(var item in ioValues)
            {
                retVal[index] = new KeyValuePair<string, string>(item.IOName, item.Value);
                index++;
            }

            return retVal;
        }

        public object TestConnection()
        {
            var list = new List<IRLVIOValues>();
            list.Add(new RLVIOValues() { IOName = "Slot", Value = "8" });

            currentRneuronId = -5924438487369566974;
            IRLVScaleSelectionPanel_ScaleChangedHandler(10);
            IRLVScaleSelectionPanel_ScaleChangedHandler(30);
            IRLVScaleSelectionPanel_ScaleChangedHandler(50);
            IRLVScaleSelectionPanel_ScaleChangedHandler(70);
            IRLVScaleSelectionPanel_ScaleChangedHandler(100);

            return GetComparisonData(-7809728163201080506, -561943335262828002);
            //return GetCaseDetails(1712);
            //return GetCasesLearningEvents(list, 100);
            //return rlmHistory.GetCaseIODetails(1712);
        }
    }
}

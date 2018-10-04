using System;
using System.Collections.Generic;
using System.Text;

namespace RLM.Models.Interfaces
{
    public interface IRlmDbDataLegacy
    {
        double GetVariance(Int64 networkId, int top);
        int GetTotalSimulationInSeconds(Int64 networkId);
        IEnumerable<Session> GetSessions(Int64 networkId, int? skip = null, int? take = null, bool descending = false);
        IEnumerable<RlmSessionSummary> GetSessionSummary(Int64 networkId, int groupBy, bool descending = false);
        int GetSessionCount(Int64 networkId);
        IEnumerable<Case> GetCases(long sessionId, int? skip = null, int? take = null);
        int GetCaseCount(long sessionId);
        RlmStats GetStatistics(Int64 networkId);
        int GetNumSessionSinceBestScore(long rnetworkId);

        IEnumerable<RlmSessionHistory> GetSessionHistory(int? pageFrom = null, int? pageTo = null);
        IEnumerable<RlmSessionHistory> GetSignificantLearningEvents(int? pageFrom = null, int? pageTo = null);
        IEnumerable<RlmCaseHistory> GetSessionCaseHistory(long sessionId, int? pageFrom = null, int? pageTo = null);
        RlmCaseIOHistory GetCaseIOHistory(long caseId, long rneuronId, long solutionId);
        long? GetRneuronIdFromInputs(KeyValuePair<string, string>[] inputs);
        long? GetSolutionIdFromOutputs(KeyValuePair<string, string>[] outputs);
        IEnumerable<RlmLearnedCase> GetLearnedCases(long rneuronId, long solutionId, double scale);
        IEnumerable<RlmLearnedSession> GetLearnedSessions(double scale);
        long? GetNextPreviousLearnedCaseId(long caseId, bool next = false);
        long? GetNextPreviousLearnedSessionId(long sessionId, bool next = false);
        IEnumerable<RlmLearnedSession> GetSessionDetails(params long[] sessionIds);
        IEnumerable<RlmLearnedCaseDetails> GetCaseDetails(params long[] caseIds);
        IEnumerable<RlmIODetails>[] GetCaseIODetails(long caseId);
        IEnumerable<RlmLearnedSessionDetails> GetSessionIODetails(params long[] sessionIds);
    }
}

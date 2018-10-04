// Copyright 2017 Ryskamp Innovations LLC
// License Available through the RLM License Agreement
// https://github.com/useaible/RyskampLearningMachine/blob/dev-branch/License.md

using RLM.Database;
using RLM.Models;
using RLM.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM
{
    public class RlmSessionCaseHistory
    {
        public string DatabaseName { get; private set; }
        private IRlmDbData rlmDb;

        public RlmSessionCaseHistory(IRlmDbData rlmDb)
        {
            this.rlmDb = rlmDb;
            rlmDb.Initialize();
            DatabaseName = rlmDb.DatabaseName;
        }
        public IEnumerable<RlmSessionHistory> GetSessionHistory(int? pageFrom = null, int? pageTo = null)
        {
            IEnumerable<RlmSessionHistory> retVal = rlmDb.GetSessionHistory(pageFrom, pageTo);

            return retVal;
        }

        public IEnumerable<RlmSessionHistory> GetSignificantLearningEvents(int? pageFrom = null, int? pageTo = null)
        {
            IEnumerable<RlmSessionHistory> retVal = rlmDb.GetSignificantLearningEvents(pageFrom, pageTo);

            return retVal;
        }

        public IEnumerable<RlmCaseHistory> GetSessionCaseHistory(long sessionId, int? pageFrom = null, int? pageTo = null)
        {
            IEnumerable<RlmCaseHistory> retVal = rlmDb.GetSessionCaseHistory(sessionId, pageFrom, pageTo);

            return retVal;
        }

        public RlmCaseIOHistory GetCaseIOHistory(long caseId, long rneuronId, long solutionId)
        {
            RlmCaseIOHistory retVal = rlmDb.GetCaseIOHistory(caseId, rneuronId, solutionId);

            return retVal;
        }

        public long? GetRneuronIdFromInputs(KeyValuePair<string, string>[] inputs)
        {
            long? retVal = rlmDb.GetRneuronIdFromInputs(inputs);

            return retVal;
        }

        public long? GetSolutionIdFromOutputs(KeyValuePair<string, string>[] outputs)
        {
            long? retVal = rlmDb.GetSolutionIdFromOutputs(outputs);

            return retVal;
        }

        public IEnumerable<RlmLearnedCase> GetLearnedCases(long rneuronId, long solutionId, double scale)
        {
            IEnumerable<RlmLearnedCase> retVal = rlmDb.GetLearnedCases(rneuronId, solutionId, scale);

            return retVal;
        }

        public IEnumerable<RlmLearnedSession> GetLearnedSessions(double scale)
        {
            IEnumerable<RlmLearnedSession> retVal = rlmDb.GetLearnedSessions(scale);

            return retVal;
        }

        public long? GetNextPreviousLearnedCaseId(long caseId, bool next = false)
        {
            long? retVal = rlmDb.GetNextPreviousLearnedCaseId(caseId, next);

            return retVal;
        }

        public long? GetNextPreviousLearnedSessionId(long sessionId, bool next = false)
        {
            long? retVal = rlmDb.GetNextPreviousLearnedSessionId(sessionId, next);

            return retVal;
        }

        public IEnumerable<RlmLearnedSession> GetSessionDetails(params long[] sessionIds)
        {
            IEnumerable<RlmLearnedSession> retVal = rlmDb.GetSessionDetails(sessionIds);

            return retVal;
        }

        public IEnumerable<RlmLearnedCaseDetails> GetCaseDetails(params long[] caseIds)
        {
            IEnumerable<RlmLearnedCaseDetails> retVal = rlmDb.GetCaseDetails(caseIds);

            return retVal;
        }

        public IEnumerable<RlmIODetails>[] GetCaseIODetails(long caseId)
        {
            var retVal = rlmDb.GetCaseIODetails(caseId);

            return retVal;
        }

        public IEnumerable<RlmLearnedSessionDetails> GetSessionIODetails(params long[] sessionIds)
        {
            var retVal = rlmDb.GetSessionIODetails(sessionIds).ToList();

            return retVal;
        }
    }
}

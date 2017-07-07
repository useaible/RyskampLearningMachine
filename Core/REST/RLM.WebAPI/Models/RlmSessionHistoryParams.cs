using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RLM.WebAPI.Models
{
    public class RlmFilterResultParams : RlmParams
    {
        public int? Skip { get; set; }
        public int? Take { get; set; }
    }

    public class RlmGetSessionCaseParams : RlmFilterResultParams
    {
        public long SessionId { get; set; }
    }

    public class RlmGetCaseIOParams : RlmParams
    {
        public long CaseId { get; set; }
        public long RneuronId { get; set; }
        public long SolutionId { get; set; }
    }

    public class RlmGetNextPrevLearnedCaseIdParams : RlmParams
    {
        public long? CaseId { get; set; }
        public bool IsNext { get; set; }
    }

    public class RlmGetSessionDetailsParams : RlmParams
    {
        public long[] SessionIds { get; set; }
    }

    public class RlmGetRneuronIdFromInputs : RlmParams
    {
        public KeyValuePair<string,string>[] InputValuesPair { get; set; }
    }

    public class RlmGetSolutionIdFromOutputs : RlmParams
    {
        public KeyValuePair<string, string>[] OutputValuesPair { get; set; }
    }

    public class RlmGetLearnedCasesParams : RlmParams
    {
        public long RneuronId { get; set; }
        public long SolutionId { get; set; }
        public double Scale { get; set; }
    }

    public class RlmGetLearnedCaseDetailsParams : RlmParams
    {
        public long CaseId { get; set; }
    }

    public class RlmGetCaseDetailsParams : RlmParams
    {
        public long CaseId { get; set; }
    }

    public class RlmGetCaseIODetailsParams : RlmParams
    {
        public long CaseId { get; set; }
    }
}
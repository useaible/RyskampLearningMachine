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
}
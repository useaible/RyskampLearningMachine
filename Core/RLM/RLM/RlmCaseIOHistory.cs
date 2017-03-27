// Copyright 2017 Ryskamp Innovations LLC
// License Available through the RLM License Agreement
// https://github.com/useaible/RyskampLearningMachine/blob/dev-branch/License.md

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM
{
    public class RlmCaseIOHistory
    {
        public long Id { get; set; }
        public IEnumerable<RlmCaseInputOutput> Inputs { get; set; }
        public IEnumerable<RlmCaseInputOutput> Outputs { get; set; }
    }

    public class RlmCaseInputOutput
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }
}

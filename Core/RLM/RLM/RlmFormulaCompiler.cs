using NCalc2;
using RLM.Enums;
using RLM.Models.Interfaces;
using RLM.Models.Optimizer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RLM
{
    public class RlmFormulaCompiler : IDisposable
    {
        static RlmFormulaCompiler()
        {
            // Turn off trace msgs coming from the Ncalc 3rd party library
            // NOTE since they use the default trace listener, this disables us as well if we decide to use it
            if (Trace.Listeners.Count > 0)
            {
                foreach(TraceListener listener in Trace.Listeners)
                {
                    if (listener is DefaultTraceListener)
                    {
                        listener.Filter = new EventTypeFilter(SourceLevels.Off);
                        break;
                    }
                }
            }
        }

        private Regex variableRegex = new Regex(@"\[.*?\]");
        private RlmOptimizer optimizerInstance;

        public RlmFormulaCompiler(RlmOptimizer opt)
        {
            optimizerInstance = opt;            
        }

        public object Parse(IRlmFormula rlmFormula)
        {
            var retVal = new object();
            
            foreach(var line in rlmFormula.Formula)
            {
                bool assignVar = false;
                string expressionStr = line;

                // checks if contains assignment operator
                object assignVarObject = null;
                if (line.Contains(" = "))
                {
                    int index = line.IndexOf(" = ");
                    string assignVarName = line.Substring(0, index).Trim().Replace("[", "").Replace("]", "");
                    expressionStr = line.Substring(index + 2).Trim();

                    assignVar = assignVarName != rlmFormula.Name;
                    if (assignVar)
                    {
                        assignVarObject = FindOptimizerObject(assignVarName);
                    }
                }

                var matches = variableRegex.Matches(expressionStr);
                var exp = new Expression(expressionStr);

                foreach (Match match in matches)
                {
                    string parameterKey = match.Value.Replace("[", "").Replace("]", "");
                    object variableObj = FindOptimizerObject(parameterKey);
                    if (variableObj is TrainingVariable)
                    {
                        var tv = (TrainingVariable)variableObj;
                        exp.Parameters[parameterKey] = tv.Value;
                    }
                    else if (variableObj is Resource)
                    {
                        //TODO: must check for data type
                        var res = (Resource)variableObj;
                        if (res.Type == Category.Variable || res.Type == Category.Constant)
                        {
                            exp.Parameters[parameterKey] = res.Value;
                        }
                        else // type is Computed
                        {
                            exp.Parameters[parameterKey] = Parse(res);
                        }
                    }
                    else if (variableObj is Constraint)
                    {
                        var constr = (Constraint)variableObj;
                        bool result = Convert.ToBoolean(Parse(constr));
                        if (result)
                        {
                            exp.Parameters[parameterKey] = Parse(constr.SuccessScore);
                        }
                        else
                        {
                            exp.Parameters[parameterKey] = Parse(constr.FailScore);
                        }
                    }
                    //else if (variableObj is ResourceAttributeDetails)
                    //{
                    //    var resAttrDetails = (ResourceAttributeDetails)variableObj;
                    //    exp.Parameters[parameterKey] = resAttrDetails.Value;
                    //}
                    else  // either raw value or a c# object (list?) TODO which we must support
                    {
                        exp.Parameters[parameterKey] = variableObj;
                    }
                }

                if (assignVar)
                {
                    // TODO make this dynamic instead of just double
                    double value = Convert.ToDouble(exp.Evaluate());
                    if (assignVarObject is Resource)
                    {
                        ((Resource)assignVarObject).Value = value;
                    }
                    else
                    {
                        ((TrainingVariable)assignVarObject).Value = value;
                    }
                }
                else
                {
                    retVal = exp.Evaluate();
                    if (rlmFormula.Name == RlmOptReservedKeywords.CycleScore.ToString() || rlmFormula.Name == RlmOptReservedKeywords.SessionScore.ToString())
                    {
                        retVal = optimizerInstance.TrainingVariables[rlmFormula.Name].Value = Convert.ToDouble(retVal);
                    }
                }
            }

            if (retVal is Decimal)
            {
                retVal = Convert.ToDouble(retVal);
            }

            return retVal;
        }

        private object FindOptimizerObject(string name)
        {
            object retVal = new object();
            string[] segments = name.Split('.');

            RlmOptObjectType objectType;
            RlmOptReservedKeywords? keyword = null;

            // 1st level - check reserved keywords (global vars)
            if (segments[0] == RlmOptReservedKeywords.CycleOutputs.ToString() || 
                segments[0] == RlmOptReservedKeywords.SessionOutputs.ToString() || 
                segments[0] == RlmOptReservedKeywords.CycleInputs.ToString())
            {
                objectType = RlmOptObjectType.GlobalVar;
                keyword = (RlmOptReservedKeywords)Enum.Parse(typeof(RlmOptReservedKeywords), segments[0]);
                retVal = optimizerInstance;
            }
            else if (segments[0] == RlmOptReservedKeywords.CycleScore.ToString() || 
                segments[0] == RlmOptReservedKeywords.SessionScore.ToString())
            {
                objectType = RlmOptObjectType.TrainingVar;
                keyword = (RlmOptReservedKeywords)Enum.Parse(typeof(RlmOptReservedKeywords), segments[0]);
                retVal = optimizerInstance.TrainingVariables[name];
            }
            // check resources
            else if (optimizerInstance.Resources.ContainsKey(segments[0]))
            {
                objectType = RlmOptObjectType.Resource;
                retVal = optimizerInstance.Resources[segments[0]];
            }
            // check constraints
            else if (optimizerInstance.Constraints.ContainsKey(segments[0]))
            {
                objectType = RlmOptObjectType.Constraint;
                retVal = optimizerInstance.Constraints[segments[0]];
            }
            else
            {
                var segments2 = segments[0].Split('|');
                if (segments2.Length >= 2)
                {
                    if(segments2[1] == "Max")
                    {
                        var data = optimizerInstance.Resources.Where(a => a.Value.RLMObject == RLMObject.Output && a.Value.Type == Category.Data);
                        if(data != null && data.Count() > 0)
                        {
                            var maxVal = data.First().Value.DataObjDictionary.Select(a => a.Value.AttributeDictionary[segments2[0]]).Max();
                            retVal = maxVal;
                            return retVal;
                        }
                        else
                        {
                            throw new KeyNotFoundException($"Unable to located the segment key '{segments[0]}'");
                        }
                    }
                    else if(segments2[1] == "Min")
                    {
                        var data = optimizerInstance.Resources.Where(a => a.Value.RLMObject == RLMObject.Output && a.Value.Type == Category.Data);
                        if (data != null && data.Count() > 0)
                        {
                            var minVal = data.First().Value.DataObjDictionary.Select(a => a.Value.AttributeDictionary[segments2[0]]).Min();
                            retVal = minVal;
                            return retVal;
                        }
                        else
                        {
                            throw new KeyNotFoundException($"Unable to located the segment key '{segments[0]}'");
                        }
                    }
                    else
                    {
                        throw new KeyNotFoundException($"Unable to located the segment key '{segments[0]}'");
                    }
                }
                else
                {
                    throw new KeyNotFoundException($"Unable to located the segment key '{segments[0]}'");
                }
            }

            // 2nd level
            if (segments.Length >= 2)
            {
                // for global vars
                switch (objectType)
                {
                    case RlmOptObjectType.GlobalVar:
                        retVal = null;
                        switch (keyword)
                        {
                            case RlmOptReservedKeywords.CycleOutputs:
                                if (optimizerInstance.CycleOutputs.ContainsKey(segments[1]))
                                {
                                    retVal = optimizerInstance.CycleOutputs[segments[1]];                                    
                                }
                                break;

                            case RlmOptReservedKeywords.SessionOutputs:
                                if (segments[1] == RlmOptReservedKeywords.Duplicates.ToString())
                                {
                                    int duplicates = 0;
                                    foreach(var sessOutput in optimizerInstance.SessionOutputs)
                                    {
                                        duplicates += sessOutput.Value
                                            .GroupBy(a => a)
                                            .Where(a => a.Count() > 1)
                                            .Select(a => a.Count() - 1)
                                            .Sum();
                                    }
                                    retVal = duplicates;
                                }
                                else if (optimizerInstance.SessionOutputs.ContainsKey(segments[1]))
                                {
                                    retVal = optimizerInstance.SessionOutputs[segments[1]];
                                }
                                break;

                            default: // CycleInputs
                                if (optimizerInstance.CycleInputs.ContainsKey(segments[1]))
                                {
                                    retVal = optimizerInstance.CycleInputs[segments[1]];
                                }
                                break;
                        }

                        if (retVal == null)
                        {
                            throw new KeyNotFoundException($"Unable to locate the segment key '{segments[1]}'");
                        }
                        break;

                    default: // for Resources                
                        // TODO support session level reference
                        var res = (Resource)retVal;

                        // TODO might support other data types?
                        int cycleOutput = Convert.ToInt32(optimizerInstance.CycleOutputs[segments[0]]);

                        var dataObj = res.DataObjDictionary.ElementAt(cycleOutput).Value;
                        if (dataObj.AttributeDictionary.ContainsKey(segments[1]))
                        {
                            retVal = dataObj.AttributeDictionary[segments[1]];
                        }
                        else
                        {
                            throw new KeyNotFoundException($"Unable to locate the segment key '{segments[1]}'");
                        }
                        break;
                }
            }
            
            return retVal;
        }

        public void Dispose()
        {
            optimizerInstance = null;
        }
    }
}

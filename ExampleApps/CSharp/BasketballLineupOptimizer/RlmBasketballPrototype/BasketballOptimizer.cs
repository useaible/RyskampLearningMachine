using RLM;
using RLM.SQLServer;
using RLM.Enums;
using RLM.Models.Optimizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RlmBasketballPrototype
{
    public class BasketballOptimizer
    {
        public static List<string> Players { get; set; } = new List<string>();
        public BasketballOptimizer()
        {

        }
        public static RlmOptimizer GetOptimizer()
        {
            RlmDbDataSQLServer rlmDb = new RlmDbDataSQLServer("BasketballOptimizer");
            RlmOptimizer rlmOptimizer = new RlmOptimizer(rlmDb);
            Dictionary<string, Resource> resources = new Dictionary<string, Resource>();

            //input resource
            Resource input1 = new Resource
            {
                Name = "Position",
                Type = Category.Range,
                RLMObject = RLMObject.Input,
                Min = 1,
                Max = 5,
                DataType = "System.Int32",
                RlmInputDataType = RlmInputDataType.Int,
                RlmInputType = RlmInputType.Distinct
            };
            rlmOptimizer.Resources.Add(input1.Name, input1);

            //output resource
            Resource output1 = new Resource
            {
                Name = "Player",
                Type = Category.Data,
                RLMObject = RLMObject.Output,
                DataType = "System.Int32",
                RlmInputDataType = RlmInputDataType.Int
            };
            output1.UploadDataResource("PlayerDetails2.csv");
            rlmOptimizer.Resources.Add(output1.Name, output1);

            #region Metrics
            //PG
            var pgMetric = new Resource();
            pgMetric.Name = "PGMetric";
            pgMetric.Type = Category.Computed;
            pgMetric.Formula = new string[]
            {
                @"([Player.Playmaking] * .3) + 
                ([Player.OutsideScoring] * .3) + 
                ([Player.PerimeterDefense] * .3) + 
                ([Player.Speed] * .1)"
            };
            rlmOptimizer.Resources.Add(pgMetric.Name, pgMetric);

            //SG
            var sgMetric = new Resource()
            {
                Name = "SGMetric",
                Type = Category.Computed,
                Formula = new string[]
                {
                    @"([Player.Playmaking] * .1) + 
                    ([Player.OutsideScoring] * .1) + 
                    ([Player.PerimeterDefense] * .2) + 
                    ([Player.Speed] * .6)"
                }
            };
            rlmOptimizer.Resources.Add(sgMetric.Name, sgMetric);

            //SF
            var sfMetric = new Resource()
            {
                Name = "SFMetric",
                Type = Category.Computed,
                Formula = new string[]
                {
                    @"([Player.OutsideScoring] * .1) + 
                    ([Player.PerimeterDefense] * .45) +                     
                    ([Player.Speed] * .1) + 
                    ([Player.LowPostDefense] * .15) +                     
                    ([Player.InsideScoring] * .25)"
                }
            };
            rlmOptimizer.Resources.Add(sfMetric.Name, sfMetric);

            //PF
            var pfMetric = new Resource()
            {
                Name = "PFMetric",
                Type = Category.Computed,
                Formula = new string[]
                {
                    @"([Player.Playmaking] * .05) + 
                    ([Player.OutsideScoring] * .35) + 
                    ([Player.PerimeterDefense] * .05) + 
                    ([Player.Speed] * .05) + 
                    ([Player.Rebounding] * .2) + 
                    ([Player.LowPostDefense] * .15) +  
                    ([Player.Strength] * .1) + 
                    ([Player.InsideScoring] * .05)"
                }
            };
            rlmOptimizer.Resources.Add(pfMetric.Name, pfMetric);

            //CENTER
            var centerMetric = new Resource()
            {
                Name = "CenterMetric",
                Type = Category.Computed,
                Formula = new string[]
                {
                    @"([Player.Playmaking] * .05) + 
                    ([Player.OutsideScoring] * .50) + 
                    ([Player.PerimeterDefense] * .05) + 
                    ([Player.Speed] * .1) + 
                    ([Player.Rebounding] * .1) + 
                    ([Player.LowPostDefense] * .1) + 
                    ([Player.Strength] * .1) + 
                    ([Player.InsideScoring] * .1)"
                }
            };
            rlmOptimizer.Resources.Add(centerMetric.Name, centerMetric); 
            #endregion

            #region Constraints
            //PG
            Constraint pgConst = new Constraint()
            {
                Name = "PGConstraint",
                Formula = new List<string> { "[CycleInputs.Position] == 1 && [Player.Height] > 195 && [Player.Height] <= 198" },
                FailScore = new ConstraintScore() { Name = "FailScore", Formula = new string[] { "0" } },
                SuccessScore = new ConstraintScore() { Name = "SucessScore", Formula = new string[] { "[PGMetric]" } }
            };

            //SG
            Constraint sgConst = new Constraint()
            {
                Name = "SGConstraint",
                Formula = new List<string> { "[CycleInputs.Position] == 2 && [Player.Height] > 198 && [Player.Height] <= 203" },
                FailScore = new ConstraintScore() { Name = "FailScore", Formula = new string[] { "0" } },
                SuccessScore = new ConstraintScore() { Name = "SucessScore", Formula = new string[] { "[SGMetric]" } }
            };

            //SF
            Constraint sfConst = new Constraint()
            {
                Name = "SFConstraint",
                Formula = new List<string> { "[CycleInputs.Position] == 3 && [Player.Height] == 200" },
                FailScore = new ConstraintScore() { Name = "FailScore", Formula = new string[] { "0" } },
                SuccessScore = new ConstraintScore() { Name = "SucessScore", Formula = new string[] { "[SFMetric]" } }
            };

            //PF
            Constraint pfConst = new Constraint()
            {
                Name = "PFConstraint",
                Formula = new List<string> { "[CycleInputs.Position] == 4 && [Player.Height] > 205 && [Player.Height] <= 213" },
                FailScore = new ConstraintScore() { Name = "FailScore", Formula = new string[] { "0" } },
                SuccessScore = new ConstraintScore() { Name = "SucessScore", Formula = new string[] { "[PFMetric]" } }
            };

            //CENTER
            Constraint centerConst = new Constraint()
            {
                Name = "CenterConstraint",
                Formula = new List<string> { "[CycleInputs.Position] == 5 && [Player.Height] > 205" },
                FailScore = new ConstraintScore() { Name = "FailScore", Formula = new string[] { "0" } },
                SuccessScore = new ConstraintScore() { Name = "SucessScore", Formula = new string[] { "[CenterMetric]" } }
            };

            rlmOptimizer.Constraints[pgConst.Name] = pgConst; //PG
            rlmOptimizer.Constraints[sgConst.Name] = sgConst; //SG
            rlmOptimizer.Constraints[sfConst.Name] = sfConst; //SF
            rlmOptimizer.Constraints[pfConst.Name] = pfConst; //PF
            rlmOptimizer.Constraints[centerConst.Name] = centerConst; //CENTER
            #endregion

            //Cycle scoring formula
            var cyclePhase = new ScoringPhase();
            cyclePhase.Name = "CycleScore";
            cyclePhase.Formula = new string[]
            {
                "[CycleScore] = [PGConstraint] + [SGConstraint] + [SFConstraint] + [PFConstraint] + [CenterConstraint]",
                "[SessionScore] = [SessionScore] + [CycleScore]",
            };
            rlmOptimizer.CyclePhase = cyclePhase;

            //Session scoring formula
            var sessionPhase = new ScoringPhase();
            sessionPhase.Name = "SessionScore";
            sessionPhase.Formula = new string[]
            {
                "[SessionScore]"
            };
            rlmOptimizer.SessionPhase = sessionPhase;

            //Settings
            RlmSettings settings = new RlmSettings()
            {
                StartRandomness = 30,
                EndRandomness = 0,
                MaxLinearBracket = 15,
                MinLinearBracket = 3,
                NumOfSessionsReset = 100,
                SimulationTarget = 500,
                NumScoreHits = 10,
                SimulationType = RlmSimulationType.Sessions,
                Time = new TimeSpan(0, 0, 10)
            };

            rlmOptimizer.Settings = settings;

            var resourceIn = rlmOptimizer.Resources.Where(a => a.Value.RLMObject == RLMObject.Input).First();
            rlmOptimizer.CycleInputs[resourceIn.Value.Name] = 0;

            var resourceOut = rlmOptimizer.Resources.Where(a => a.Value.RLMObject == RLMObject.Output).First();
            rlmOptimizer.CycleOutputs[resourceOut.Value.Name] = 0;

            rlmOptimizer.SessionOutputs["Player"] = new List<object> { };

            return rlmOptimizer;
        }

        public static void PrintResults(RlmOptimizer opt)
        {
            Resource r1 = opt.Resources.First(a => a.Value.RLMObject == RLMObject.Input).Value;
            for (int i = 1; i <= r1.Max; i++)
            {
                int output = Convert.ToInt32(opt.SessionOutputs["Player"].ElementAt(i - 1));
                Console.WriteLine($"Position: {i}, Player: {opt.Resources["Player"].DataObjDictionary.ElementAt(output).Value.AttributeDictionary["PlayerName"]}");
                Players.Add(opt.Resources["Player"].DataObjDictionary.ElementAt(output).Value.AttributeDictionary["PlayerName"].ToString());
            }
        }
    }
}

using RLM;
using RLM.SQLServer;
using RLM.Enums;
using RLM.Models.Interfaces;
using RLM.Models.Optimizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RlmBasketballPrototype
{
    class Program
    {
        // Change Values here for test
        private const int TargetForThisRun = 200;
        private static string FileNameForThisTest = "RLMTest" + TargetForThisRun.ToString() + "Sessions.csv";

        static void Main(string[] args)
        {
            IRlmDbData rlmDbData = new RlmDbDataSQLServer("BasketballPrototype");

            RlmOptimizer opt = new RlmOptimizer(rlmDbData);
            Dictionary<string, Resource> resources = new Dictionary<string, Resource>();

            Resource r1 = new Resource
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
            opt.Resources.Add(r1.Name, r1);

            Resource r2 = new Resource
            {
                Name = "Player",
                Type = Category.Data,
                RLMObject = RLMObject.Output,
                DataType = "System.Int32",
                RlmInputDataType = RlmInputDataType.Int
            };
            r2.UploadDataResource("PlayerDetails2.csv");
            opt.Resources.Add(r2.Name, r2);

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
                //([Player.Rebounding] * .05) + 
                //([Player.LowPostDefense] * .05) + 
                //([Player.Strength] * .05) + 
                //([Player.InsideScoring] * .05)"
            };
            opt.Resources.Add(pgMetric.Name, pgMetric);

            //SG
            var sgMetric = new Resource()
            {
                Name = "SGMetric",
                Type = Category.Computed,
                Formula = new string[]
                {
                    @"([Player.Playmaking] * .1) + 
                    ([Player.OutsideScoring] * .5) + 
                    ([Player.PerimeterDefense] * .3) + 
                    ([Player.Speed] * .1)" 
                    //([Player.Rebounding] * .07) + 
                    //([Player.LowPostDefense] * .07) + 
                    //([Player.Strength] * .06) + 
                    //([Player.InsideScoring] * .1)"
                }
            };
            opt.Resources.Add(sgMetric.Name, sgMetric);

            //SF
            var sfMetric = new Resource()
            {
                Name = "SFMetric",
                Type = Category.Computed,
                Formula = new string[]
                {
                     @"([Player.OutsideScoring] * .3) + 
                    ([Player.PerimeterDefense] * .3) +                     
                    ([Player.Speed] * .1) + 
                    ([Player.LowPostDefense] * .15) +                     
                    ([Player.InsideScoring] * .15)"
                }
            };
            opt.Resources.Add(sfMetric.Name, sfMetric);

            //PF
            var pfMetric = new Resource()
            {
                Name = "PFMetric",
                Type = Category.Computed,
                Formula = new string[]
                {
                    //@"([Player.Playmaking] * .05) + 
                    //([Player.OutsideScoring] * .05) + 
                    //([Player.PerimeterDefense] * .05) + 
                    //([Player.Speed] * .05) + 
                    @"([Player.Rebounding] * .2) + 
                    ([Player.LowPostDefense] * .15) +  
                    ([Player.Strength] * .15) + 
                    ([Player.InsideScoring] * .5)"
                }
            };
            opt.Resources.Add(pfMetric.Name, pfMetric);

            //CENTER
            var centerMetric = new Resource()
            {
                Name = "CenterMetric",
                Type = Category.Computed,
                Formula = new string[]
                {
                     //@"([Player.Playmaking] * .05) + 
                    //([Player.OutsideScoring] * .05) + 
                    //([Player.PerimeterDefense] * .05) + 
                    //([Player.Speed] * .05) + 
                    @"([Player.Rebounding] * .4) + 
                    ([Player.LowPostDefense] * .3) + 
                    ([Player.Strength] * .2) + 
                    ([Player.InsideScoring] * .1)"
                }
            };
            opt.Resources.Add(centerMetric.Name, centerMetric);

            var totalMetric = new Resource();
            totalMetric.Name = "Total";
            totalMetric.Type = Category.Variable;
            totalMetric.Value = 0;
            opt.Resources.Add(totalMetric.Name, totalMetric);

            //PG
            Constraint pgConst = new Constraint()
            {
                Name = "PGConstraint",
                Formula = new List<string> { "[CycleInputs.Position] == 1 && [Player.Height] <= 194" },
                FailScore = new ConstraintScore() { Name = "FailScore", Formula = new string[] { "0" } },
                SuccessScore = new ConstraintScore() { Name = "SucessScore", Formula = new string[] { "[PGMetric]" } }
            };

            //SG
            Constraint sgConst = new Constraint()
            {
                Name = "SGConstraint",
                Formula = new List<string> { "[CycleInputs.Position] == 2 && [Player.Height] > 194 && [Player.Height] <= 200" },
                FailScore = new ConstraintScore() { Name = "FailScore", Formula = new string[] { "0" } },
                SuccessScore = new ConstraintScore() { Name = "SucessScore", Formula = new string[] { "[SGMetric]" } }
            };

            //SF
            Constraint sfConst = new Constraint()
            {
                Name = "SFConstraint",
                Formula = new List<string> { "[CycleInputs.Position] == 3 && [Player.Height] > 200 && [Player.Height] <= 208" },
                FailScore = new ConstraintScore() { Name = "FailScore", Formula = new string[] { "0" } },
                SuccessScore = new ConstraintScore() { Name = "SucessScore", Formula = new string[] { "[SFMetric]" } }
            };

            //PF
            Constraint pfConst = new Constraint()
            {
                Name = "PFConstraint",
                Formula = new List<string> { "[CycleInputs.Position] == 4 && [Player.Height] > 208 && [Player.Height] <= 212" },
                FailScore = new ConstraintScore() { Name = "FailScore", Formula = new string[] { "0" } },
                SuccessScore = new ConstraintScore() { Name = "SucessScore", Formula = new string[] { "[PFMetric]" } }
            };

            //CENTER
            Constraint centerConst = new Constraint()
            {
                Name = "CenterConstraint",
                Formula = new List<string> { "[CycleInputs.Position] == 5 && [Player.Height] > 212" },
                FailScore = new ConstraintScore() { Name = "FailScore", Formula = new string[] { "0" } },
                SuccessScore = new ConstraintScore() { Name = "SucessScore", Formula = new string[] { "[CenterMetric]" } }
            };

            var cyclePhase = new ScoringPhase();
            cyclePhase.Name = "CycleScore";
            cyclePhase.Formula = new string[]
            {
                "[CycleScore] = [PGConstraint] + [SGConstraint] + [SFConstraint] + [PFConstraint] + [CenterConstraint]",
                "[SessionScore] = [SessionScore] + [CycleScore]",
            };
            //cyclePhase.Constraints.Add(cons1);
            opt.Constraints[pgConst.Name] = pgConst; //PG
            opt.Constraints[sgConst.Name] = sgConst; //SG
            opt.Constraints[sfConst.Name] = sfConst; //SF
            opt.Constraints[pfConst.Name] = pfConst; //PF
            opt.Constraints[centerConst.Name] = centerConst; //CENTER

            opt.CyclePhase = cyclePhase;

            var sessionPhase = new ScoringPhase();
            sessionPhase.Name = "SessionScore";
            sessionPhase.Formula = new string[]
            {
                "[SessionScore]",
                //"[Total] = 0"
            };

            opt.SessionPhase = sessionPhase;

            RlmSettings settings = new RlmSettings()
            {
                Name = "BasketballOptimizer",
                StartRandomness = 50,
                EndRandomness = 0,
                MaxLinearBracket = 15,
                MinLinearBracket = 3,
                NumOfSessionsReset = 100,
                SimulationTarget = TargetForThisRun,
                NumScoreHits = 10,
                SimulationType = RlmSimulationType.Score,
                Time = new TimeSpan(0, 5, 0),
                DatabaseName = "BasketballPrototype"
            };

            opt.StartTraining(settings);

            //Save to CSV Code
            SaveToCSV saveToCSV = new SaveToCSV(FileNameForThisTest);
            saveToCSV.WriteRLMSettings(settings);
            //saveToCSV.WriteData(opt.SessionData, "Session,SessionScore");
            //saveToCSV.WriteData(opt.PredictData, "Position,Score");
            saveToCSV.WriteSpace();

            for (int i = 1; i <= r1.Max; i++)
            {
                int output = Convert.ToInt32(opt.SessionOutputs["Player"].ElementAt(i - 1));
                string line = $"Position: {i}, Player: {opt.Resources["Player"].DataObjDictionary.ElementAt(output).Value.AttributeDictionary["PlayerName"]}";
                Console.WriteLine(line);
                saveToCSV.WriteLine(line);
            }

            saveToCSV.WriteSpace();
            //saveToCSV.WriteLine("Total Elapsed Time: " + opt.Elapsed);

            //Console.WriteLine(compiler.Parse(cyclePhase));
            Console.ReadLine();
        }
    }
}

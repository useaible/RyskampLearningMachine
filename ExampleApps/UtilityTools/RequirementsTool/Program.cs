using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;

namespace RequirementsTool
{
    class Program
    {
        [STAThread]
        static int Main(string[] args)
        {            
            if (args.Length == 0)
            {
                CheckAllWizard();
            }
            else
            {
                var checkers = new List<FeatureChecker>();
                foreach (var arg in args)
                {
                    FeatureChecker checker = null;
                    switch (arg.ToLower())
                    {
                        case "-sql":
                            checker = new SQLServerChecker();
                            break;

                        case "-net":
                            checker = new NetFrameworkChecker();
                            break;

                        case "-os":
                            checker = new OSRequirementChecker();
                            break;

                        case "-ram":
                            checker = new RAMRequirementChecker();
                            break;

                        case "-vs":
                            checker = new RAMRequirementChecker();
                            break;

                        case "-py":
                            checker = new PythonChecker();
                            break;

                        case "-iis":
                            checker = new IISChecker();
                            break;

                        case "-iisexpress":
                            checker = new IISExpressChecker();
                            break;

                        default:
                            throw new ArgumentException($"Invalid argument {arg}");
                    }

                    if (checker != null)
                        checkers.Add(checker);

                    foreach(var item in checkers)
                    {
                        bool result = item.Check();

                        if (!result)
                        {
                            return -1;
                        }
                    }
                }
            }
           
            return 0;
        }

        private static void CheckAllWizard()
        {
            List<FeatureChecker> systemCheckers = new List<FeatureChecker>()
            {
                new OSRequirementChecker(),
                new RAMRequirementChecker()
            };

            var vsChecker = new VisualStudioChecker();
            List<FeatureChecker> softwareCheckers = new List<FeatureChecker>()
            {
                new NetFrameworkChecker(),
                new SQLServerChecker(),
                vsChecker
            };

            StringBuilder resultsSb = new StringBuilder();
            StringBuilder errBuilder = new StringBuilder();

            Console.WriteLine("\nSystem Requirements Check:");

            //Python Check
            getPy:
            Console.Write("\n\nAre you using Python? y/n ");
            ConsoleKeyInfo yesPY = Console.ReadKey();

            switch (yesPY.Key)
            {
                case ConsoleKey.Y:
                    softwareCheckers.Add(new PythonChecker());
                    vsChecker.CheckPythonTools = true;
                    break;
                case ConsoleKey.N:
                    break;
                default:
                    Console.WriteLine("\nOops, invalid input. Try again.");
                    goto getPy;
            }

            //IIS Check
            getIIS:
            Console.Write("\nAre you using the Ryskamp Learning Machine(RLM) Web API? y/n ");
            ConsoleKeyInfo yesIIS = Console.ReadKey();

            switch (yesIIS.Key)
            {
                case ConsoleKey.Y:
                    softwareCheckers.Add(new IISChecker());
                    softwareCheckers.Add(new IISExpressChecker());
                    break;
                case ConsoleKey.N:
                    break;
                default:
                    Console.WriteLine("\nOops, invalid input. Try again.");
                    goto getIIS;
            }


            var sysStr = string.Format("{0}\n{1}\n{2}\n", getBorder("=", 32), "| System Requirements: |", getBorder("=", 32));
            Console.WriteLine("\n\n" + sysStr);
            resultsSb.AppendLine(sysStr);
            foreach (var v in systemCheckers)
            {
                v.Check();

                Console.WriteLine();
                resultsSb.AppendLine();

                string titleStr = $"{getBorder("-", 16)}[{v.Name}]{getBorder("-", 16)}\n";
                Console.WriteLine(titleStr);
                resultsSb.AppendLine(titleStr);

                Console.WriteLine();
                resultsSb.AppendLine();

                var strInfo = v.ToString();
                Console.WriteLine(strInfo);
                resultsSb.AppendLine(strInfo);

                Console.WriteLine();
                resultsSb.AppendLine();
            }

            Console.WriteLine();
            resultsSb.AppendLine();

            var softStr = string.Format("\n{0}\n{1}\n{2}\n", getBorder("=", 32), "| Software Requirements: |", getBorder("=", 32));
            Console.WriteLine(softStr);
            resultsSb.AppendLine(softStr);
            foreach (var v in softwareCheckers)
            {
                v.Check();

                Console.WriteLine();
                resultsSb.AppendLine();

                string titleStr = $"{getBorder("-", 16)}[{v.Name}]{getBorder("-", 16)}\n";
                Console.WriteLine(titleStr);
                resultsSb.AppendLine(titleStr);

                Console.WriteLine();
                resultsSb.AppendLine();

                var strInfo = v.ToString();
                Console.WriteLine(strInfo);
                resultsSb.AppendLine(strInfo);

                if (v is SQLServerChecker && v.HasCorrectVersion)
                {
                    // display instances with connection strings template
                    var sql = v as SQLServerChecker;
                    var instancesInfo = sql.GetInstancesInfo();
                    Console.WriteLine(instancesInfo);
                    resultsSb.AppendLine(instancesInfo);
                }

                Console.WriteLine();
                resultsSb.AppendLine();
            }

            //Copy results to clipboard
            System.Windows.Forms.Clipboard.SetText(resultsSb.ToString());
            Console.WriteLine("{0}\n{1}\n{2}", getBorder("=", 32), "| Results copied to clipboard! |", getBorder("=", 32));

            Console.ReadLine();
        }

        private static string getBorder(string character, int number)
        {
            string retVal = "";
            for (int i = 0; i < number; i++)
            {
                retVal += character;
            }
            return retVal;
        }
    }
}

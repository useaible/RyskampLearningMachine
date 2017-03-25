using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RLM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TotalRecallConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("\n\nOptions: [1] = Unfiltered Results, [2] = W/ Significant Events, [3] = Save As Html, [Esc] = Exit\n\n");
            ConsoleKeyInfo input = Console.ReadKey();

            var userInput = 0;
            switch (input.Key)
            {
                case ConsoleKey.D1:
                    userInput = 1;
                    break;
                case ConsoleKey.D2:
                    userInput = 2;
                    break;
                case ConsoleKey.D3:
                    userInput = 3;
                    break;
                case ConsoleKey.Escape:
                    userInput = 4;
                    break;
                default:
                    userInput = 1;
                    break;
            }

            getDbName:
            Console.Write("\n\nEnter database name: ");

            string dbName = Console.ReadLine();

            if (string.IsNullOrEmpty(dbName))
            {
                Console.WriteLine("Empty database name not allowed.");
                goto getDbName;
            }

            var sessionCaseApi = new SessionCaseHistory(dbName);

            if (userInput == 1)
            {
                getResults(sessionCaseApi, 1);
            }
            else if (userInput == 2)
            {
                getResults(sessionCaseApi, 2);
            }
            else if (userInput == 3)
            {
                GenerateHtmlFile(sessionCaseApi);
            }
            else if(userInput == 4)
            {
                Environment.Exit(0);
            }

            Console.ReadLine();
        }

        private static void getResults(SessionCaseHistory sessionCaseApi, int mode)
        {
            Console.WriteLine("\nGetting results...");

            var sessionHistory = mode == 1? sessionCaseApi.GetSessionHistory() : sessionCaseApi.GetSignificantLearningEvents();

            Console.WriteLine("\nRESULTS:\n");

            foreach (var h in sessionHistory)
            {
                Console.WriteLine($"Session: {h.SessionNumber}, Score: {h.SessionScore}, Start: {h.DateTimeStart}, End: {h.DateTimeStop}, Elapse: {h.Elapse}");
            }

            getSession:
            Console.WriteLine("\n\nEnter session number to view events.\n\n");
            int session = Convert.ToInt32(Console.ReadLine());

            if (session > 0)
            {
                Console.WriteLine("\nGetting events...");

                var currentSession = mode == 1? sessionHistory.ElementAt(session - 1) : sessionHistory.FirstOrDefault(a=>a.SessionNumber == session);
                var sessionEvents = sessionCaseApi.GetSessionCaseHistory(currentSession.Id);

                if (sessionEvents.Count() == 0)
                {
                    Console.WriteLine("No events found for this session! Try another one.");
                    goto getSession;
                }

                Console.WriteLine("\nEVENTS:\n\n");
                foreach (var evt in sessionEvents)
                {
                    Console.WriteLine($"Id: {evt.RowNumber}, Score: {evt.CycleScore}, Start: {evt.DateTimeStart}, End: {evt.DateTimeStop}, Elapse: {evt.Elapse}");
                }

                getEvt:
                Console.WriteLine("\n\nEnter event number to view input/output details.\n");
                int cycle = Convert.ToInt32(Console.ReadLine());

                if (cycle > 0)
                {
                    var currentCycle = sessionEvents.ElementAt(cycle - 1);
                    var ioDetail = sessionCaseApi.GetCaseIOHistory(currentCycle.Id, currentCycle.RneuronId, currentCycle.SolutionId);

                    Console.WriteLine($"\nResults for case number {cycle}");

                    Console.WriteLine("\nINPUTS:\n");
                    foreach (var cycleIn in ioDetail.Inputs)
                    {
                        Console.WriteLine($"Name: {cycleIn.Name}, Value: {cycleIn.Value}");
                    }

                    Console.WriteLine("\nOUTPUTS:\n");
                    foreach (var cycleOut in ioDetail.Outputs)
                    {
                        Console.WriteLine($"Name: {cycleOut.Name}, Value: {cycleOut.Value}");
                    }
                }

                selectEvtSess:
                Console.WriteLine("\n\n[1] = New Event, [2] = New Session, [Esc] = Exit");
                ConsoleKeyInfo evtSessIn = Console.ReadKey();

                switch (evtSessIn.Key)
                {
                    case ConsoleKey.D1:
                        goto getEvt;
                    case ConsoleKey.D2:
                        goto getSession;
                    case ConsoleKey.Escape:
                        GenerateHtmlFile(sessionCaseApi);
                        Environment.Exit(0);
                        break;
                    default:
                        goto selectEvtSess;
                }
            }
        }
        
        private static void GenerateHtmlFile(SessionCaseHistory sessionCaseApi)
        {
            getMode:
            Console.WriteLine("\nOptions: [1] = Unfiltered Results, [2] = W/ Significant Events");
            ConsoleKeyInfo userInput = Console.ReadKey();

            int mode = 0;
            if(userInput.Key == ConsoleKey.D1)
            {
                mode = 1;
            }
            else if(userInput.Key == ConsoleKey.D2)
            {
                mode = 2;
            }
            else
            {
                Console.WriteLine("\nYou're not entering the right input. Try again.");
                goto getMode;
            }


            Console.WriteLine("\nGenerating html file...");
            var sessions = mode == 1?  sessionCaseApi.GetSessionHistory() : sessionCaseApi.GetSignificantLearningEvents();

            var mainUL = "<ul>";
            foreach(var s in sessions)
            {

                mainUL += $"<li style='list-style:none;'>Session# {s.SessionNumber}";

                mainUL += $"<table style='width:25%;font-size:10px;'><thead align='left'><tr><th>Id</th><th>Score</th><th>DateTimeStart</th><th>DateTimeStop</th></tr></thead><tbody><tr><td>{s.Id}</td><td>{s.SessionScore}</td><td>{s.DateTimeStart}</td><td>{s.DateTimeStop}</td></tr></tbody></table>";

                var cases = sessionCaseApi.GetSessionCaseHistory(s.Id);

                if (cases.Count() == 0)
                    continue;

                mainUL += "<br/><span>CASES:</span>";
                var caseUL = "<ul>";
                foreach(var cd in cases)
                {
                    caseUL += $"<br/><li style='list-style:none;'>Case# {cd.RowNumber}";
                    caseUL += $"<table style='width:25%;font-size:10px;'><thead align='left'><tr><th>CaseId</th><th>Score</th><th>DateTimeStart</th><th>DateTimeStop</th></tr></thead style='border-style:solid;border-width:1px;'><tbody><tr><td>{cd.Id}</td><td>{cd.CycleScore}</td><td>{cd.DateTimeStart}</td><td>{cd.DateTimeStop}</td></tr></tbody></table>";

                    var ioTable = "<table style='width:25%;font-size:10px;'><thead align='left'><tr><th>Name</th><th>Value</th></tr></thead><tbody>";
                    var ioTable2 = "<table style='width:25%;font-size:10px;'><thead align='left'><tr><th>Name</th><th>Value</th></tr></thead><tbody>";

                    var ioDetail = sessionCaseApi.GetCaseIOHistory(cd.Id, cd.RneuronId, cd.SolutionId);


                    foreach(var ioin in ioDetail.Inputs)
                    {
                        ioTable += $"<tr><td>{ioin.Name}</td><td>{ioin.Value}</td></tr>";
                    }

                    foreach (var ioout in ioDetail.Outputs)
                    {
                        ioTable2 += $"<tr><td>{ioout.Name}</td><td>{ioout.Value}</td></tr>";
                    }

                    ioTable += "</tbody></table>";
                    ioTable2 += "</tbody></table>";

                    caseUL += "<br/>IO Details<br/><br/>";

                    var caseIO = $"<table><th>Inputs</th><th>Outputs</th><tbody><tr><td>{ioTable}</td><td>{ioTable2}</td></tr></tbody></table>";

                    caseUL += caseIO;

                    caseUL += "</li><br/>";
                }

                caseUL += "</ul>";

                mainUL += caseUL;
                mainUL += "</li>";
            }

            mainUL += "</ul>";

            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "learning_history_" + sessionCaseApi.DatabaseName + ".html";
                File.WriteAllText(path, mainUL);

                
                Console.WriteLine("\n\nData successfully generated to HTML file in the following path. " + path + "");
            }
            catch(Exception e)
            {
                Console.WriteLine("Failed to generate html file.");
            }
        }
    }
}

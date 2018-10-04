using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RLM;
using RLM.Models.Interfaces;
using RLM.SQLServer;
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
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("\n\nOptions: \n\n[1] = All Games Played \n[2] = All Games W/ Significant Learning \n[3] = Save As Html \n[Esc] = Exit\n\n");
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

            IRlmDbData sqlDbData = new RlmDbDataSQLServer(dbName);
            var sessionCaseApi = new RlmSessionCaseHistory(sqlDbData);
            bool dbOk = false;

            if (userInput == 1)
            {
                getResults(sessionCaseApi, 1, out dbOk);
                if (!dbOk)
                {
                    goto getDbName;
                }
            }
            else if (userInput == 2)
            {
                getResults(sessionCaseApi, 2, out dbOk);
                if (!dbOk)
                {
                    goto getDbName;
                }
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

        private static void getResults(RlmSessionCaseHistory sessionCaseApi, int mode, out bool dbOk)
        {
            dbOk = true;

            Console.WriteLine("\nGetting results...");

            var sessionHistory = mode == 1? sessionCaseApi.GetSessionHistory() : sessionCaseApi.GetSignificantLearningEvents();

            if(sessionHistory.Count() == 0)
            {
                Console.WriteLine("\nYou entered an invalid database. Try again.");
                dbOk = false;
                return;
            }

            Console.WriteLine("\nRESULTS:\n");

            var histStr = new StringBuilder();

            //Sessions header
            histStr.AppendLine(string.Format("{0}{1,15}{2,20}{3,33}{4,37}", "Session", "Score", "Start", "End", "Elapse"));

            foreach (var h in sessionHistory)
            {
                Console.WriteLine($"Session: {h.SessionNumber}, Score: {h.SessionScore}, Start: {h.DateTimeStart}, End: {h.DateTimeStop}, Elapse: {h.Elapse}");

                string resultsStr = string.Format("{0}{1,20}{2,30}{3,35}{4,30}", h.SessionNumber, h.SessionScore, h.DateTimeStart, h.DateTimeStop, h.Elapse);

                histStr.AppendLine(resultsStr);
            }

            if (histStr.Length > 1) {
                //System.Windows.Forms.Clipboard.SetText(histStr.ToString()); //todo: netcore
                Console.WriteLine("\n*** Results copied to clipboard! ***");
            }

            getSession:
            Console.Write("\n\nEnter session number to view events. ");
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

                var eventsStr = new StringBuilder();

                //Events header
                eventsStr.AppendLine($"[Events for Session# {currentSession.SessionNumber}]\n");
                eventsStr.AppendLine(string.Format("{0}{1,15}{2,20}{3,25}{4,27}", "Number", "Score", "Start", "End", "Elapse"));

                foreach (var evt in sessionEvents)
                {
                    Console.WriteLine($"Number: {evt.RowNumber}, Score: {evt.CycleScore}, Start: {evt.DateTimeStart}, End: {evt.DateTimeStop}, Elapse: {evt.Elapse}");

                    string resultStr = string.Format("{0}{1,18}{2,30}{3,25}{4,20}", evt.RowNumber, evt.CycleScore, evt.DateTimeStart, evt.DateTimeStop, evt.Elapse);
                    eventsStr.AppendLine(resultStr);
                }

                if (eventsStr.Length > 1)
                {
                    //System.Windows.Forms.Clipboard.SetText(eventsStr.ToString()); //todo: netcore
                    Console.WriteLine("\n*** Results copied to clipboard! ***");
                }

                getEvt:

                eventsStr = new StringBuilder();

                Console.Write("\n\nEnter event number to view input/output details. ");
                int cycle = Convert.ToInt32(Console.ReadLine());

                if (cycle > 0)
                {
                    var ioDetailsStr = new StringBuilder();

                    var currentCycle = sessionEvents.ElementAt(cycle - 1);
                    var ioDetail = sessionCaseApi.GetCaseIOHistory(currentCycle.Id, currentCycle.RneuronId, currentCycle.SolutionId);

                    eventsStr.AppendLine($"[Session# {currentSession.SessionNumber}, Case# {currentCycle.RowNumber}]\n");
                    eventsStr.AppendLine(string.Format("{0}{1,15}{2,20}{3,25}{4,27}", "Number", "Score", "Start", "End", "Elapse"));

                    string resultStr = string.Format("{0}{1,18}{2,30}{3,25}{4,20}", currentCycle.RowNumber, currentCycle.CycleScore, currentCycle.DateTimeStart, currentCycle.DateTimeStop, currentCycle.Elapse);

                    eventsStr.AppendLine(resultStr);

                    Console.WriteLine($"\nResults for case number {cycle}");

                    Console.WriteLine("\nINPUTS:\n");
                    ioDetailsStr.AppendLine("\n\n[INPUTS:]\n");

                    foreach (var cycleIn in ioDetail.Inputs)
                    {
                        Console.WriteLine($"Name: {cycleIn.Name}, Value: {cycleIn.Value}");

                        ioDetailsStr.AppendLine($"Name: {cycleIn.Name}, Value: {cycleIn.Value}");
                    }

                    Console.WriteLine("\nOUTPUTS:\n");
                    ioDetailsStr.AppendLine("\n\n[OUTPUTS:]\n");

                    foreach (var cycleOut in ioDetail.Outputs)
                    {
                        Console.WriteLine($"Name: {cycleOut.Name}, Value: {cycleOut.Value}");

                        ioDetailsStr.AppendLine($"Name: {cycleOut.Name}, Value: {cycleOut.Value}");
                    }

                    eventsStr.AppendLine(ioDetailsStr.ToString());
                    if (eventsStr.Length > 1)
                    {
                        //System.Windows.Forms.Clipboard.SetText(eventsStr.ToString()); //todo: netcore
                        Console.WriteLine("\n*** Results copied to clipboard! ***");
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
        
        private static void GenerateHtmlFile(RlmSessionCaseHistory sessionCaseApi)
        {
            getMode:

            Console.WriteLine("\n\nGenerating an HTML file. Please select an option below.");
            Console.WriteLine("\nOptions: [1] = All Games, [2] = All Games W/ Significant Learning");
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

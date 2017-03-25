using RLM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TotalRecallConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("\n\nOptions: [1] = Unfiltered Results, [2] = W/ Significant Events, [Esc] = Exit\n\n");
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
                case ConsoleKey.Escape:
                    userInput = 3;
                    break;
                default:
                    userInput = 1;
                    break;
            }

            Console.Write("\n\nEnter database name: ");

            string dbName = Console.ReadLine();//"RLM_lander_dab02539173d40db921279dc37c56887";
            var network = new RlmNetwork(dbName);

            if (userInput == 1)
            {
                Console.WriteLine("\n\nRESULTS:\n\n");

                var sessionHistory = network.GetSessionHistory();

                foreach (var h in sessionHistory)
                {
                    Console.WriteLine($"Session: {h.SessionNumber}, Score: {h.SessionScore}");
                }

                Console.WriteLine("\n\nEnter session number to view events.\n\n");
                int session = Convert.ToInt32(Console.ReadLine());

                if(session > 0)
                {
                    var currentSession = sessionHistory.ElementAt(session - 1);
                    Console.WriteLine("\n\nEVENTS:\n\n");

                    var sessionEvents = network.GetSessionCaseHistory(currentSession.Id);
                }
            }
            else if (userInput == 2)
            {
                Console.WriteLine("Significant Learning Results:");
            }
            else if (userInput == 3)
            {
                Environment.Exit(0);
            }

            Console.ReadLine();
        }
    }
}

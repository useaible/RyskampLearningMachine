using Encog.Engine.Network.Activation;
using Encog.ML;
using Encog.ML.Data;
using Encog.ML.Data.Basic;
using Encog.ML.Genetic;
using Encog.ML.Train;
using Encog.Neural.Networks;
using Encog.Neural.Networks.Training;
using Encog.Neural.Networks.Training.Anneal;
using Encog.Neural.Pattern;
using Encog.Util.Arrayutil;
using Tools;
using RLM;
using RLM.Enums;
using RLM.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogisticsGameLib;
using System.Threading;

namespace LogisticsConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            ConsoleKeyInfo choice;

            bool isExit = false;
            do
            {
                Console.Clear();
                Console.WriteLine("Logistics Simulator");
                Console.WriteLine("Select an AI");
                Console.WriteLine("1) Ryskamp Learning Machine \n2) Encog \n3) Exit");
                choice = Console.ReadKey();

                switch (choice.KeyChar)
                {
                    case '1':
                        RlmLogistics.LogisticTrain();
                        break;
                    case '2':
                        EncogLogistics.LogisticTrain();
                        break;
                    case '3':
                        isExit = true;
                        break;
                    default:
                        Console.WriteLine("\nInvalid input try again...");
                        System.Threading.Thread.Sleep(1000);
                        break;
                }
            } while (!isExit);
        }
    }
}

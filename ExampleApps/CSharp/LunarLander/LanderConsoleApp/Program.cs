using Encog.Engine.Network.Activation;
using Encog.ML;
using Encog.ML.Genetic;
using Encog.ML.Train;
using Encog.Neural.Networks;
using Encog.Neural.Networks.Training.Anneal;
using Encog.Neural.Pattern;
using LanderGameLib;
using Tools;
using RLM.Models.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LunarLanderConsoleApp
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
                Console.WriteLine("Lunar Lander");
                Console.WriteLine("Select an AI");
                Console.WriteLine("1) Ryskamp Learning Machine \n2) Encog \n3) Exit");
                choice = Console.ReadKey();

                switch (choice.KeyChar)
                {
                    case '1':
                        RLMLander.LanderTrain();
                        break;
                    case '2':
                        EncogLander.LanderTrain();
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

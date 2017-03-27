// Copyright 2017 Ryskamp Innovations LLC
// License Available through the RLM License Agreement
// https://github.com/useaible/RyskampLearningMachine/blob/dev-branch/License.md

using System;

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

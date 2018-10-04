// Copyright 2017 Ryskamp Innovations LLC
// License Available through the RLM License Agreement
// https://github.com/useaible/RyskampLearningMachine/blob/dev-branch/License.md

using RLM;
using RLM.SQLServer;
using System;

namespace LunarLanderConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Lunar Lander");

            RLMLander lander;
            if (args.Length > 0)
            {
                lander = new RLMLander(Convert.ToInt32(args[0]));
                lander.LanderTrain();
            }
            else
            {
                lander = new RLMLander();
                lander.LanderTrain();
                Console.ReadLine();
            }
        }
    }
}

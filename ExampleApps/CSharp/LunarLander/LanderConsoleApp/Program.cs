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
            Console.WriteLine("Lunar Lander");
            var lander = new RLMLander();
            lander.LanderTrain();
        }
    }
}

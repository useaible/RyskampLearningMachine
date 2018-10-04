// Copyright 2017 Ryskamp Innovations LLC
// License Available through the RLM License Agreement
// https://github.com/useaible/RyskampLearningMachine/blob/dev-branch/License.md

using System;
using System.Runtime.InteropServices;

namespace LogisticsConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // NOTE: to display data persistence progress, uncomment line 39 in RlmLogistics.cs

            Console.WriteLine("Logistics Simulator");
            var logist = new RlmLogistics();
            logist.LogisticTrain();

            Console.ReadLine();
        }        
    }
}

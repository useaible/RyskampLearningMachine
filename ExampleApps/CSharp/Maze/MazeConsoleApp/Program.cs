// Copyright 2017 Ryskamp Innovations LLC
// License Available through the RLM License Agreement
// https://github.com/useaible/RyskampLearningMachine/blob/dev-branch/License.md

using Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MazeGameLib;

namespace MazeConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Maze");
            var rlmMaze = new RlmMazePlayer();
            rlmMaze.MazeTrain();            
        }
    }
}

using Encog.ML;
using Encog.Neural.Networks;
using Encog.Neural.Networks.Training;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LanderGameLib
{
    public class PilotScore : ICalculateScore
    {
        private int sessionCnt = 0;

        public double CalculateScore(IMLMethod network)
        {
            int cnt = Interlocked.Increment(ref sessionCnt);

            NeuralPilot pilot = new NeuralPilot((BasicNetwork)network, false);

            var score = pilot.ScorePilot();
            Console.WriteLine($"Session #{cnt}, Score: {Math.Abs(score).ToString("#,##0")}");

            return score;
        }


        public bool ShouldMinimize
        {
            get { return false; }
        }


        /// <inheritdoc/>
        public bool RequireSingleThreaded
        {
            get { return false; }
        }
    }
}

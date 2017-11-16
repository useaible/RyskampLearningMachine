using PoCTools.Settings;
using RetailPoC20.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RetailPoC20
{
    public class SimulationSetter
    {
        public SimulationType SimType { get; private set; }
        public int? Sessions { get; private set; }
        public double? Hours { get; private set; }
        public double? Score { get; private set; }
        public bool EnableSimDisplay { get; set; }
        public int HiddenLayers { get; private set; }
        public int HiddenLayerNeurons { get; private set; }

        private RPOCSimulationSettings simSettings;
        private double maxScore = -1;
        private double MAX_SCORE = 0;
        private Regex regexNumbersOnly = new Regex("[^0-9]+");

        public SimulationSetter(RPOCSimulationSettings simSettings)
        {
            this.simSettings = simSettings;
        }

        public double CalculateMaxScore()
        {
            using (PlanogramContext ctx = new PlanogramContext())
            {
                MockData mock = new MockData(ctx);
                MAX_SCORE = mock.GetItemMaxScoreForTop(simSettings);
            }

            return MAX_SCORE;
        }
    }
}

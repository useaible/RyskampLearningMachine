using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoCTools.Settings
{
    public class SimulationSettings
    {
        public const int NUM_SCORE_HITS = 10;
        public const int MAX_ITEMS = 10;

        public string DBIdentifier { get; set; }
        public SimulationType SimType { get; set; } = SimulationType.Score;
        public int? Sessions { get; set; }
        public double? Hours { get; set; }
        public DateTime StartedOn { get; set; }
        public DateTime? EndsOn { get; set; }
        public double? Score { get; set; }
        public bool EnableSimDisplay { get; set; } = true;

        public double DefaultScorePercentage { get; set; }

        // encog settings
        public bool EncogSelected { get; set; } = false;
        public int HiddenLayers { get; set; } = 1;
        public int HiddenLayerNeurons { get; set; } = 0;
        public int PopulationSize { get; set; } = 500;
    }
}

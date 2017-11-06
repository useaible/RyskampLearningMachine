using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailPoC.Models
{
    public class PlanogramOptResultsSettings : PlanogramOptResults
    {
        // RLM
        public int MaxItems { get; set; }
        public int StartRandomness { get; set; }
        public int EndRandomness { get; set; }
        public int SessionsPerBatch { get; set; }
        public double CurrentRandomnessValue { get; set; }
        public string InputType { get; set; }

        // Encog and Tensorflow
        public string TrainingMethod { get; set; }
        public string ActivationFunction { get; set; }
        public string HiddenLayers { get; set; }
        public string HiddenLayerNeurons { get; set; }

        // for simulated annealing
        public string MaxTemperature { get; set; }
        public string MinTemperature { get; set; }
        public string NumCycles { get; set; }

        // for genetic algo
        public string PopulationSize { get; set; }        
    }
}

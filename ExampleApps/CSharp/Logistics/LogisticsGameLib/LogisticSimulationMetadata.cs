using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogisticsGameLib
{
    public class LogisticSimulationMetadata
    {
        public double StorageCostPerDay { get; set; }
        public double BacklogCostPerDay { get; set; }
        public int RetailerInitialInventory { get; set; }
        public int WholesalerInitialInventory { get; set; }
        public int DistributorInitialInventory { get; set; }
        public int FactoryInitialInventory { get; set; }
        public int NoOfDaysInterval { get; set; }
        public double random_action_prob { get; set; }
        public double RANDOM_ACTION_DECAY { get; set; }
        public double HIDDEN1_SIZE { get; set; }
        public double HIDDEN2_SIZE { get; set; }
        public double LEARNING_RATE { get; set; }
        public double MINIBATCH_SIZE { get; set; }
        public double DISCOUNT_FACTOR { get; set; }
        public double TARGET_UPDATE_FREQ { get; set; }
    }

    public class LogisticSimulationMetadata_Encog : LogisticSimulationMetadata
    {
        public int HiddenLayerNeurons { get; set; }
        public int TrainingMethodType { get; set; }
        public int? Cycles { get; set; }
        public double? StartTemp { get; set; }
        public double? StopTemp { get; set; }
        public int? PopulationSize { get; set; }
        public int Epochs { get; set; }
        public int? MinRandom { get; set; }
        public int? MaxRandom { get; set; }
        public double? LearnRate { get; set; }
        public double? Momentum { get; set; }
        public int ActivationFunction { get; set; }
        public bool HasBiasNeuron { get; set; }
        public List<int> HiddenLayerNeuronsInputs { get; set; } = new List<int>();
    }
}

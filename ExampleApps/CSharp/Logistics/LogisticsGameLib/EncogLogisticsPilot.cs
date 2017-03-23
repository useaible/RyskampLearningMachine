using Encog.ML;
using Encog.ML.Data;
using Encog.ML.Data.Basic;
using Encog.Neural.Networks;
using Encog.Neural.Networks.Training;
using Encog.Util.Arrayutil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using useAIble.Core.Models.GameData;
using useAIble.GameLibrary.LogisticSimulation;

namespace useAIble.AIModule
{
    public class EncogLogisticsPilot
    {
        private readonly LogisticSimulator sim;
        private readonly BasicNetwork _network;

        private NormalizedField min = new NormalizedField(NormalizationAction.Normalize, "min", 51, 0, -1, 1);
        private NormalizedField max = new NormalizedField(NormalizationAction.Normalize, "max", 120, 51, -1, 1);
        private NormalizedField units = new NormalizedField(NormalizationAction.Normalize, "units", 100, 1, -1, 1);

        public static int CycleCount { get; set; } = 0;
        public static List<int> Scores { get; set; } = new List<int>();

        public LogisticSimulator Simulator { get { return sim; } }

        public EncogLogisticsPilot(BasicNetwork network, LogisticSimulationMetadata_Encog metadata, bool turnOffMqtt = true)
        {
            _network = network;
            sim = new LogisticSimulator(
                metadata.StorageCostPerDay, 
                metadata.BacklogCostPerDay, 
                metadata.RetailerInitialInventory, 
                metadata.DistributorInitialInventory, 
                metadata.WholesalerInitialInventory, 
                metadata.FactoryInitialInventory);
        }

        public EncogLogisticOutput ScorePilot(IEnumerable<int> customerOrder, IEnumerable<LogisticSimulatorOutput> logisticOutputs = null, int? delay = null)
        {
            CycleCount++;

            var logOutput = logisticOutputs == null ? GetEncogOutput(_network) : logisticOutputs;

            // reset sim outputs
            sim.ResetSimulationOutput();

            sim.start(logOutput, (delay.HasValue) ? delay.Value : 50, customerOrder);

            var score = GetScore(sim);
            Scores.Add(20000 - score);

            var retVal = new EncogLogisticOutput() { Score = 20000 - score, Settings = logOutput };
            sim.SimulationOutput.Settings = logOutput;

            return retVal;
        }
        
        public IEnumerable<LogisticSimulatorOutput> GetEncogOutput(BasicNetwork network)
        {
            var input = new BasicMLData(1);
            input[0] = 1;

            IMLData output = _network.Compute(input);
            var logOutput = new List<LogisticSimulatorOutput>();
            logOutput.Add(new LogisticSimulatorOutput() { Name = "Retailer_Min", Value = Convert.ToInt32(min.DeNormalize(output[0])) });
            logOutput.Add(new LogisticSimulatorOutput() { Name = "Retailer_Max", Value = Convert.ToInt32(max.DeNormalize(output[1])) });
            logOutput.Add(new LogisticSimulatorOutput() { Name = "WholeSaler_Min", Value = Convert.ToInt32(min.DeNormalize(output[2])) });
            logOutput.Add(new LogisticSimulatorOutput() { Name = "WholeSaler_Max", Value = Convert.ToInt32(max.DeNormalize(output[3])) });
            logOutput.Add(new LogisticSimulatorOutput() { Name = "Distributor_Min", Value = Convert.ToInt32(min.DeNormalize(output[4])) });
            logOutput.Add(new LogisticSimulatorOutput() { Name = "Distributor_Max", Value = Convert.ToInt32(max.DeNormalize(output[5])) });
            logOutput.Add(new LogisticSimulatorOutput() { Name = "Factory_Min", Value = Convert.ToInt32(min.DeNormalize(output[6])) });
            logOutput.Add(new LogisticSimulatorOutput() { Name = "Factory_Max", Value = Convert.ToInt32(max.DeNormalize(output[7])) });
            logOutput.Add(new LogisticSimulatorOutput() { Name = "Factory_Units_Per_Day", Value = Convert.ToInt32(units.DeNormalize(output[8])) });

            return logOutput;
        }

        public int GetScore(LogisticSimulator sim)
        {
            var score = sim.SumAllCosts() * -1;
            return Convert.ToInt32(20000 - score);
        }
    }

    public class EncogLogisticsPilotScore : ICalculateScore
    {
        public EncogLogisticsPilotScore(IEnumerable<int> customerOrders, LogisticSimulationMetadata_Encog metadata)
        {
            CustomerOrders = customerOrders;
            Metadata = metadata;
        }

        public IEnumerable<int> CustomerOrders { get; set; }
        public LogisticSimulationMetadata_Encog Metadata { get; set; }

        public double CalculateScore(IMLMethod network)
        {
            EncogLogisticsPilot pilot = new EncogLogisticsPilot((BasicNetwork)network, Metadata);
            var logisticOutput =  pilot.ScorePilot(CustomerOrders);
           
            return logisticOutput.Score;
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
        
    public class EncogLogisticOutput
    {
        public double Score { get; set; }
        public IEnumerable<LogisticSimulatorOutput> Settings { get; set; }
    }
}

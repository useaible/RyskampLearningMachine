using Encog.Engine.Network.Activation;
using Encog.ML;
using Encog.ML.Data;
using Encog.ML.Data.Basic;
using Encog.ML.Genetic;
using Encog.ML.Train;
using Encog.Neural.Networks;
using Encog.Neural.Networks.Training;
using Encog.Neural.Networks.Training.Anneal;
using Encog.Neural.Pattern;
using Encog.Util.Arrayutil;
using RetailPoC20.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PoCTools.Settings;
using System.Collections.Concurrent;

namespace RetailPoC20
{
    public class PlanogramOptimizerEncog
    {
        //private Item[] items;
        private RPOCSimulationSettings simSettings;
        private SimulationCsvLogger logger;

        private BasicNetwork network;
        private IMLTrain train;
        private PlanogramScore planogramScore;

        private UpdateUINonRLMCallback updateUI;
        private UpdateStatusCallback updateStatus;
        private AutoResetEvent signal;


        const int POPULATION_SIZE = 200;
        
        // 10-24 set anneal to default false
        public PlanogramOptimizerEncog(Item[] items, RPOCSimulationSettings simSettings, UpdateUINonRLMCallback updateUI = null, UpdateStatusCallback updateStatus = null, SimulationCsvLogger logger = null, bool anneal = false)
        {
            updateStatus?.Invoke("Initializing...");

            //this.items = items;
            this.simSettings = simSettings;
            this.logger = logger;
            this.updateUI = updateUI;
            this.updateStatus = updateStatus;
            this.signal = new AutoResetEvent(false);

            network = CreateNetwork();
            planogramScore = new PlanogramScore()
            {
                SimSettings = simSettings,
                Items = items,
                UpdateUI = updateUI,
                Logger = logger
            };

            if (anneal)
            {
                train = new NeuralSimulatedAnnealing(network, planogramScore, 18, 2, 50); 
                //train = new NeuralSimulatedAnnealing(network, planogramScore, 10, 2, (simSettings.SimType == SimulationType.Sessions) ?  1 : 10); // todo make the # of cycles an input for users?
            }
            else
            {
                train = new MLMethodGeneticAlgorithm(() => {
                    ((IMLResettable)network).Reset();
                    return network;
                }, planogramScore, POPULATION_SIZE);
            }
           
            planogramScore.StopEncogTraningEvent += () =>
            {
                signal.Set();
            };
        }

        public void StartOptimization(CancellationToken? cancelToken = null)
        {
            updateStatus?.Invoke("Training...");

            double hours = simSettings.SimType == SimulationType.Time ? simSettings.Hours.Value : 0;
            simSettings.StartedOn = DateTime.Now;
            simSettings.EndsOn = DateTime.Now.AddHours(hours);
            
            do
            {
                Task.Run(() =>
                {
                    train.Iteration();
                    signal.Set();
                });

                signal.WaitOne();

                if (cancelToken.HasValue && cancelToken.Value.IsCancellationRequested)
                    break;

            } while ((simSettings.SimType == SimulationType.Sessions && simSettings.Sessions > planogramScore.SessionNumber) ||
                (simSettings.SimType == SimulationType.Time && simSettings.EndsOn > DateTime.Now) ||
                (simSettings.SimType == SimulationType.Score && RPOCSimulationSettings.NUM_SCORE_HITS > planogramScore.NumScoreHits));

            // display for final results only
            updateUI?.Invoke(planogramScore.LastResult, true);

            updateStatus?.Invoke("Done", true);
        }

        private BasicNetwork CreateNetwork()
        {
            var pattern = new FeedForwardPattern { InputNeurons = 1 };

            for (int i = 0; i < simSettings.HiddenLayers; i++)
            {
                pattern.AddHiddenLayer(simSettings.HiddenLayerNeurons);
            }

            pattern.OutputNeurons = 1;
            pattern.ActivationFunction = new ActivationElliottSymmetric();
            //pattern.ActivationFunction = new ActivationTANH();
            var network = (BasicNetwork)pattern.Generate();
            network.Reset();
            return network;
        }
    }

    public delegate void StopEncogTraining();
    public class PlanogramScore : ICalculateScore
    {
        private ConcurrentBag<double> metricScoreHistory = new ConcurrentBag<double>();
        private ConcurrentQueue<double> metricAvgLastTen = new ConcurrentQueue<double>();

        public event StopEncogTraining StopEncogTraningEvent;

        public int SessionNumber { get; set; } = 0;
        public int NumScoreHits { get; set; } = 0;
        public RPOCSimulationSettings SimSettings { get; set; }
        public Item[] Items { get; set; }
        public UpdateUINonRLMCallback UpdateUI { get; set; }
        public SimulationCsvLogger Logger { get; set; }
        public PlanogramOptResults LastResult { get; set; }
        public bool IsStopped { get; set; }

        public double CalculateScore(IMLMethod network)
        {
            if (IsStopped) return 0;

            SessionNumber++;

            DateTime startSession = DateTime.Now;

            PlanogramSimulation pilot = new PlanogramSimulation((BasicNetwork)network, Items, SimSettings);
            var result = LastResult = pilot.ScorePilot();

            result.CurrentSession = SessionNumber;
            metricScoreHistory.Add(result.Score);
            metricAvgLastTen.Enqueue(result.Score);
            if (metricAvgLastTen.Count > 10)
            {
                double outVal = 0;
                metricAvgLastTen.TryDequeue(out outVal);
            }
            result.MaxScore = metricScoreHistory.Max();
            result.MinScore = metricScoreHistory.Min();
            result.AvgScore = metricScoreHistory.Average();
            result.AvgLastTen = metricAvgLastTen.Average();

            if (SimSettings.Score.HasValue)
            {
                if (result.Score >= SimSettings.Score.Value)
                {
                    NumScoreHits++;
                }
                else
                {
                    NumScoreHits = 0;
                }
            }

            if (NumScoreHits >= RPOCSimulationSettings.NUM_SCORE_HITS)
            {
                StopEncogTraningEvent?.Invoke();
                IsStopped = true;
            }

            result.NumScoreHits = NumScoreHits;

            if (Logger != null)
            {
                SimulationData logdata = new SimulationData();

                logdata.Session = result.CurrentSession;
                logdata.Score = result.Score;
                logdata.Elapse = DateTime.Now - startSession;

                Logger.Add(logdata, false);
            }

            UpdateUI?.Invoke(result, SimSettings.EnableSimDisplay);

            return result.Score;
        }


        public bool ShouldMinimize { get { return false; ; } }

        public bool RequireSingleThreaded { get { return true; } }
    }

    public class PlanogramSimulation
    {
        const int MAX_ITEMS = 10;

        private readonly NormalizedField slotNormalizer;
        private readonly NormalizedField itemNormalizer;        
        private readonly BasicNetwork network;
        private readonly RPOCSimulationSettings simSettings;
        private readonly Item[] items;

        public static int CycleCount { get; set; }

        public PlanogramSimulation(BasicNetwork network, Item[] items, RPOCSimulationSettings simSettings)
        {
            slotNormalizer = new NormalizedField(NormalizationAction.Normalize, "Slot", (simSettings.NumSlots * simSettings.NumShelves) - 1, 0, -1, 1);
            itemNormalizer = new NormalizedField(NormalizationAction.Normalize, "Item", items.Length - 1, 0, -1, 1);

            this.items = items;
            this.simSettings = simSettings;
            this.network = network;
        }

        private bool CheckDuplicateItem(IDictionary<int, int> itemDict, int itemIndex, int numFacings = 1)
        {
            bool retVal = false;

            // change MAX_ITEMS value to 1 to enforce uniqueness or greater than 1 to enforce a certain number. 
            // -1 value (default) will be infinity, meaning we allow however many duplicates in the planogram
            if (MAX_ITEMS < 0)
            {
                retVal = true;
            }
            else
            {
                if (itemDict.ContainsKey(itemIndex))
                {
                    int count = itemDict[itemIndex];
                    count += numFacings;
                    if (count <= MAX_ITEMS)
                    {
                        itemDict[itemIndex] = count;
                        retVal = true;
                    }
                }
                else
                {
                    itemDict.Add(itemIndex, numFacings);
                    retVal = true;
                }
            }

            return retVal;
        }

        public PlanogramOptResults ScorePilot()
        {
            PlanogramOptResults retVal = new PlanogramOptResults();

            var shelves = new List<Shelf>();
            double totalMetricScore = 0;
            int slotNumber = 0;
            Dictionary<int, int> itemDict = new Dictionary<int, int>();
            bool hasExceedMax = false;

            for (int i = 0; i < simSettings.NumShelves; i++)
            {
                Shelf shelfInstance = new Shelf();
                for (int p = 0; p < simSettings.NumSlots; p++)
                {
                    var inputs = new BasicMLData(1);
                    inputs[0] = slotNormalizer.Normalize(slotNumber);

                    ////Actual Code
                    //IMLData output = network.Compute(inputs);
                    //int index = Convert.ToInt32(itemNormalizer.DeNormalize(output[0]));

                    int index = 1;
                    IMLData output = null;
                    //Test Code

                    do
                    {
                        output = network.Compute(inputs);
                        index = Convert.ToInt32(itemNormalizer.DeNormalize(output[0]));

                    } while (index < 0 || index > items.Length - 1);



                    //// Gino Code for Genetic
                    //if (index < 0 || index > items.Length - 1)
                    //{
                    //    index = 0;
                    //}

                    Item itemReference = items[index];

                    if (!hasExceedMax)
                    {
                        hasExceedMax = !(CheckDuplicateItem(itemDict, index));
                    }

                    // with the item reference we will also have the Attributes which we need to calculate for the metrics
                    double itemMetricScore = PlanogramOptimizer.GetCalculatedWeightedMetrics(itemReference, simSettings);

                    // we add the item's metric score to totalMetricScore(which is our Session score in this case)
                    // notice that we multplied it with the numFacings since that is how many will also show up on the planogram
                    totalMetricScore += itemMetricScore;
                    
                    shelfInstance.Add(itemReference, itemMetricScore);

                    slotNumber++;
                }

                shelves.Add(shelfInstance);
            }

            retVal.Shelves = shelves;
            retVal.Score = (hasExceedMax) ? 0 : totalMetricScore;
            retVal.TimeElapsed = DateTime.Now - simSettings.StartedOn;

            return retVal;
        }
    }
}

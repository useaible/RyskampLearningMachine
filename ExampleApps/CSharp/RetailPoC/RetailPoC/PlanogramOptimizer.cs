using RetailPoC.Models;
using RLM;
using RLM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailPoC
{
    //public delegate void SessionDone(PlanogramOptResults results);
    public delegate void UpdateUICallback(PlanogramOptResults results, bool enableSimDisplay);
    public delegate void UpdateStatusCallback(string statusMsg, bool isDone = false);

    /// <summary>
    /// Generates an optimized set of items based on their calculated metrics and positioned on the shelf and slot(s)
    /// </summary>
    public class PlanogramOptimizer
    {
        // modify this to enforce how many duplicate items allowed in the planogram
        // -1 = infinite
        const int MAX_ITEMS = 10; //10 Facings max of any single item

        private RlmNetwork network;
        private Item[] items;
        private SimulationSettings simSettings;

        //public event SessionDone OnSessionDone;
        private UpdateUICallback UpdateUI;
        private UpdateStatusCallback UpdateStatus;

        private List<double> metricScoreHistory = new List<double>();
        private Queue<double> metricAvgLastTen = new Queue<double>();
        public int totalSessions = 0;
        private SimulationCsvLogger logger;
        private int numScoreHits = 0;

        /// <summary>
        /// Instantiates a new instance of the plangoram optimizer
        /// </summary>
        /// <param name="items">The dataset (items with their attributes and metrics) for the RLM to learn from</param>
        /// <param name="simSettings">Holds data which dictates what type of simulation to run and for how long. Also holds the weights metrics and other general settings</param>
        /// <param name="updateUI">Callback function for sending the results of the optimization for each session</param>
        /// <param name="updateStatus">Callback function for sending the current status of the RLM</param>
        /// <param name="logger">Logs the per session stats and allows users to download the CSV file after the training</param>
        /// <remarks>Used a callback instead of an event because we worry that the display might not keep up with the optimization. You can disable the display by setting it in the Simulation panel</remarks>
        public PlanogramOptimizer(Item[] items, SimulationSettings simSettings, UpdateUICallback updateUI = null, UpdateStatusCallback updateStatus = null, SimulationCsvLogger logger = null)
        {
            this.logger = logger;
            this.items = items.ToArray();
            this.simSettings = simSettings;
            UpdateUI = updateUI;
            UpdateStatus = updateStatus;

            UpdateStatus?.Invoke("Initializing...");

            // creates the network (and the underlying DB) with a unique name to have a different network everytime you run a simulation
            network = new RlmNetwork("RLM_planogram_" + Guid.NewGuid().ToString("N"));

            // checks if the network structure already exists
            // if not then we proceed to define the inputs and outputs
            if (!network.LoadNetwork("planogram"))
            {
                string int32Type = typeof(Int32).ToString();

                var inputs = new List<RlmIO>();
                //inputs.Add(new RlmIO() { Name = "Shelf", DotNetType = int32Type, Min = 1, Max = simSettings.NumShelves, Type = RLM.Enums.RlmInputType.Linear });
                inputs.Add(new RlmIO() { Name = "Slot", DotNetType = int32Type, Min = 1, Max = simSettings.NumSlots * simSettings.NumShelves, Type = RLM.Enums.RlmInputType.Linear });

                var outputs = new List<RlmIO>();
                outputs.Add(new RlmIO() { Name = "Item", DotNetType = int32Type, Min = 0, Max = this.items.Length - 1 });

                // change Max to any number above 1 (and must not be go beyond the NumSlots value) to have multiple facings
                //outputs.Add(new RlmIO() { Name = "NumFacings", DotNetType = int32Type, Min = 1, Max = 1 });

                // creates the network
                network.NewNetwork("planogram", inputs, outputs);
            }
        }

        /// <summary>
        /// Starts optimization based on the array of items and simulation setting that were passed in during instantiation
        /// </summary>
        /// <returns>The final (predicted) output of the optimization</returns>
        public PlanogramOptResults StartOptimization()
        {
            UpdateStatus?.Invoke("Training...");

            var retVal = new PlanogramOptResults();

            // checks what type of simulation we are running
            int sessions = simSettings.SimType == SimulationType.Sessions ? simSettings.Sessions.Value : 100;
            double hours = simSettings.SimType == SimulationType.Time ? simSettings.Hours.Value : 0;
            simSettings.StartedOn = DateTime.Now;
            simSettings.EndsOn = DateTime.Now.AddHours(hours);

            // simulation scenarios:
            // if we are doing SimulationType.Sessions then the do-while loop will fail, just does the codeblock once, and trains the network with the specific number of sessions
            // if we are doing SimulationType.Time then as default it will train for 100 sessions at a time (per loop) and continues to do so until time runs out
            // if we are doing SimulationType.Score then it will execute just like the Time option except that it will continue to do so until the specified Score is met or surpassed
            do
            {
                // settings
                network.NumSessions = sessions;
                // you can change this and start experimenting which range is best
                // NOTE if you decide to have multiple facings (by changing the NumFacings output Max to greater than 1) and enforce item uniqueness then the EndRandomness must not be 0 (i suggest 5 as the minimum) 
                // as the planogram needs a bit of randomness towards the end since we have a condition where we enforce uniqueness of items. In the event that the RLM outputs an item that 
                // is already in the planogram then having enough randomness left will allow it to still have a chance to suggest a different item otherwise the RLM will suggest the same item over and over again and cause an infinite loop
                network.StartRandomness = 5;
                network.EndRandomness = (simSettings.SimType == SimulationType.Sessions) ? 0 : network.StartRandomness;
                network.MaxLinearBracket = 17;
                network.MinLinearBracket = 0;

                // since we might be retraining the network (i.e, SimulationType.Time or Score), we need to call this method to reset the randomization counter of the RLM
                network.ResetRandomizationCounter();

                // training, train 90% per session batch if not Session Type
                retVal = Optimize((simSettings.SimType == SimulationType.Sessions) ? sessions : Convert.ToInt32(sessions * .9));
                
                // for non Sessions type, we predict 10% times per session batch
                if (simSettings.SimType != SimulationType.Sessions)
                {
                    int predictTimes = Convert.ToInt32(sessions * .1);
                    if (simSettings.SimType == SimulationType.Score && predictTimes < SimulationSettings.NUM_SCORE_HITS)
                        predictTimes = SimulationSettings.NUM_SCORE_HITS;
                    retVal = Optimize(predictTimes, false);
                }

            } while ((simSettings.SimType == SimulationType.Time && simSettings.EndsOn > DateTime.Now) ||
                (simSettings.SimType == SimulationType.Score && SimulationSettings.NUM_SCORE_HITS > numScoreHits));

            // if Sessions type, we do a final prediction
            if (simSettings.SimType == SimulationType.Sessions)
            {
                retVal = Optimize(1, false);
            }

            network.TrainingDone();

            UpdateStatus?.Invoke("Done", true);

            return retVal;
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

        /// <summary>
        /// Does training or prediction for a set number of sessions
        /// </summary>
        /// <param name="sessions">Number of session to train/predict for</param>
        /// <param name="learn">Lets the RLM know if we are training or predicting</param>
        /// <returns>The final output for the training or prediction</returns>
        private PlanogramOptResults Optimize(int sessions, bool learn = true)
        {
            var output = new PlanogramOptResults();

            // holds the unique SKUs that are already in the planogram. Ensures there are no duplicate SKUs
            var hashedSku = new HashSet<int>();
            var itemDict = new Dictionary<int, int>();

            var shelves = new List<Shelf>();
            double totalMetricScore = 0;

            for (int i = 0; i < sessions; i++)
            {
                // reset for next session
                shelves.Clear();
                hashedSku.Clear();
                itemDict.Clear();
                totalMetricScore = 0;
                totalSessions++;
                DateTime startSession = DateTime.Now;

                // starts the session. we need to save the session ID as we will pass it to the cycle.Run later on
                long sessionId = network.SessionStart();

                int numSlotFlattened = 0;
                // iterates for how many number of shelves our planogram has
                for (int shelf = 1; shelf <= simSettings.NumShelves; shelf++)
                {
                    // this shelf instance will hold the items the RLM will output
                    var shelfInstance = new Shelf() { Number = shelf };
                    int itemIndex;
                    int numFacings;

                    // iterates for how many number of slots on each shelf
                    // notice that the slot is incremented depending on how many number of facings was outputed by the RLM
                    for (int slot = 1; slot <= simSettings.NumSlots; slot += numFacings)
                    {
                        itemIndex = -1;
                        numFacings = -1;
                        bool isValid = false;
                        numSlotFlattened++;
                        do
                        {
                            // create the inputs with their corresponding values
                            var inputs = new List<RlmIOWithValue>();
                            //inputs.Add(new RlmIOWithValue(network.Inputs.First(a => a.Name == "Shelf"), shelf.ToString()));
                            //inputs.Add(new RlmIOWithValue(network.Inputs.First(a => a.Name == "Slot"), slot.ToString()));
                            inputs.Add(new RlmIOWithValue(network.Inputs.First(a => a.Name == "Slot"),numSlotFlattened.ToString()));

                            // runs a cycle with the sessionId passed in
                            var cycle = new RlmCycle();
                            var rlmOutput = cycle.RunCycle(network, sessionId, inputs, learn);

                            // get the outputs
                            // the RLM outputs the item index so we will get the actual reference to the Item later on and 
                            // the second output is the number of facings that will show up on the plangram
                            itemIndex = Convert.ToInt32(rlmOutput.CycleOutput.Outputs.First(a => a.Name == "Item").Value);
                            numFacings = 1;//Convert.ToInt32(rlmOutput.CycleOutput.Outputs.First(a => a.Name == "NumFacings").Value);

                            // we calculate how many remaining slots are there left to check for validity
                            // because there might not be enough slots for what the number of facings was outputed by the RLM
                            int remainingSlots = simSettings.NumSlots - shelfInstance.Items.Count();
                            bool isWithinLimit = CheckDuplicateItem(itemDict, itemIndex, numFacings);

                            // here we check if the item is not a duplicate and that we have enough slots to fit the number of facings
                            //if (hashedSku.Add(itemIndex) && remainingSlots >= numFacings)
                            if (isWithinLimit && remainingSlots >= numFacings)
                            {
                                isValid = true;
                                Item itemReference = items[itemIndex]; // we get the item reference using the index outputed by the RLM

                                // with the item reference we will also have the Attributes which we need to calculate for the metrics
                                double itemMetricScore = PlanogramOptimizer.GetCalculatedWeightedMetrics(itemReference, simSettings);

                                // we add the item's metric score to totalMetricScore(which is our Session score in this case)
                                // notice that we multplied it with the numFacings since that is how many will also show up on the planogram
                                totalMetricScore += (itemMetricScore * numFacings);

                                // add the items to the shelf container depending on how many facings
                                for (int n = 0; n < numFacings; n++)
                                {
                                    shelfInstance.Add(itemReference, itemMetricScore);
                                }

                                // A non-duplicate is good.
                                network.ScoreCycle(rlmOutput.CycleOutput.CycleID, 1);
                            }
                            else
                            {
                                // we give the cycle a zero (0) score as it was not able to satisfy our conditions (punish it)
                                isValid = false;
                                network.ScoreCycle(rlmOutput.CycleOutput.CycleID, 0);
                                //System.Diagnostics.Debug.WriteLine("try again");
                            }
                        } while (!isValid); // if invalid, we redo the whole thing until the RLM is able to output an item that is unique and fits the remaining slot in the planogram
                    }

                    shelves.Add(shelfInstance);
                }

                // ends the session with the summed metric score for all items in the planogram
                network.SessionEnd(totalMetricScore);
                System.Diagnostics.Debug.WriteLine($"Session #{i}, Score: {totalMetricScore}");

                // set statistics and the optimized planogram shelves (and items inside them)
                output.Shelves = shelves;
                metricScoreHistory.Add(totalMetricScore);
                metricAvgLastTen.Enqueue(totalMetricScore);
                if(metricAvgLastTen.Count > 10)
                {
                    metricAvgLastTen.Dequeue();
                }
                output.Score = totalMetricScore;
                output.AvgScore = metricScoreHistory.Average();
                output.AvgLastTen = metricAvgLastTen.Average();
                output.MinScore = metricScoreHistory.Min();
                output.MaxScore = metricScoreHistory.Max();
                output.TimeElapsed = DateTime.Now - simSettings.StartedOn;
                output.CurrentSession = totalSessions;

                if (logger != null)
                {
                    SimulationData logdata = new SimulationData();

                    logdata.Session = output.CurrentSession;
                    logdata.Score = output.Score;
                    logdata.Elapse = DateTime.Now - startSession;

                    logger.Add(logdata);
                }

                // update the numScoreHits if the sim type is Score
                if (simSettings.SimType == SimulationType.Score)
                {
                    if (totalMetricScore >= simSettings.Score.Value)
                    {
                        numScoreHits++;
                        output.NumScoreHits = numScoreHits;
                    }
                    else
                    {
                        numScoreHits = 0;
                    }
                }                

                //OnSessionDone?.Invoke(output);

                // updates the results to the UI
                bool enableSimDisplay = (!learn) ? true : (learn && simSettings.EnableSimDisplay) ? true : false;
                if (enableSimDisplay)
                {
                    output.MetricMin = simSettings.ItemMetricMin;
                    output.MetricMax = simSettings.ItemMetricMax;
                    output.CalculateItemColorIntensity();
                }
                UpdateUI?.Invoke(output, enableSimDisplay);

                // checks if we have already by passed the time or score that was set
                // if we did, then we stop the training and end it abruptly
                if ((simSettings.SimType == SimulationType.Time && simSettings.EndsOn.Value <= DateTime.Now) ||
                    (simSettings.SimType == SimulationType.Score && numScoreHits >= SimulationSettings.NUM_SCORE_HITS))
                    break;

                System.Diagnostics.Debug.WriteLine("Randomness: " + network.RandomnessCurrentValue);
            }

            return output;
        }

        /// <summary>
        /// Calculates the metric for an item based on the attributes it has been assigned
        /// </summary>
        /// <param name="item">The item reference with its attributes</param>
        /// <returns>The calculated metric</returns>
        public static double GetCalculatedWeightedMetrics(Item item, SimulationSettings simSettings)
        {
            double retVal = 0;
            double[] metrics = new double[10];

            // we go through each attribute for the item and sum up each of its metric
            foreach (var attr in item.Attributes)
            {
                metrics[0] += attr.Metric1;
                metrics[1] += attr.Metric2;
                metrics[2] += attr.Metric3;
                metrics[3] += attr.Metric4;
                metrics[4] += attr.Metric5;
                metrics[5] += attr.Metric6;
                metrics[6] += attr.Metric7;
                metrics[7] += attr.Metric8;
                metrics[8] += attr.Metric9;
                metrics[9] += attr.Metric10;
            }

            // with the metrics all summed up, we then get the percentage (based on the ones set in SimulationSettings) so we can get its weight metrics
            metrics[0] = simSettings.Metric1 == 0 ? 0 : metrics[0] * (simSettings.Metric1 / 100D);
            metrics[1] = simSettings.Metric2 == 0 ? 0 : metrics[1] * (simSettings.Metric2 / 100D);
            metrics[2] = simSettings.Metric3 == 0 ? 0 : metrics[2] * (simSettings.Metric3 / 100D);
            metrics[3] = simSettings.Metric4 == 0 ? 0 : metrics[3] * (simSettings.Metric4 / 100D);
            metrics[4] = simSettings.Metric5 == 0 ? 0 : metrics[4] * (simSettings.Metric5 / 100D);
            metrics[5] = simSettings.Metric6 == 0 ? 0 : metrics[5] * (simSettings.Metric6 / 100D);
            metrics[6] = simSettings.Metric7 == 0 ? 0 : metrics[6] * (simSettings.Metric7 / 100D);
            metrics[7] = simSettings.Metric8 == 0 ? 0 : metrics[7] * (simSettings.Metric8 / 100D);
            metrics[8] = simSettings.Metric9 == 0 ? 0 : metrics[8] * (simSettings.Metric9 / 100D);
            metrics[9] = simSettings.Metric10 == 0 ? 0 : metrics[9] * (simSettings.Metric10 / 100D);

            // now having the weighted metrics calculated we simply sum it all up to get a single metric used to score the item
            retVal = metrics.Sum();

            return retVal;
        }
    }
}

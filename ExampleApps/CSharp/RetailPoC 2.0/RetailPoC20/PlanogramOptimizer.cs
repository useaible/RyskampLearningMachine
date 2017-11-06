using RetailPoC.Models;
using RLM;
using RLM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PoCTools.Settings;

namespace RetailPoC
{
    //public delegate void SessionDone(PlanogramOptResults results);
    public delegate void UpdateUICallback(PlanogramOptResultsSettings results, bool enableSimDisplay);
    public delegate void UpdateUINonRLMCallback(PlanogramOptResults results, bool enableSimDisplay);
    public delegate void UpdateStatusCallback(string statusMsg, bool isDone = false);

    /// <summary>
    /// Generates an optimized set of items based on their calculated metrics and positioned on the shelf and slot(s)
    /// </summary>
    public class PlanogramOptimizer
    {
        // modify this to enforce how many duplicate items allowed in the planogram
        // -1 = infinite
        int MAX_ITEMS = RPOCSimulationSettings.MAX_ITEMS; //10 Facings max of any single item        
        const int START_RANDOMNESS = 2;
        const int END_RANDOMNESS = 0;
        //const int MAX_LINEAR = 0;
        //const int MIN_LINEAR = 0;
        const int DEFAULT_SESSIONS_PER_BATCH = 800;
        const int PREDICT_SESSIONS = 10;
        const bool ENABLE_RLM_OUTPUT_LIMITER = true;

        private RlmNetwork network;
        private Item[] items;
        private RPOCSimulationSettings simSettings;
        private List<int> currentItemIndexes;

        //public event SessionDone OnSessionDone;
        private UpdateUICallback UpdateUI;
        private UpdateStatusCallback UpdateStatus;

        private List<double> metricScoreHistory = new List<double>();
        private Queue<double> metricAvgLastTen = new Queue<double>();
        public int totalSessions = 0;
        private SimulationCsvLogger logger;
        private int numScoreHits = 0;
        private RLM.Enums.RlmInputType inputType;

        /// <summary>
        /// Instantiates a new instance of the plangoram optimizer
        /// </summary>
        /// <param name="items">The dataset (items with their attributes and metrics) for the RLM to learn from</param>
        /// <param name="simSettings">Holds data which dictates what type of simulation to run and for how long. Also holds the weights metrics and other general settings</param>
        /// <param name="updateUI">Callback function for sending the results of the optimization for each session</param>
        /// <param name="updateStatus">Callback function for sending the current status of the RLM</param>
        /// <param name="logger">Logs the per session stats and allows users to download the CSV file after the training</param>
        /// <remarks>Used a callback instead of an event because we worry that the display might not keep up with the optimization. You can disable the display by setting it in the Simulation panel</remarks>
        public PlanogramOptimizer(Item[] items, RPOCSimulationSettings simSettings, UpdateUICallback updateUI = null, UpdateStatusCallback updateStatus = null, SimulationCsvLogger logger = null, string dbIdentifier = null)
        {
            this.logger = logger;
            this.items = items.ToArray();
            this.simSettings = simSettings;
            UpdateUI = updateUI;
            UpdateStatus = updateStatus;
            if (ENABLE_RLM_OUTPUT_LIMITER)
            {
                currentItemIndexes = new List<int>();
            }

            UpdateStatus?.Invoke("Initializing...");

            // creates the network (and the underlying DB) with a unique name to have a different network everytime you run a simulation
            network = new RlmNetwork(dbIdentifier != null ? dbIdentifier : "RLM_planogram_" +  Guid.NewGuid().ToString("N"));

            // checks if the network structure already exists
            // if not then we proceed to define the inputs and outputs

            inputType = RLM.Enums.RlmInputType.Distinct;
            if (!network.LoadNetwork("planogram"))
            {
                string int32Type = typeof(Int32).ToString();

                var inputs = new List<RlmIO>();
                //inputs.Add(new RlmIO() { Name = "Shelf", DotNetType = int32Type, Min = 1, Max = simSettings.NumShelves, Type = RLM.Enums.RlmInputType.Linear });
                inputs.Add(new RlmIO() { Name = "Slot", DotNetType = int32Type, Min = 1, Max = simSettings.NumSlots * simSettings.NumShelves, Type = inputType });

                var outputs = new List<RlmIO>();
                outputs.Add(new RlmIO() { Name = "Item", DotNetType = int32Type, Min = 0, Max = this.items.Length - 1 });

                // change Max to any number above 1 (and must not be go beyond the NumSlots value) to have multiple facings
                //outputs.Add(new RlmIO() { Name = "NumFacings", DotNetType = int32Type, Min = 1, Max = 1 });

                // creates the network
                network.NewNetwork("planogram", inputs, outputs);
            }
        }

        private int GetEquivalentIndex(int index)
        {
            var item = currentItemIndexes.ElementAt(index);
            return item;
        }

        private void ResetCurrentItemIndexes()
        {
            if (!ENABLE_RLM_OUTPUT_LIMITER) return;

            currentItemIndexes.Clear();
            for (int i = 0; i < items.Length; i++)
            {
                currentItemIndexes.Add(i);
            }
        }

        /// <summary>
        /// Starts optimization based on the array of items and simulation setting that were passed in during instantiation
        /// </summary>
        /// <returns>The final (predicted) output of the optimization</returns>
        public PlanogramOptResults StartOptimization(CancellationToken? cancelToken = null)
        {
            UpdateStatus?.Invoke("Training...");

            var retVal = new PlanogramOptResults();

            // checks what type of simulation we are running
            int sessions = simSettings.SimType == SimulationType.Sessions ? simSettings.Sessions.Value : DEFAULT_SESSIONS_PER_BATCH;
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
                network.StartRandomness = START_RANDOMNESS;
                network.EndRandomness = END_RANDOMNESS; //(simSettings.SimType == SimulationType.Sessions) ? 0 : network.StartRandomness;
                //network.MaxLinearBracket = MAX_LINEAR;
                //network.MinLinearBracket = MIN_LINEAR;

                // since we might be retraining the network (i.e, SimulationType.Time or Score), we need to call this method to reset the randomization counter of the RLM
                network.ResetRandomizationCounter();

                // training, train 90% per session batch if not Session Type
                var trainingTimes = (simSettings.SimType == SimulationType.Sessions) ? sessions : sessions - PREDICT_SESSIONS;
                retVal = Optimize(trainingTimes, true, simSettings.EnableSimDisplay);
                
                if (cancelToken.HasValue && cancelToken.Value.IsCancellationRequested)
                    return retVal;

                // for non Sessions type, we predict {PREDICT_SESSIONS}-times per session batch
                if (simSettings.SimType != SimulationType.Sessions)
                {
                    int predictTimes = PREDICT_SESSIONS;
                    if (simSettings.SimType == SimulationType.Score && predictTimes < RPOCSimulationSettings.NUM_SCORE_HITS)
                        predictTimes = RPOCSimulationSettings.NUM_SCORE_HITS;
                    retVal = Optimize(predictTimes, false, simSettings.EnableSimDisplay);
                }

            } while ((simSettings.SimType == SimulationType.Time && simSettings.EndsOn > DateTime.Now) ||
                (simSettings.SimType == SimulationType.Score && RPOCSimulationSettings.NUM_SCORE_HITS > numScoreHits));

            // we do a final prediction and to ensure we update the plangoram display for the final output
            retVal = Optimize(1, false, true);

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
                    // update the current item indexes for the rlm output limiter
                    currentItemIndexes?.Remove(itemIndex);

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
        private PlanogramOptResults Optimize(int sessions, bool learn = true, bool enablePlanogramDisplay = false, CancellationToken? cancelToken = null)
        {
            var output = new PlanogramOptResultsSettings();

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
                ResetCurrentItemIndexes();
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

                            var rlmItemOutput = network.Outputs.FirstOrDefault();
                            var rlmIdeas = new List<RlmIdea>()
                            {
                                new RlmOutputLimiter(rlmItemOutput.ID, currentItemIndexes.Count - 1, GetEquivalentIndex)
                            };

                            // runs a cycle with the sessionId passed in
                            var cycle = new RlmCycle();
                            var rlmOutput = cycle.RunCycle(network, sessionId, inputs, learn, ideas: rlmIdeas);

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
                                network.ScoreCycle(rlmOutput.CycleOutput.CycleID, -1);
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
                output.MaxItems = MAX_ITEMS;
                output.StartRandomness = network.StartRandomness;
                output.EndRandomness = network.EndRandomness;
                output.SessionsPerBatch = DEFAULT_SESSIONS_PER_BATCH;
                output.InputType = inputType.ToString();
                output.CurrentRandomnessValue = network.RandomnessCurrentValue;

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
                //bool enableSimDisplay = (!learn) ? true : (learn && simSettings.EnableSimDisplay) ? true : false;
                if (enablePlanogramDisplay)
                {
                    output.MetricMin = simSettings.ItemMetricMin;
                    output.MetricMax = simSettings.ItemMetricMax;
                    output.CalculateItemColorIntensity();
                }
                UpdateUI?.Invoke(output, enablePlanogramDisplay);

                // checks if we have already by passed the time or score that was set
                // if we did, then we stop the training and end it abruptly
                if ((simSettings.SimType == SimulationType.Time && simSettings.EndsOn.Value <= DateTime.Now) ||
                    (simSettings.SimType == SimulationType.Score && numScoreHits >= RPOCSimulationSettings.NUM_SCORE_HITS))
                    break;

                if (cancelToken.HasValue && cancelToken.Value.IsCancellationRequested)
                    return output;
            }

            return output;
        }

        /// <summary>
        /// Calculates the metric for an item based on the attributes it has been assigned
        /// </summary>
        /// <param name="item">The item reference with its attributes</param>
        /// <returns>The calculated metric</returns>
        public static double GetCalculatedWeightedMetrics(Item item, RPOCSimulationSettings simSettings)
        {
            double retVal = 0;
            double[] metrics = GetCalculatedWeightedMetricArray(item, simSettings);

            // now having the weighted metrics calculated we simply sum it all up to get a single metric used to score the item
            retVal = metrics.Sum();

            return retVal;
        }

        public static double[] GetCalculatedWeightedMetricArray(Item item, RPOCSimulationSettings simSettings)
        {
            double[] retVal = new double[10];

            // we go through each attribute for the item and sum up each of its metric
            foreach (var attr in item.Attributes)
            {
                retVal[0] += attr.Metric1;
                retVal[1] += attr.Metric2;
                retVal[2] += attr.Metric3;
                retVal[3] += attr.Metric4;
                retVal[4] += attr.Metric5;
                retVal[5] += attr.Metric6;
                retVal[6] += attr.Metric7;
                retVal[7] += attr.Metric8;
                retVal[8] += attr.Metric9;
                retVal[9] += attr.Metric10;
            }

            // with the retVal all summed up, we then get the percentage (based on the ones set in SimulationSettings) so we can get its weight retVal
            retVal[0] = simSettings.Metric1 == 0 ? 0 : retVal[0] * (simSettings.Metric1 / 100D);
            retVal[1] = simSettings.Metric2 == 0 ? 0 : retVal[1] * (simSettings.Metric2 / 100D);
            retVal[2] = simSettings.Metric3 == 0 ? 0 : retVal[2] * (simSettings.Metric3 / 100D);
            retVal[3] = simSettings.Metric4 == 0 ? 0 : retVal[3] * (simSettings.Metric4 / 100D);
            retVal[4] = simSettings.Metric5 == 0 ? 0 : retVal[4] * (simSettings.Metric5 / 100D);
            retVal[5] = simSettings.Metric6 == 0 ? 0 : retVal[5] * (simSettings.Metric6 / 100D);
            retVal[6] = simSettings.Metric7 == 0 ? 0 : retVal[6] * (simSettings.Metric7 / 100D);
            retVal[7] = simSettings.Metric8 == 0 ? 0 : retVal[7] * (simSettings.Metric8 / 100D);
            retVal[8] = simSettings.Metric9 == 0 ? 0 : retVal[8] * (simSettings.Metric9 / 100D);
            retVal[9] = simSettings.Metric10 == 0 ? 0 : retVal[9] * (simSettings.Metric10 / 100D);

            return retVal;
        }
    }
}

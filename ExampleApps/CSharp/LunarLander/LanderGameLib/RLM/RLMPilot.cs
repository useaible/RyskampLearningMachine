using RLM;
using RLM.Enums;
using RLM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanderGameLib
{
    public class RLMPilot
    {
        private const string NETWORK_NAME = "lunar lander";
        private RlmNetwork network;

        public RLMPilot(bool learn = false, int numSessions = 50, int startRandomness = 30, int endRandomness = 0, int maxLinearBracket = 15, int minLinearBracket = 3)
        {
            string dbName = "RLM_lander_" + Guid.NewGuid().ToString("N");
            network = new RlmNetwork(dbName);

            if (!network.LoadNetwork(NETWORK_NAME))
            {
                var inputs = new List<RlmIO>();
                inputs.Add(new RlmIO("fuel", typeof(System.Int32).ToString(), 0, 200, RlmInputType.Linear));
                inputs.Add(new RlmIO("altitude", typeof(System.Double).ToString(), 0, 10000, RlmInputType.Linear));
                inputs.Add(new RlmIO("velocity", typeof(System.Double).ToString(), -LanderSimulator.TerminalVelocity, LanderSimulator.TerminalVelocity, RlmInputType.Linear));

                var outputs = new List<RlmIO>();
                outputs.Add(new RlmIO("thrust", typeof(System.Boolean).ToString(), 0, 1));

                network.NewNetwork(NETWORK_NAME, inputs, outputs);
            }

            Learn = learn;
            network.NumSessions = numSessions;
            network.StartRandomness = startRandomness;
            network.EndRandomness = endRandomness;
            network.MaxLinearBracket = maxLinearBracket;
            network.MinLinearBracket = minLinearBracket;
        }

        public bool Learn { get; set; }

        public void StartSimulation(int sessionNumber, bool showOutput = true)
        {
            var sim = new LanderSimulator();
            var sessionId = network.SessionStart();

            while (sim.Flying)
            {
                var inputs = new List<RlmIOWithValue>();
                inputs.Add(new RlmIOWithValue(network.Inputs.First(a => a.Name == "fuel"), sim.Fuel.ToString()));
                inputs.Add(new RlmIOWithValue(network.Inputs.First(a => a.Name == "altitude"), Math.Round(sim.Altitude, 2).ToString()));
                inputs.Add(new RlmIOWithValue(network.Inputs.First(a => a.Name == "velocity"), Math.Round(sim.Velocity, 2).ToString()));

                var cycle = new RlmCycle();
                var cycleOutcome = cycle.RunCycle(network, network.CurrentSessionID, inputs, Learn);

                bool thrust = Convert.ToBoolean(cycleOutcome.CycleOutput.Outputs.First(a => a.Name == "thrust").Value);
                if (thrust && showOutput && !Learn)
                {
                    Console.WriteLine("@THRUST");
                }

                var score = scoreTurn(sim, thrust);
                network.ScoreCycle(cycleOutcome.CycleOutput.CycleID, score);

                sim.Turn(thrust);

                if (showOutput && !Learn)
                {
                    Console.WriteLine(sim.Telemetry());
                }

                if (sim.Fuel == 0 && sim.Altitude > 0 && sim.Velocity == -LanderSimulator.TerminalVelocity)
                    break;
            }
            
            if (showOutput)
            {
                Console.WriteLine($"Session #{ sessionNumber } \t Score: {sim.Score}");
            }
            
            network.SessionEnd(sim.Score);
        }

        public double scoreTurn(LanderSimulator sim, bool thrust)
        {
            double retVal = 0;

            if (sim.Altitude <= 1000)
            {
                if (sim.Fuel > 0 && sim.Velocity >= -5 && !thrust)
                {
                    retVal = sim.Fuel;
                }
                else if (sim.Fuel > 0 && sim.Velocity < -5 && thrust)
                {
                    retVal = sim.Fuel;
                }
            }
            else if (sim.Altitude > 1000 && !thrust)
            {
                retVal = 200;
            }

            return retVal;
        }
        
        public void TrainingDone()
        {
            network.TrainingDone();
        }        
    }
}

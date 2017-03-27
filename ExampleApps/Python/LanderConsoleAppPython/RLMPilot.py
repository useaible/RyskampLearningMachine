import clr
import uuid

from RLM import *
from RLM.Models import *
from LanderGameLib import *
from System.Collections.Generic import *

class Pilot(object):
    
    NETWORK_NAME = "lunar lander"
    network = None
    Learn = False

    def __init__(self, learn = False, numSessions = 50, startRandomness = 30, endRandomness = 0, maxLinearBracket = 15, minLinearBracket = 3):
        
        network = RlmNetwork()
        self.Learn = learn
        dbName = "RLM_lander_" + str(uuid.uuid1()).replace('-', '')
        self.network = RlmNetwork(dbName)

        print("Initializing network...\n\n")
        if not self.network.LoadNetwork(self.NETWORK_NAME):
            
            print("\n\nNetwork not found...")
            print("Creating new network...\n\n")

            inputs = List[RlmIO]()
            outputs = List[RlmIO]()
            
            inputFuel = RlmIO()
            inputAltitude = RlmIO()
            inputVelocity = RlmIO()

            outputThrust = RlmIO()

            #input settings
            inputFuel.Name = "fuel"
            inputFuel.DotNetType = "System.Int32"
            inputFuel.Min = 0
            inputFuel.Max = 200
            inputFuel.Type = 1

            inputAltitude.Name = "altitude"
            inputAltitude.DotNetType = "System.Double"
            inputAltitude.Min = 0
            inputAltitude.Max = 10000
            inputAltitude.Type = 1

            inputVelocity.Name = "velocity"
            inputVelocity.DotNetType = "System.Double"
            inputVelocity.Min = -LanderSimulator.TerminalVelocity
            inputVelocity.Max = LanderSimulator.TerminalVelocity
            inputVelocity.Type = 1

            inputs.Add(inputFuel)
            inputs.Add(inputAltitude)
            inputs.Add(inputVelocity)
            
            #output settings
            outputThrust.Name = "thrust"
            outputThrust.DotNetType = "System.Boolean"
            outputThrust.Min = 0
            outputThrust.Max = 1

            outputs.Add(outputThrust)

            #save and create network
            self.network.NewNetwork(self.NETWORK_NAME, inputs, outputs)
            print("\n\nNetwork created...")

        self.Learn = learn
        self.network.NumSessions = numSessions
        self.network.StartRandomness = startRandomness
        self.network.EndRandomness = endRandomness
        self.network.MaxLinearBracket = maxLinearBracket
        self.network.MinLinearBracket = minLinearBracket

        print("Training started...\n\n")

    def StartSimulation(self, sessionNumber, showOutput = True):
        
        sim = LanderSimulator()
        sessionId = self.network.SessionStart()

        while sim.Flying:

            inputs = List[RlmIOWithValue]()

            inputFuelObj = next((x for x in self.network.Inputs if x.Name == "fuel"), None)
            inputAltitudeObj = next((x for x in self.network.Inputs if x.Name == "altitude"), None)
            inputVelocityObj = next((x for x in self.network.Inputs if x.Name == "velocity"), None)

            inputFuel = RlmIOWithValue(inputFuelObj, str(sim.Fuel))
            inputAltitude = RlmIOWithValue(inputAltitudeObj, str(round(sim.Altitude, 2)))
            inputVelocity = RlmIOWithValue(inputVelocityObj, str(round(sim.Velocity, 2)))

            inputs.Add(inputFuel)
            inputs.Add(inputAltitude)
            inputs.Add(inputVelocity)

            cycle = RlmCycle()
            cycleOutcome = cycle.RunCycle(self.network, self.network.CurrentSessionID, inputs, self.Learn)

            outcomeVal = next((x for x in cycleOutcome.CycleOutput.Outputs if x.Name == "thrust"), None)
            thrust = True if outcomeVal.Value == 'True' else False
            if thrust and showOutput and not self.Learn:
                print("@THRUST")

            score = self.scoreTurn(sim, thrust)
            self.network.ScoreCycle(cycleOutcome.CycleOutput.CycleID, score)

            sim.Turn(thrust)

            if showOutput and not self.Learn:
                print(sim.Telemetry())

            if sim.Fuel == 0 and sim.Altitude > 0 and sim.Velocity == -LanderSimulator.TerminalVelocity:
                break
            
        if showOutput:
            print("Session #" + str(sessionNumber) + "\t Score: " + str(sim.Score))

        self.network.SessionEnd(sim.Score)

    def scoreTurn(self, sim, thrust):

        retVal = 0

        if sim.Altitude <= 1000:
            if sim.Fuel > 0 and sim.Velocity >= -5 and not thrust:
                retVal = sim.Fuel
            elif sim.Fuel > 0 and sim.Velocity < -5 and thrust:
                retVal = sim.Fuel
        elif sim.Altitude > 1000 and not thrust:
            retVal = 200

        return retVal

    def trainingDone(self):
        self.network.TrainingDone()

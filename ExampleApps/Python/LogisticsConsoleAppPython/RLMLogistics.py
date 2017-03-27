import clr
import uuid

from RLM import *
from RLM.Models import *
from LogisticsGameLib import *
from System.Collections.Generic import *

class RLMPlayer(object):
   
    def LogisticTrain(self):

        print("\n\nRLM network settings:\n")

        #Get user inputs for the network settings.
        sessions = self.getInput("\nEnter number of sessions [default 100]:", 100)
        startRand = self.getInput("Enter start randomness [default 100]:", 100)
        endRand = self.getInput("Enter end randomness [default 0]:", 0)

        dbName = "RLM_logistic_" + str(uuid.uuid1()).replace('-', '') #The database where training results will be saved.
        networkName = "Logistics Network" #The name of our network
        simulator = None

        #Default customer order data for the simulator.
        orderData = [11, 1, 16, 6, 10, 26, 1, 25, 28, 25, 2, 23, 3, 5, 24, 20, 3, 27, 22, 24, 19, 29, 27, 28, 24, 2, 9, 26, 7, 4, 18, 21, 18, 26, 8, 27, 5, 29, 27, 6, 6, 17, 4, 6, 3, 22, 17, 1, 21, 21, 1, 21, 27, 19, 17, 11, 4, 18, 11, 8, 18, 5, 18, 25, 7, 10, 6, 7, 27, 27, 15, 3, 29, 17, 19, 2, 25, 21, 25, 3, 15, 9, 5, 15, 4, 27, 23, 29, 9, 26, 24, 16, 19, 19, 17, 1, 28, 9, 29, 23]
        customerOrders = List[int]()

        for item in orderData:
            customerOrders.Add(item)

        network = RlmNetwork(dbName) #Create an instance of RLM network with the specified database name.

        print("Initializing network...\n\n")
        if not network.LoadNetwork(networkName): #Check if the current network already exist otherwise have it created.

            print("\n\nNetwork not found...")
            print("Creating new network...\n\n")

            inputs = List[RlmIO]()
            outputs = List[RlmIO]()
            
            inputX = RlmIO()

            outputRetailerMin = RlmIO()
            outputRetailerMax = RlmIO()
            outputWholesalerMin = RlmIO()
            outputWholesalerMax = RlmIO()
            outputDistributorMin = RlmIO()
            outputDistributorMax = RlmIO()
            outputFactoryMin = RlmIO()
            outputFactoryMax = RlmIO()
            outputFactoryUnitsPerDay = RlmIO()

            #input settings
            inputX.Name = "X"
            inputX.DotNetType = "System.Int32"
            inputX.Min = 1
            inputX.Max = 1
            inputX.Type = 0

            #Add input to list
            inputs.Add(inputX)
            
            #output settings
            minFrom = 0
            minTo = 50
            maxFrom = 51
            maxTo = 120
            factoryMin = 1
            factoryMax = 100

            #Retailer
            outputRetailerMin.Name = "Retailer_Min"
            outputRetailerMin.DotNetType = "System.Int16"
            outputRetailerMin.Min = minFrom
            outputRetailerMin.Max = minTo

            outputRetailerMax.Name = "Retailer_Max"
            outputRetailerMax.DotNetType = "System.Int16"
            outputRetailerMax.Min = maxFrom
            outputRetailerMax.Max = maxTo

            #Wholesaler
            outputWholesalerMin.Name = "WholeSaler_Min"
            outputWholesalerMin.DotNetType = "System.Int16"
            outputWholesalerMin.Min = minFrom
            outputWholesalerMin.Max = minTo

            outputWholesalerMax.Name = "WholeSaler_Max"
            outputWholesalerMax.DotNetType = "System.Int16"
            outputWholesalerMax.Min = maxFrom
            outputWholesalerMax.Max = maxTo

            #Distributor
            outputDistributorMin.Name = "Distributor_Min"
            outputDistributorMin.DotNetType = "System.Int16"
            outputDistributorMin.Min = minFrom
            outputDistributorMin.Max = minTo

            outputDistributorMax.Name = "Distributor_Max"
            outputDistributorMax.DotNetType = "System.Int16"
            outputDistributorMax.Min = maxFrom
            outputDistributorMax.Max = maxTo

            #Factory
            outputFactoryMin.Name = "Factory_Min"
            outputFactoryMin.DotNetType = "System.Int16"
            outputFactoryMin.Min = minFrom
            outputFactoryMin.Max = minTo

            outputFactoryMax.Name = "Factory_Max"
            outputFactoryMax.DotNetType = "System.Int16"
            outputFactoryMax.Min = maxFrom
            outputFactoryMax.Max = maxTo

            #Factory Units Per Day
            outputFactoryUnitsPerDay.Name = "Factory_Units_Per_Day"
            outputFactoryUnitsPerDay.DotNetType = "System.Int16"
            outputFactoryUnitsPerDay.Min = factoryMin
            outputFactoryUnitsPerDay.Max = factoryMax

            #Add all outputs to list
            outputs.Add(outputRetailerMin)
            outputs.Add(outputRetailerMax)
            outputs.Add(outputWholesalerMin)
            outputs.Add(outputWholesalerMax)
            outputs.Add(outputDistributorMin)
            outputs.Add(outputDistributorMax)
            outputs.Add(outputFactoryMin)
            outputs.Add(outputFactoryMax)
            outputs.Add(outputFactoryUnitsPerDay)

            #Create and save the network
            network.NewNetwork(networkName, inputs, outputs)
            print("\n\nNetwork created...")

        #RLM network settings
        network.NumSessions = sessions
        network.StartRandomness = startRand
        network.EndRandomness = endRand

        #simulator settings
        storageCost = 0.5
        backlogCost = 1
        initialInventory = 50

        #LogisticSimulator class is a c# object that process the simulation of the beer game
        #You can create your own simulator in python to replace this one if needed, but for us, we just gonna use our existing c# simulator
        #as it will be a big work for us to translate all of our simulator codes to python
        simulator = LogisticSimulator(storageCost, backlogCost, initialInventory, initialInventory, initialInventory, initialInventory)

        #Start the training
        print("Training started...\n\n")
        predictedLogisticOutputs = List[LogisticSimulatorOutput]()

        for i in range(0, sessions):
            sessId = network.SessionStart()

            inputs = List[RlmIOWithValue]()

            inputXObj = next((x for x in network.Inputs if x.Name == "X"), None)
            inputX = RlmIOWithValue(inputXObj, "1")

            inputs.Add(inputX)

            cycle = RlmCycle()
            cycleOutcome = cycle.RunCycle(network, sessId, inputs, True)

            simOutputs = cycleOutcome.CycleOutput.Outputs

            outputs = List[LogisticSimulatorOutput]()

            for item in simOutputs:

                simout = LogisticSimulatorOutput()
                simout.Name = item.Name
                simout.Value = int(item.Value)

                outputs.Add(simout)

            simulationDelay = 50 #having a delay of 50ms in beer game simulation
            simulator.ResetSimulationOutput()
            simulator.start(outputs, simulationDelay, customerOrders)

            network.ScoreCycle(cycleOutcome.CycleOutput.CycleID, 0)
                
            totalCosts = simulator.SumAllCosts() #get the total cost of the beer game simulation

            network.SessionEnd(totalCosts)

            score = '${:,.2f}'.format(abs(totalCosts))

            print("Session #"+ str(i + 1) + "\t Score: " + str(score))

            if i == (sessions -1):
                predictedLogisticOutputs = outputs

        network.TrainingDone()

        #Printing the predicted results
        print("\nPredicted outputs:\n")
        resultText = ""

        for item in predictedLogisticOutputs:
            resultText += "\n" + item.Name + ": " + str(item.Value)

        print(resultText + "\n")

        return

    def getInput(self,label, defVal = 0):

        validInput = False
        retVal = 1

        while not validInput:

            print(label)
            rawInput = ""

            try:
                rawInput = int(input())
                validInput = True
                retVal = rawInput
            except ValueError:
                validInput = False

            if not validInput and rawInput == "":
                validInput = True
                retVal = defVal
            elif validInput and retVal < 0:
                print("Cannot be less than zero, try again...")
                validInput = False
            elif not validInput:
                print("Invalid input try again...")

        return retVal



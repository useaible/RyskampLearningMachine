import time
import uuid

from RLM import *
from RLM.Models import *
from MazeGameLib import *
from System.Collections.Generic import *

class RLMTraveler(object):
    
    networkName = "maze"
    maze = None
    network = None
    learn = True
    gameTimeout = 5 #seconds

    def __init__(self, maze, numSessions, startRandomness, endRandomness):

        dbName = "RLM_maze" + maze.Name + "_" + str(uuid.uuid1()).replace('-', '');
        self.network = RlmNetwork(dbName);
        self.maze = maze

        #load network
        print("Initializing network...")
        if not self.network.LoadNetwork(self.networkName):

            print("\nNetwork not found...")
            print("Creating new network...\n\n")

            inputs = List[RlmIO]()
            outputs = List[RlmIO]()
            
            inputX = RlmIO()
            inputY = RlmIO()

            outputDirection = RlmIO()

            #input settings
            inputX.Name = "x"
            inputX.DotNetType = "System.Int32"
            inputX.Min = 0
            inputX.Max = maze.Width - 1
            inputX.Type = 0

            inputY.Name = "y"
            inputY.DotNetType = "System.Int32"
            inputY.Min = 0
            inputY.Max = maze.Height - 1
            inputY.Type = 0

            inputs.Add(inputX)
            inputs.Add(inputY)

            #output settings 
            outputDirection.Name = "direction"
            outputDirection.DotNetType = "System.Int32"
            outputDirection.Min = 0
            outputDirection.Max = 3

            outputs.Add(outputDirection)

            #save and create network
            self.network.NewNetwork(self.networkName, inputs, outputs)
            print("\n\nNew network created...")

        self.network.NumSessions = numSessions
        self.network.StartRandomness = startRandomness
        self.network.EndRandomness = endRandomness

        print("Training started...\n\n")

        return 
    
    def Travel(self):

        game = MazeGame()
        game.InitGame(self.maze)

        outcome = MazeCycleOutcome()

        #start session
        sessionId = self.network.SessionStart()

        moves = 0
        timeout = time.time() + self.gameTimeout
        
        while not outcome.GameOver and time.time() < timeout :

            moves = moves + 1

            #set up input with values
            inputs = List[RlmIOWithValue]()

            inputXObj = next((x for x in self.network.Inputs if x.Name == "x"), None)
            inputYObj = next((x for x in self.network.Inputs if x.Name == "y"), None)

            inputX = RlmIOWithValue(inputXObj, str(game.traveler.location.X))
            inputY = RlmIOWithValue(inputYObj, str(game.traveler.location.Y))

            inputs.Add(inputX)
            inputs.Add(inputY)

            #do training cycle
            cycle = RlmCycle()
            cycleResult = cycle.RunCycle(self.network, sessionId, inputs, self.learn)

            #get AI output
            output = next((x for x in cycleResult.CycleOutput.Outputs if x.Name == "direction"), None)
            direction = int(output.Value)

            #make the move in game and score it
            outcome = game.CycleMaze(direction)
            cycleScore = self.ScoreMove(outcome)

            #save the cycle score
            self.network.ScoreCycle(cycleResult.CycleOutput.CycleID, cycleScore)
                        
        #compute session core and end session
        sessionScore = self.CalculateGameScore(moves);
        self.network.SessionEnd(sessionScore)

        self.network.TrainingDone()

        print (sessionScore)

        return

    def ScoreMove(self, outcome):

        score = 0;

        if outcome.GameOver:
            score = 100 * 2
        elif not outcome.BumpedIntoWall:
            score = 100
            
        return score

    def CalculateGameScore(self, moves):

        score = (20000 + (self.maze.PerfectGameMovesCount * 1000)) - (moves * 1000);

        return score


    



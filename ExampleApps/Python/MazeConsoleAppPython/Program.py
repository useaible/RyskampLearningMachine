import clr

from MazeGameLib import *
from RLMTraveler import *
from RLM.Models.Exceptions import *

def getInput(label, defVal = 0):

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

def main():

    try:
        print("Maze")
        size = getInput("Enter maze size [must be 8 or greater]: ", 8)

        if size >= 8:
            #generate maze
            gen = MazeGenerator()
            gen.Generate(size, size)
            gen.Solve()

            maze = MazeInfo()
            maze.GoalPosition = gen.GoalLocation
            maze.StartingPosition = gen.StartingPosition
            maze.Grid = gen.TheMazeGrid
            maze.Name = str(size) + "x" + str(size)
            maze.PerfectGameMovesCount = gen.PerfectGameMovesCount
            maze.Height = size
            maze.Width = size

            print("Perfect move count for this maze would be " + str(gen.PerfectGameMovesCount))
            print("\n")

            sessions = getInput("Number of sessions [default 50]: ", 50)
            startRandomness = getInput("Start randomness [default 100]: ", 100)
            endRandomness = getInput("End randomness [default 0]: ", 0)
            randomnessThrough = getInput("Number of sessions to enforce randomness [default 1]: ", 1)
        

            print("\n")

            traveler = RLMTraveler(maze, randomnessThrough, startRandomness,endRandomness)

            traveler.learn = True
            for i in range(0, randomnessThrough):
                traveler.Travel()

            traveler.learn = False
            for i in range(0, sessions - randomnessThrough):
                traveler.Travel()
    except Exception as err:
        if type(err.InnerException) == RlmDefaultConnectionStringException:
            print("Error: " + err.InnerException.Message)
        else:
            print("Error: " + err)

    return

if __name__ == "__main__":
    main()
import sys
import clr
import RLMPilot

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

    print("\n\nRLM network settings:\n")
    sessions = getInput("Enter number of sessions [default 100]:", 100)
    startRand = getInput("Enter start randomness [default 30]:", 30)
    endRand = getInput("Enter end randomness [default 0]:", 0)
    maxLinearBracket = getInput("Max linear bracket value [default 15]:", 15)
    minLinearBracket = getInput("Min linear bracket value [default 3]:", 3)
    print("\n")

    pilot = RLMPilot.Pilot(True, sessions, startRand, endRand, maxLinearBracket, minLinearBracket)

    for i in range(0, sessions):
        pilot.StartSimulation(i, True)

    pilot.trainingDone()

    return

if __name__ == "__main__":
    main()
           
        
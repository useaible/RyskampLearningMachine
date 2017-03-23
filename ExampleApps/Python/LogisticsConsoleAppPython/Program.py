import sys
import clr
import RLMLogistics

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

    logisticPlayer = RLMLogistics.RLMPlayer()
    logisticPlayer.LogisticTrain()

    return

if __name__ == "__main__":
    main()
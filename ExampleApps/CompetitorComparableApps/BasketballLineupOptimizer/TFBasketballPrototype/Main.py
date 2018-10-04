print ("Loading assemblies, please wait.")

import os
import sys
if 'win' in sys.platform:
    import pythoncom
    pythoncom.CoInitialize()

    sys.path.append(os.path.dirname(__file__))

import clr
from RLM import *
from RLM.Models.Optimizer import *
from RLM.Enums import *
from RlmBasketballPrototype import *
from TFBasketballPrototype import *

if __name__ == "__main__":

    opt = BasketballOptimizer.GetOptimizer()
    tfBasketball = TFBasketballPrototype(opt)
    tfBasketball.train()
    BasketballOptimizer.PrintResults(opt);    
    tfBasketball.Save.WriteList(BasketballOptimizer.Players, "Position,Player")
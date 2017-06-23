print ("Loading assemblies, please wait.")

import os
import sys
if 'win' in sys.platform:
    import pythoncom
    pythoncom.CoInitialize()

    sys.path.append(os.path.dirname(__file__))

import clr
clr.AddReference("System.Xml")
clr.AddReference("PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")
clr.AddReference("PresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")

binPath = 'CSharp\\RetailPoC\\RetailPoC\\bin\\Debug\\'
mahApps = os.path.abspath(os.path.join('..', '..', '..', binPath + 'MahApps.Metro.dll'))
clr.AddReference(mahApps)

#from System import Nullable, Boolean
#from System.IO import StringReader
#from System.Xml import XmlReader
#from System.Windows.Markup import XamlReader, XamlWriter
#from System.Windows import Window, Application, Thickness, MessageBox, FontStyles
#from System.Windows.Controls import *

from System.Windows import Application
from RetailPoC import *
from RetailPoC.Models import *
import random
from threading import Thread

from PlanogramOptTensorflow import *
from ConfigFileManager import *

def tensorflowLogic(items, sim):
    opt = PlanogramOptTensorflow(items, sim)
    opt.train(0.001, main.UpdateTensorflowResults, main.UpdateTensorflowStatus, main.AddLogDataTensorflow)

def start(items, sim):    
    t = Thread(target=tensorflowLogic, args=(items, sim,))
    t.start()

if __name__ == "__main__":
    mgr = ConfigFileManager()
    if (mgr.configure()):        
        main = MainWindow(True)
        main.OnSimulationStart += start
        helpPath = os.environ['PYTHONPATH'].split(os.pathsep)[0]
        main.HelpPath = helpPath
        #Application().Run(main)
        App().Run(main)
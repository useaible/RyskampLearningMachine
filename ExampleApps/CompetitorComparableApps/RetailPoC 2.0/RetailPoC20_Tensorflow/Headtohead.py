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

#comment this gino
binPath = 'CSharp\\RetailPoC 2.0\\RetailPoC20\\bin\\Debug\\'
mahApps = os.path.abspath(os.path.join('..', '..', '..', binPath + 'MahApps.Metro.dll'))
clr.AddReference(mahApps)

#currDir = os.path.dirname(sys.argv[0])
currDir = os.path.realpath(__file__).replace('Headtohead.py', '')

#uncomment this gino
#clr.AddReference(currDir + '\\CSharp\\RetailPoC.exe')
#clr.AddReference(currDir + '\\CSharp\\MahApps.Metro.dll')

#from System import Nullable, Boolean
#from System.IO import StringReader
#from System.Xml import XmlReader
#from System.Windows.Markup import XamlReader, XamlWriter
#from System.Windows import Window, Application, Thickness, MessageBox, FontStyles
#from System.Windows.Controls import *

from System.Windows import Application
from System.Threading import CancellationToken
from RetailPoC20 import *
from RetailPoC20.Models import *
import random
from threading import Thread

from PlanogramOptTensorflow import *
from ConfigFileManager import *

def tensorflowLogic(items, sim, token):
    opt = PlanogramOptTensorflow(items, sim, token)
    opt.train(0.001, main.UpdateTensorflowResults, main.UpdateTensorflowStatus, main.AddLogDataTensorflow, main.TFSettings)

def start(items, sim, token):    
    t = Thread(target=tensorflowLogic, args=(items, sim, token,))
    t.start()

if __name__ == "__main__":
    mgr = ConfigFileManager()
    if (mgr.configure()):        
        main = MainWindow(True, False)
        main.OnSimulationStart += start
        #helpPath = os.environ['PYTHONPATH'].split(os.pathsep)[0]
        #helpPath = currDir + "\\Help\\Retail POC How to.docx"
        #main.HelpPath = helpPath
        #Application().Run(main)
        App().Run(main)
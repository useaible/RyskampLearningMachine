import tensorflow as tf
import tensorflow.contrib.slim as slim
import numpy as np

from RetailPoC20 import *
from RetailPoC20.Models import *
from System import TimeSpan
from System.Threading import CancellationToken
from PoCTools.Settings import SimulationSettings
from PoCTools.Settings import SimulationType


import time
import datetime

class PlanogramOptTensorflow():

    def __init__(self, items, simSettings, token):
        self.items = items
        self.simSettings = simSettings
        self.cancelToken = token   
        self.numSlots = simSettings.NumShelves * simSettings.NumSlots 
        self.numOutputs = len(self.items)
        self.simType = self.simSettings.SimType
        self.totalSeconds = (3600 * self.simSettings.Hours + time.time()) if (self.simSettings.SimType == 1) else 0       
        self.randomReset = 100
                    
    def normalize(self, originalStart, originalEnd, newStart, newEnd, value):
        scale = (newEnd - newStart) / (originalEnd - originalStart);
        return (newStart + ((value - originalStart) * scale));

    def endTraining(self, criteria):
        if (self.cancelToken.IsCancellationRequested):
            return True;

        if (self.simType == 0):
            if (criteria >= self.simSettings.Sessions):
                return True
        elif (self.simType == 1):
            if (criteria >= self.totalSeconds):
                return True
        else:
            if (criteria >= SimulationSettings.NUM_SCORE_HITS):
                return True
        return False

    def train(self, lr, ui_results_callback, ui_status_callback, ui_logger, tf_settings):        

        lr = 0.001

        NUM_HIDDEN_LAYERS = 3
        HIDDEN_LAYER_NEURONS = 500
        TF_ACTIVATION = tf.nn.softmax
        TF_OPTIMIZER = tf.train.AdamOptimizer(learning_rate=lr)

        n_nodes_hl1 = HIDDEN_LAYER_NEURONS
        n_nodes_hl2 = HIDDEN_LAYER_NEURONS
        n_nodes_hl3 = HIDDEN_LAYER_NEURONS

        ui_status_callback("Initializing...")          

        tf.reset_default_graph() #Clear the Tensorflow graph.      

        #These lines established the feed-forward part of the network. The agent takes a state and produces an action.
        self.slot_in = tf.placeholder(shape=[1],dtype=tf.int32)
        slot_in_OH = slim.one_hot_encoding(self.slot_in, self.numSlots)

        hidden_1_layer = {'weights':tf.Variable(tf.random_normal([self.numSlots, n_nodes_hl1]), name="hl_w_1"),
                      'biases':tf.Variable(tf.ones([n_nodes_hl1]), name="hl_b_1")}

        hidden_2_layer = {'weights':tf.Variable(tf.random_normal([n_nodes_hl1, n_nodes_hl2]), name="hl_w_2"),
                          'biases':tf.Variable(tf.ones([n_nodes_hl2]), name="hl_b_2")}

        hidden_3_layer = {'weights':tf.Variable(tf.random_normal([n_nodes_hl3, self.numSlots]), name="hl_w_3"),
                          'biases':tf.Variable(tf.ones([self.numSlots]), name="hl_b_3")}

        output_layer = {'weights':tf.Variable(tf.random_normal([self.numSlots, self.numOutputs]), name="out_w"),
                        'biases':tf.Variable(tf.ones([self.numSlots, self.numOutputs]), name="out_b"),}


        l1 = tf.add(tf.matmul(slot_in_OH,hidden_1_layer['weights']), hidden_1_layer['biases'])
        l1 = TF_ACTIVATION(l1)

        l2 = tf.add(tf.matmul(l1,hidden_2_layer['weights']), hidden_2_layer['biases'])
        l2 = TF_ACTIVATION(l2)

        l3 = tf.add(tf.matmul(l2,hidden_3_layer['weights']), hidden_3_layer['biases'])
        l3 = TF_ACTIVATION(l3)

        output = tf.matmul(l3,output_layer['weights']) + output_layer['biases']
        
        #self.output = tf.reshape(output,[-1])

        #The next six lines establish the training proceedure. We feed the reward and chosen action into the network
        #to compute the loss, and use it to update the network.
        self.reward_holder = tf.placeholder(shape=[1],dtype=tf.float32)
        self.action_holder = tf.placeholder(shape=[1],dtype=tf.int32)        
        self.chosen_action = tf.argmax(output[self.slot_in[0]], 0)
        self.responsible_weight = tf.slice(output, [self.slot_in[0],self.action_holder[0]],[1,1])
        self.loss = -(tf.log(self.responsible_weight[0][0])*self.reward_holder)
        optimizer = TF_OPTIMIZER#tf.train.AdamOptimizer(learning_rate=lr)
        self.update = optimizer.minimize(self.loss)     
        weights = tf.trainable_variables()[6] #The weights we will evaluate to look into the network.
 
        #get tensorflow current settings and pass to UI
        optimizerName = optimizer.__class__.__name__
        activationName = TF_ACTIVATION.__name__
        tf_settings(optimizerName, activationName, NUM_HIDDEN_LAYERS, HIDDEN_LAYER_NEURONS, lr)
        
        print(tf.trainable_variables())
        totalMetricScoreArr = []
        avgMetricScoreLastTen = []
        flag = 0
        startTime = time.time()
        sessionCounter = 0                
        init = tf.initialize_all_variables()
        randomness = 1

        ui_status_callback("Training...")

        trainingNum = -1
        predictNum = 0
        doPredict = False
        interval = 1 / (self.randomReset - SimulationSettings.NUM_SCORE_HITS)

        # Launch the tensorflow graph
        with tf.Session() as sess:
            sess.run(init)
            highestPossibleSessionScore = self.simSettings.ItemMetricMax * self.numSlots
            lowestPossibleSessionScore = self.simSettings.ItemMetricMin * self.numSlots

            matrix = {};
            while (not self.endTraining(flag)):     
                totalMetricScore = 0
                results = PlanogramOptResults()
                sessionStartTime = time.time()
                actionsPerSlot = {}
                hasDupItems = False

                if (not doPredict):
                    trainingNum += 1
                    if (trainingNum != 0 and trainingNum % (self.randomReset - SimulationSettings.NUM_SCORE_HITS) == 0):
                        doPredict = True
                    else:
                        randomness -= interval;
                
                if (doPredict):
                    predictNum += 1
                    randomness = 0
                    if (predictNum > SimulationSettings.NUM_SCORE_HITS):
                        doPredict = False
                        predictNum = 0
                        randomness = 1
                        trainingNum = 0

                #Reset facings list
                facingsList = {}
                for it in self.items:
                    facingsList[it.ID] = 0
                
                #Train                
                hasDupItems = False
                action = 0
                for sl in range(self.numSlots):                   
                    #get an item randomly
                    if (np.random.rand(1) <= randomness):
                        action = np.random.randint(self.numOutputs)
                    else:
                        action = sess.run(self.chosen_action, feed_dict={self.slot_in:[sl]})
                                                
                    actionsPerSlot[sl] = action

                    #Get item reference and calculate metric score
                    item = self.items[action]
                    metricScore = PlanogramOptimizer.GetCalculatedWeightedMetrics(item, self.simSettings)

                    dupItemVal = facingsList[item.ID] #Get item number of facings

                    if(not hasDupItems and dupItemVal < 10):
                        reward = 1
                        dupItemVal = dupItemVal + 1 #Increment item number of facings
                        facingsList[item.ID] = dupItemVal #Update item number of facings in the list
                        totalMetricScore += metricScore
                    else:
                        totalMetricScore = 0
                        reward = -1
                        hasDupItems = True

                #Update network
                #print (totalMetricScore)
                if (hasDupItems):
                    totalMetricScore = 0
                    sessionScoreNormalized = -1
                else:
                    sessionScoreNormalized = self.normalize(lowestPossibleSessionScore, highestPossibleSessionScore, -1, 1, totalMetricScore)

                if (not doPredict or (doPredict and predictNum <= 1)):
                    for sl in range(self.numSlots):
                        feed_dict={self.reward_holder:[sessionScoreNormalized],self.action_holder:[actionsPerSlot[sl]],self.slot_in:[sl]}
                        loss,update,matrix = sess.run([self.loss,self.update,weights], feed_dict=feed_dict)
                    
                #Reset facings list
                facingsList = {}
                for it in self.items:
                    facingsList[it.ID] = 0

                totalMetricScore = 0
                #Extract updated results from matrix
                shelfItems = []                
                for slotIndex in range(self.numSlots):

                    slotArr = matrix[slotIndex]
                    itemIndex = np.argmax(slotArr)
                    item = self.items[itemIndex]   
                    shelfItems.append(item)
                    #actionsPerSlot[slotIndex] = itemIndex

                    dupItemVal = facingsList[item.ID] #Get item number of facings

                    if(dupItemVal < 10):
                        dupItemVal = dupItemVal + 1 #Increment item number of facings
                        facingsList[item.ID] = dupItemVal #Update item number of facings in the list
                    else:
                        hasDupItems = True

                    if ((slotIndex+1) % 24 == 0):
                        shelf = Shelf()
                        for ii in range(len(shelfItems)):                                 
                            metricScore = PlanogramOptimizer.GetCalculatedWeightedMetrics(shelfItems[ii], self.simSettings)            
                            totalMetricScore += metricScore
                            shelf.Add(shelfItems[ii], metricScore)
                        shelfItems = []
                        results.Shelves.Add(shelf)   
                                         
                if (hasDupItems):
                    totalMetricScore = 0

                sessionCounter+=1
                currentTime = time.time()

                #Setup planogram results and stats
                totalMetricScoreArr.append(totalMetricScore)
                avgMetricScoreLastTen.append(totalMetricScore)
                if (len(avgMetricScoreLastTen) > 10):
                    avgMetricScoreLastTen.pop(0)

                results.Score = totalMetricScore
                results.AvgScore = np.average(totalMetricScoreArr)
                results.AvgLastTen = np.average(avgMetricScoreLastTen)
                results.MinScore = np.amin(totalMetricScoreArr)
                results.MaxScore = np.amax(totalMetricScoreArr)
                results.TimeElapsed = TimeSpan.FromSeconds(float(currentTime - startTime))
                results.CurrentSession = sessionCounter
                
                #Update the training flag
                if (self.simType == 0):
                    flag = sessionCounter
                elif (self.simType == 1):
                    flag = currentTime
                else:
                    if (totalMetricScore >= self.simSettings.Score):
                        flag+=1
                        results.NumScoreHits = flag
                    else:
                        flag = 0

                ui_logger(sessionCounter, totalMetricScore, float(currentTime-sessionStartTime))

                #Pass results to ui
                if (self.simSettings.EnableSimDisplay):
                    results.MetricMin = self.simSettings.ItemMetricMin
                    results.MetricMax = self.simSettings.ItemMetricMax
                    results.CalculateItemColorIntensity()               
                ui_results_callback(results, self.simSettings.EnableSimDisplay)                

            ui_results_callback(results, True) #To ensure final output is displayed just in case the enableSimDisplay is turned off
            ui_status_callback("Done", True)
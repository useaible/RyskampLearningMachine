import tensorflow as tf
import tensorflow.contrib.slim as slim
import numpy as np

from RetailPoC import *
from RetailPoC.Models import *
from System import TimeSpan

import time
import datetime

class PlanogramOptTensorflow():

    def __init__(self, items, simSettings):
        self.items = items
        self.simSettings = simSettings        
        self.numSlots = simSettings.NumShelves * simSettings.NumSlots 
        self.numOutputs = len(self.items)
        self.simType = self.simSettings.SimType
        self.totalSeconds = (3600 * self.simSettings.Hours + time.time()) if (self.simSettings.SimType == 1) else 0       
                    
    def normalize(self, originalStart, originalEnd, newStart, newEnd, value):
        scale = (newEnd - newStart) / (originalEnd - originalStart);
        return (newStart + ((value - originalStart) * scale));

    def endTraining(self, criteria):
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

    def train(self, lr, ui_results_callback, ui_status_callback, ui_logger):
        ui_status_callback("Initializing...")          

        tf.reset_default_graph() #Clear the Tensorflow graph.      

        #These lines established the feed-forward part of the network. The agent takes a state and produces an action.
        self.slot_in = tf.placeholder(shape=[1],dtype=tf.int32)
        slot_in_OH = slim.one_hot_encoding(self.slot_in, self.numSlots)
        output = slim.fully_connected([slot_in_OH],self.numOutputs,\
            biases_initializer=None,activation_fn=tf.nn.sigmoid,weights_initializer=tf.zeros_initializer())
        self.output = tf.reshape(output,[-1])
        self.chosen_action = tf.argmax(self.output,0)

        #The next six lines establish the training proceedure. We feed the reward and chosen action into the network
        #to compute the loss, and use it to update the network.
        self.reward_holder = tf.placeholder(shape=[1],dtype=tf.float32)
        self.action_holder = tf.placeholder(shape=[1],dtype=tf.int32)
        self.responsible_weight = tf.slice(self.output,self.action_holder,[1])
        self.loss = -(tf.log(self.responsible_weight)*self.reward_holder)
        optimizer = tf.train.GradientDescentOptimizer(learning_rate=lr)
        self.update = optimizer.minimize(self.loss)        
        weights = tf.trainable_variables()[0] #The weights we will evaluate to look into the network.
 
        totalMetricScoreArr = []
        avgMetricScoreLastTen = []
        flag = 0
        startTime = time.time()
        sessionCounter = 0                
        init = tf.initialize_all_variables()

        ui_status_callback("Training...")

        # Launch the tensorflow graph
        with tf.Session() as sess:
            sess.run(init)
            highestPossibleSessionScore = self.simSettings.ItemMetricMax * self.numSlots
            lowestPossibleSessionScore = self.simSettings.ItemMetricMin * self.numSlots

            while (not self.endTraining(flag)):     
                totalMetricScore = 0
                results = PlanogramOptResults()
                sessionStartTime = time.time()

                actionsPerSlot = {}

                #Reset facings list
                facingsList = {}
                for it in self.items:
                    facingsList[it.ID] = 0
                
                #Train                
                for sl in range(self.numSlots):
                    reward = 0
                    while(reward != 1):
                        #get an item randomly
                        action = np.random.randint(self.numOutputs)

                        #Get item reference and calculate metric score
                        item = self.items[action]
                        metricScore = PlanogramOptimizer.GetCalculatedWeightedMetrics(item, self.simSettings)

                        dupItemVal = facingsList[item.ID] #Get item number of facings

                        if(dupItemVal < 10):
                            reward = 1
                            dupItemVal = dupItemVal + 1 #Increment item number of facings
                            facingsList[item.ID] = dupItemVal #Update item number of facings in the list
                            actionsPerSlot[sl] = action
                            totalMetricScore += metricScore
                        else:
                            reward = -1
                                            
                #Update network
                #print (totalMetricScore)
                sessionScoreNormalized = self.normalize(lowestPossibleSessionScore, highestPossibleSessionScore, 0, 1, totalMetricScore)
                for sl in range(self.numSlots):
                    feed_dict={self.reward_holder:[sessionScoreNormalized],self.action_holder:[actionsPerSlot[sl]],self.slot_in:[sl]}
                    _,matrix = sess.run([self.update,weights], feed_dict=feed_dict)
                
                totalMetricScore = 0
                #Extract updated results from matrix
                shelfItems = []                
                for slotIndex in range(self.numSlots):
                    slotArr = matrix[slotIndex]
                    itemIndex = np.argmax(slotArr)
                    item = self.items[itemIndex]   
                    shelfItems.append(item)
                    if ((slotIndex+1) % 24 == 0):
                        shelf = Shelf()
                        for ii in range(len(shelfItems)):                                 
                            metricScore = PlanogramOptimizer.GetCalculatedWeightedMetrics(shelfItems[ii], self.simSettings)            
                            totalMetricScore += metricScore
                            shelf.Add(shelfItems[ii], metricScore)
                        shelfItems = []
                        results.Shelves.Add(shelf)                    

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
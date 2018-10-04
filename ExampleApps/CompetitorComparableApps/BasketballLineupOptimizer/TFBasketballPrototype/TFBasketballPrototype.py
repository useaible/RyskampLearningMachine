import tensorflow as tf
import tensorflow.contrib.slim as slim
import numpy as np
import SaveToCSV

import clr
from RLM import *
from RLM.Models.Optimizer import *
from System import *
from System import Object
from System.Linq import Expressions
from System.Collections.Generic import *

class TFBasketballPrototype():

    def __init__(self, basketballOpt):
        self.basketballOpt = basketballOpt
        self.lowestSessionScore = -1
        self.SessionData = List[String]()
        self.Save = None
        self.BiggestSessionScore = 0
        self.CycleScores = List[String]()

    def train(self):

        print('Training started...')

        #SETTINGS
        target_score = 400
        learning_rate = 5  
        randomness = 0.5
        randomReset = self.basketballOpt.Settings.NumOfSessionsReset
        numScoreHits = self.basketballOpt.Settings.NumScoreHits
        simType = self.basketballOpt.Settings.SimulationType #[Sessions, Score, Time]

        NUM_HIDDEN_LAYERS = 3
        HIDDEN_LAYER_NEURONS = 100
        TF_ACTIVATION = tf.nn.softmax
        TF_OPTIMIZER = tf.train.GradientDescentOptimizer(learning_rate)

        self.numPositions = int(self.basketballOpt.Resources["Position"].Max + 1)
        self.numOutputs = self.basketballOpt.Resources["Player"].DataObjDictionary.Count

        n_nodes_hl1 = HIDDEN_LAYER_NEURONS
        n_nodes_hl2 = HIDDEN_LAYER_NEURONS
        n_nodes_hl3 = HIDDEN_LAYER_NEURONS


        tf.reset_default_graph() #Clear the Tensorflow graph.    

        self.position_in = tf.placeholder(shape=[1],dtype=tf.int32)
        position_in_OH = slim.one_hot_encoding(self.position_in, self.numPositions)

        hidden_1_layer = {'weights':tf.Variable(tf.random_normal([self.numPositions, n_nodes_hl1]), name="hl_w_1"),
                          'biases':tf.Variable(tf.random_normal([n_nodes_hl1]), name="hl_b_1")}

        hidden_2_layer = {'weights':tf.Variable(tf.random_normal([n_nodes_hl1, n_nodes_hl2]), name="hl_w_2"), 
                          'biases':tf.Variable(tf.random_normal([n_nodes_hl2]), name="hl_b_2")}

        hidden_3_layer = {'weights':tf.Variable(tf.random_normal([n_nodes_hl3, self.numPositions]), name="hl_w_3"),
                          'biases':tf.Variable(tf.random_normal([self.numPositions]), name="hl_b_3")}

        output_layer = {'weights':tf.Variable(tf.random_normal([self.numPositions, self.numOutputs]), name="out_w"),
                        'biases':tf.Variable(tf.random_normal([self.numPositions, self.numOutputs]), name="out_b")}

        l1 = tf.add(tf.matmul(position_in_OH, hidden_1_layer['weights']), hidden_1_layer['biases'])
        l1 = TF_ACTIVATION(l1)

        l2 = tf.add(tf.matmul(l1, hidden_2_layer['weights']), hidden_2_layer['biases'])
        l2 = TF_ACTIVATION(l2)

        l3 = tf.add(tf.matmul(l2, hidden_3_layer['weights']), hidden_3_layer['biases'])
        l3 = TF_ACTIVATION(l3)

        output = tf.matmul(l3, output_layer['weights']) + output_layer['biases']
        output = TF_ACTIVATION(output)

        self.reward_holder = tf.placeholder(shape=[1],dtype=tf.float32)
        self.reward_holder_mult = tf.placeholder(shape=[1],dtype=tf.float32)
        self.action_holder = tf.placeholder(shape=[1],dtype=tf.int32)        
        self.responsible_weight = tf.slice(output, [self.position_in[0],self.action_holder[0]],[1,1])
        self.loss =  -(tf.log(self.responsible_weight[0][0])*self.reward_holder[0])
        optimizer = TF_OPTIMIZER
        self.update = optimizer.minimize(self.loss)     
        weights = tf.trainable_variables()[6] #The weights we will evaluate to look into the network.

        self.chosen_action = tf.argmax(output[self.position_in[0]], 0)

        trainingNum = -1
        predictNum = 0
        doPredict = False
        currRandomness = randomness
        interval = currRandomness / (randomReset - numScoreHits)
        init = tf.initialize_all_variables()

        #start: CSV Logger
        save = SaveToCSV.SaveToCSV("TFData")
        self.Save = save
        save.WriteLine("Learning Rate,Number of Hidden Layers,Hidden Layer Neurons,Activation Function,Randomness")
        tfSettings = str(learning_rate) + "," + str(NUM_HIDDEN_LAYERS) + "," + str(HIDDEN_LAYER_NEURONS) + ",None," + str(randomness)
        save.WriteLine(tfSettings)
        #end: CSV Logger

        hours = 0
        if simType == 1:
            hours = self.basketballOpt.Settings.Time.TotalHours
        endsOn = DateTime.Now.AddHours(hours)

        watchStart = DateTime.Now #time started training
        with tf.Session() as sess:

            sess.run(init)
            actionPerSlot = {}
            matrix = None

            #train
            session_number = 0
            sessionScore = 0
            currNumScoreHits = 0
            while (currNumScoreHits < numScoreHits):
                session_number += 1
                if (not doPredict):
                    trainingNum += 1
                    if (trainingNum != 0 and trainingNum % (randomReset - numScoreHits) == 0):
                        doPredict = True
                    else:
                        currRandomness -= interval;
                
                if (doPredict):
                    predictNum += 1
                    currRandomness = 0
                    if (predictNum > numScoreHits):
                        doPredict = False
                        predictNum = 0
                        currRandomness = randomness
                        trainingNum = 0

                sessionScore = self.run_once(sess, session_number, currRandomness, actionPerSlot, matrix, weights, True)
                if (sessionScore > target_score):
                    currNumScoreHits += 1
                else:
                    currNumScoreHits = 0

        watchEnd = DateTime.Now.Subtract(watchStart) #time ended training
        elapsed = TimeSpan(watchEnd.Ticks).TotalHours
        print("Elapsed: ", elapsed)

        #start: CSV Logger
        save.WriteList(self.SessionData, "Session,SessionScore")
        save.WriteLine("Elapsed:," + str(elapsed))
        save.WriteList(self.CycleScores, "Position, Score")
        #end: CSV Logger
                
    def run_once(self, sess, session_num, randomness, actionPerSlot, matrix, weights, train):
        
        sessionOutputs = List[Object]()
        self.CycleScores.Clear()
        self.basketballOpt.TrainingVariables["SessionScore"].Value = 0
        hasRandom = False

        for sl in range(1, self.numPositions):

            self.basketballOpt.CycleInputs["Position"] = sl

            if train == True:
                if (np.random.rand(1) <= randomness):
                    action = np.random.randint(self.numOutputs)
                    hasRandom = True
                else:
                    action = sess.run(self.chosen_action, feed_dict={self.position_in:[sl]})
            else:
                action = sess.run(self.chosen_action, feed_dict={self.position_in:[sl]})

            actionPerSlot[sl] = action

            self.basketballOpt.CycleOutputs["Player"] = float(action)
            
            sessionOutputs.Add(float(action));
            self.basketballOpt.SessionOutputs["Player"] = sessionOutputs;

            compiler = RlmFormulaCompiler(self.basketballOpt)
            b = float(compiler.Parse(self.basketballOpt.CyclePhase))
            self.CycleScores.Add(str(b));
            self.basketballOpt.TrainingVariables["CycleScore"].Value = 0

           
        multiplier = 1
        sessionScore = float(compiler.Parse(self.basketballOpt.SessionPhase))
        sessionScoreNormalized = self.normalize(0, 500, 0, 1, sessionScore)

        prevScore = 0
        currScore = 0
        matrix = None
        for sl in range(1, self.numPositions):
            feed_dict={self.reward_holder:[sessionScoreNormalized], self.reward_holder_mult:[multiplier], self.action_holder:[actionPerSlot[sl]],self.position_in:[sl]}
            if (matrix is not None):
                prevScore = matrix[sl][actionPerSlot[sl]]
            _,matrix = sess.run([self.update,weights], feed_dict=feed_dict)
            currScore = matrix[sl][actionPerSlot[sl]]
        
        if sessionScore > self.BiggestSessionScore:
            self.BiggestSessionScore = sessionScore
        print("#" , session_num , " Score: " , str(round(sessionScore, 2)) , "         Biggest Session Score: " , str(round(self.BiggestSessionScore,2)))
        
        #saved for logging
        self.SessionData.Add(str(sessionScore))

        return sessionScore

    def normalize(self, originalStart, originalEnd, newStart, newEnd, value):
        scale = (newEnd - newStart) / (originalEnd - originalStart);
        return (newStart + ((value - originalStart) * scale));




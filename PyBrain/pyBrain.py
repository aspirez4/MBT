# Import PyBrain components
from pybrain.structure 			 import LinearLayer, FullConnection, SigmoidLayer, LSTMLayer, SoftmaxLayer, RecurrentNetwork
from pybrain.datasets            import ClassificationDataSet, SupervisedDataSet
from pybrain.supervised.trainers import BackpropTrainer
from pybrain.utilities           import percentError
from pybrain.tools.shortcuts     import buildNetwork
from math 						 import sqrt

# Import Graphical output
from pylab import ion, ioff, figure, draw, contourf, clf, show, hold, plot
from scipy import diag, arange, meshgrid, where
from numpy.random import multivariate_normal

class NN:
    elmanNN = None
    nInputCount = 0
    nHiddenCount = 0
    nOutputCount = 0
    
    
    
    def __init__( self, softmax = True, inCount = 2, hidCount = 100, outCount = 1 ):
        
        # Initialize Variables
        self.nInputCount = inCount
        self.nHiddenCount = hidCount
        self.nOutputCount = outCount
        
        # Set the net and layers
        self.elmanNN = RecurrentNetwork()
        self.elmanNN.addInputModule(LinearLayer(self.nInputCount, name='inputLayer'))
        
        if (softmax):
            self.elmanNN.addModule(SoftmaxLayer(self.nHiddenCount, name='hiddenLayer'))
            self.elmanNN.addOutputModule(LinearLayer(self.nOutputCount, name='outputLayer'))
        else:
            self.elmanNN.addModule(LSTMLayer(self.nHiddenCount, peepholes=False, name='hiddenLayer'))
            self.elmanNN.addOutputModule(LSTMLayer(self.nOutputCount, peepholes=False, name='outputLayer'))
        
        # Set the connections
        self.elmanNN.addConnection(FullConnection(self.elmanNN['inputLayer'], self.elmanNN['hiddenLayer'], name='inputToHidden'))
        self.elmanNN.addConnection(FullConnection(self.elmanNN['hiddenLayer'], self.elmanNN['outputLayer'], name='hiddenToOutput'))
        self.elmanNN.addRecurrentConnection(FullConnection(self.elmanNN['hiddenLayer'], self.elmanNN['hiddenLayer'], name='hiddenToHidden'))
        
        # Build the net
        # self.elmanNN = buildNetwork( self.nInputCount, self.nHiddenCount , self.nOutputCount , recurrent=True , hiddenclass=SoftmaxLayer , outclass=SoftmaxLayer )
        self.elmanNN.sortModules()
        
        return;
        
    def Train( self, dataSet , epochs = 500, elmanMomentum = 0.003, learningRate = 0.003 ):
        alldata = SupervisedDataSet(self.nInputCount, self.nOutputCount)
        for x in dataSet:
            alldata.addSample(x[0], x[1])
        
        dsTestDataSet, dsTrainingDataSet = alldata.splitWithProportion(0.25)
        trainer = BackpropTrainer( self.elmanNN, dataset=dsTrainingDataSet, learningrate=learningRate, momentum=elmanMomentum, verbose=True, weightdecay=0.01)		
        for i in range( epochs ):
            self.rate = trainer.train()	
        return str(self.rate)
        
    def Predict( self, input ):
        return str(self.elmanNN.activate(input)[0])
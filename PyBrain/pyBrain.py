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
    
    
    
    def __init__( self, softmax = False, inCount = 2, hidCount = 100, outCount = 1 ):
    
        # Initialize Variables
        nInputCount = inCount
        nHiddenCount = hidCount
        nOutputCount = outCount
        
        # Set the net and layers
        elmanNN = RecurrentNetwork()
        elmanNN.addInputModule(LinearLayer(nInputCount, name='inputLayer'))
        
        if (softmax):
            elmanNN.addModule(SoftmaxLayer(nHiddenCount, name='hiddenLayer'))
            elmanNN.addOutputModule(SoftmaxLayer(nOutputCount, name='outputLayer'))
        else:
    	    elmanNN.addModule(LSTMLayer(nHiddenCount, peepholes=False, name='hiddenLayer'))
            elmanNN.addOutputModule(LSTMLayer(nOutputCount, peepholes=False, name='outputLayer'))
        
        # Set the connections
        elmanNN.addConnection(FullConnection(elmanNN['inputLayer'], elmanNN['hiddenLayer'], name='inputToHidden'))
        elmanNN.addConnection(FullConnection(elmanNN['hiddenLayer'], elmanNN['outputLayer'], name='hiddenToOutput'))
        elmanNN.addRecurrentConnection(FullConnection(elmanNN['hiddenLayer'], elmanNN['hiddenLayer'], name='hiddenToHidden'))
        
        # Build the net
    	# elmanNN = buildNetwork( nInputCount, nHiddenCount , nOutputCount , recurrent=True , hiddenclass=SoftmaxLayer , outclass=SoftmaxLayer )
        elmanNN.sortModules()
    	
        return;
    
    	
    	
    	
    	
    def Train( dataSet , epochs = 5000, elmanMomentum = 0.003):
        alldata = SupervisedDataSet(nInputCount,nOutputCount)
        for x in np.nditer(a):
            alldata.addSample(x[0], x[1])
    
    
        dsTestDataSet, dsTrainingDataSet = alldata.splitWithProportion(0.25)
        trainer = BackpropTrainer( elmanNN, dataset=dsTrainingDataSet, momentum=elmanMomentum, verbose=True, weightdecay=0.01)
            for i in range( epochs ):
                trainer.train()
    			
        return;
    	
    	
    	
    	
    	
    def Predict( input )
        return elmanNN.activate(input)
# Import PyBrain components
from pybrain.datasets            import ClassificationDataSet
from math 						 import sqrt
from pybrain.utilities           import percentError
from pybrain.tools.shortcuts     import buildNetwork
from pybrain.supervised.trainers import BackpropTrainer
from pybrain.structure.modules   import SoftmaxLayer
from pybrain.structure 			 import RecurrentNetwork
from pybrain.structure 			 import LinearLayer, SigmoidLayer
from pybrain.structure 			 import FullConnection
from pybrain.datasets 			 import SupervisedDataSet

# Import Graphical output
from pylab import ion, ioff, figure, draw, contourf, clf, show, hold, plot
from scipy import diag, arange, meshgrid, where
from numpy.random import multivariate_normal

# Initialize Variables
nInputCount = 1
nHiddenCount = 100
nOutputCount = 1

# Set the net and layers
elmanNN = RecurrentNetwork()
elmanNN.addInputModule(LinearLayer(nInputCount, name='inputLayer'))
# elmanNN.addModule(LSTMLayer(nHiddenCount, peepholes=False, name='hiddenLayer'))
elmanNN.addModule(SoftmaxLayer(nHiddenCount, name='hiddenLayer'))
elmanNN.addOutputModule(SoftmaxLayer(nOutputCount, name='outputLayer'))

# Set the connections
elmanNN.addConnection(FullConnection(elmanNN['inputLayer'], elmanNN['hiddenLayer'], name='inputToHidden'))
elmanNN.addConnection(FullConnection(elmanNN['hiddenLayer'], elmanNN['outputLayer'], name='hiddenToOutput'))
elmanNN.addRecurrentConnection(FullConnection(elmanNN['hiddenLayer'], elmanNN['hiddenLayer'], name='hiddenToHidden'))

# Build the net
elmanNN.sortModules()


alldata = SupervisedDataSet(nInputCount,nOutputCount)
alldata.addSample([0],[0])
alldata.addSample([0],[0])
alldata.addSample([0],[0])
alldata.addSample([0],[1])
alldata.addSample([1],[1])
alldata.addSample([1],[1])
alldata.addSample([1],[0])
alldata.addSample([0],[1])
alldata.addSample([1],[1])
alldata.addSample([1],[0])

dsTestDataSet, dsTrainingDataSet = alldata.splitWithProportion(0.25)

# dsTestDataSet = SupervisedDataSet(2,1)
# for n in xrange(0, dsTestDataSet_temp.getLength()):
#     dsTestDataSet.appendLinked( dsTestDataSet_temp.getSample(n)[0], dsTestDataSet_temp.getSample(n)[1] )
# 
# dsTrainingDataSet = SupervisedDataSet(2,1)
# for n in xrange(0, dsTrainingDataSet_temp.getLength()):
#     dsTrainingDataSet.appendLinked( dsTrainingDataSet_temp.getSample(n)[0], dsTrainingDataSet_temp.getSample(n)[1] )
# 
# 
# dsTestDataSet._convertToOneOfMany( )	
# dsTrainingDataSet._convertToOneOfMany( )


# print "Number of training patterns: ", len(dsTrainingDataSet)
# print "Input and output dimensions: ", dsTrainingDataSet.indim, dsTrainingDataSet.outdim
# print "First sample (input, target, class):"
# print dsTrainingDataSet['input'][0], dsTrainingDataSet['target'][0], dsTrainingDataSet['class'][0]

elmanNN = buildNetwork( nInputCount, nHiddenCount , nOutputCount , recurrent=True , hiddenclass=SoftmaxLayer , outclass=SoftmaxLayer )
trainer = BackpropTrainer( elmanNN, dataset=alldata, momentum=0.003, verbose=True, weightdecay=0.01)

for i in range( 1000 ):
    mse = trainer.train()

	
print elmanNN.activate([0])
print elmanNN.activate([0])
print elmanNN.activate([0])
print elmanNN.activate([0])
print elmanNN.activate([1])
print elmanNN.activate([1])	
print elmanNN.activate([1])
print elmanNN.activate([0])
print elmanNN.activate([1])
print elmanNN.activate([1])

#trainer.trainUntilConvergence(dataset=dsTrainingDataSet, maxEpochs=1000, verbose=None, continueEpochs=10, validationProportion=0.25)
 
#trnresult = percentError( trainer.testOnClassData(),dsTrainingDataSet['target'] )
#tstresult = percentError( trainer.testOnClassData(dataset=dsTestDataSet ), dsTestDataSet['target'] )




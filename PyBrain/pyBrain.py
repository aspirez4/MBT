# Import PyBrain components
from pybrain.datasets            import ClassificationDataSet
from pybrain.utilities           import percentError
from pybrain.tools.shortcuts     import buildNetwork
from pybrain.supervised.trainers import BackpropTrainer
from pybrain.structure.modules   import SoftmaxLayer
from pybrain.structure 			 import RecurrentNetwork
from pybrain.structure 			 import LinearLayer, SigmoidLayer
from pybrain.structure 			 import FullConnection

# Import Graphical output
from pylab import ion, ioff, figure, draw, contourf, clf, show, hold, plot
from scipy import diag, arange, meshgrid, where
from numpy.random import multivariate_normal

# Initialize Variables
nInputCount = 2
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

means = [(-1,0),(2,4),(3,1)]
cov = [diag([1,1]), diag([0.5,1.2]), diag([1.5,0.7])]
alldata = ClassificationDataSet(2, 1, nb_classes=3)
for n in xrange(400):
    for klass in range(3):
        input = multivariate_normal(means[klass],cov[klass])
        alldata.addSample(input, [klass])
		
tstdata, trndata = alldata.splitWithProportion( 0.25 )
trndata._convertToOneOfMany( )
tstdata._convertToOneOfMany( )
print "Number of training patterns: ", len(trndata)
print "Input and output dimensions: ", trndata.indim, trndata.outdim
print "First sample (input, target, class):"
print trndata['input'][0], trndata['target'][0], trndata['class'][0]

elmanNN = buildNetwork( trndata.indim, 5, trndata.outdim, outclass=SoftmaxLayer )
trainer = BackpropTrainer( elmanNN, dataset=trndata, momentum=0.1, verbose=True, weightdecay=0.01)




להוריד enthought canopy
https://store.enthought.com/downloads/#default
להחליף את המשתנה סביבה של פייטון ב:
C:\Users\<username>\AppData\Local\Enthought\Canopy\User\Scripts

git clone git://github.com/pybrain/pybrain.git pybrain
python setup.py install
python
import pybrain












###### Update pip (python 2.7.9+ / 3.4+)
python -m pip install -U pip

###### Donload and install Microsoft Visual C++ Compiler for Python 2.7
https://www.microsoft.com/en-us/download/details.aspx?id=44266


###### Install tensorflow
easy_install --upgrade six
pip install --upgrade https://storage.googleapis.com/tensorflow/mac/tensorflow-0.5.0-py2-none-any.whl



easy_install -U six



###### Test TensorFlow installation
$ python
...
>>> import tensorflow as tf
>>> hello = tf.constant('Hello, TensorFlow!')
>>> sess = tf.Session()
>>> print sess.run(hello)
Hello, TensorFlow!
>>> a = tf.constant(10)
>>> b = tf.constant(32)
>>> print sess.run(a + b)
42
>>>



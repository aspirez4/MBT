import numpy as np
import theano
import theano.tensor as T
import sys

class NN:

    dtype          = None
    trainingErrors = None
    act            = None
    sigma          = None
    inputDataSet   = None
    outputDataSet  = None
    epochs         = 0
    W_xo           = None
    W_co           = None
    learningFunction = None
    predictFunction  = None
    def trainRNN(self, trainingData):
        trainCost = 0.0	
        for x in range(self.epochs):
            error = 0.0
            for j in range(len(trainingData)):  
                index = np.random.randint(0, len(trainingData))
                i, o = trainingData[index]
                trainCost = self.learningFunction(i, o)
                error += trainCost
            self.trainingErrors[x] = error 
            print "{} : {}  ({})".format(x, error, trainCost)

    def train(self, inData, outData):
        self.inputDataSet = inData
        self.outputDataSet = outData
        trainingSize = int(len(self.inputDataSet) * 0.75)
        trainingData = self.getData(fromIndex = 0, toIndex = trainingSize)	
        self.trainRNN(trainingData)
        
    def test(self):
        testingStartIndex = int(len(self.inputDataSet) * 0.75) + 1
        testingEndIndex = len(self.inputDataSet) - 1
        testingData = self.getData(fromIndex = testingStartIndex, toIndex = testingEndIndex)
        data = testingData
        count = 0
        mse = 0.0
        for inputData, targetData in data:
            currPrediction   = self.predictFunction(inputData)
            mse = mse + ((targetData[0][0] - currPrediction[0][0]) ** 2)
            count += 1
            print 'Target  : {}'.format(targetData[0][0])
            print 'Predict : {}\n'.format(currPrediction[0][0])
        print 'MSE: {}'.format(mse / count)
        return str(mse / count)
    
    def predict(self, inData):
        data = inData
        currPrediction = self.predictFunction(data)
        print currPrediction[0][0]
        return str(currPrediction[0][0])
			
    def singleData(self, index):
        a = [] 
        b = []
        a.append(self.inputDataSet[index])
        b.append(self.outputDataSet[index])
        return a,b

    def getData(self, fromIndex, toIndex):
        ret = []
        for nIndex in range(fromIndex, toIndex):
            ret.append(self.singleData(nIndex))
        return ret		

    def oneLSTMstep(self, x_t, h_tm1, c_tm1, W_xi, W_hi, W_ci, b_i, W_xf, W_hf, W_cf, b_f, W_xc, W_hc, b_c, W_xy, W_ho, W_cy, b_o, W_hy, b_y):
        i_t = self.sigma(theano.dot(x_t, W_xi) + theano.dot(h_tm1, W_hi) + theano.dot(c_tm1, W_ci) + b_i)
        f_t = self.sigma(theano.dot(x_t, W_xf) + theano.dot(h_tm1, W_hf) + theano.dot(c_tm1, W_cf) + b_f)
        c_t = f_t * c_tm1 + i_t * self.act(theano.dot(x_t, W_xc) + theano.dot(h_tm1, W_hc) + b_c) 
        o_t = self.sigma(theano.dot(x_t, self.W_xo)+ theano.dot(h_tm1, W_ho) + theano.dot(c_t, self.W_co)  + b_o)
        h_t = o_t * self.act(c_t)
        y_t = self.sigma(theano.dot(h_t, W_hy) + b_y) 
        return [h_t, c_t, y_t]
    
    #TODO: Use a more appropriate initialization method
    def sampleWeights(self, sizeX, sizeY):
        values = np.ndarray([sizeX, sizeY], dtype=self.dtype)
        for dx in xrange(sizeX):
            vals = np.random.uniform(low=-1., high=1.,  size=(sizeY,))
            #vals_norm = np.sqrt((vals**2).sum())
            #vals = vals / vals_norm
            values[dx,:] = vals
        _,svs,_ = np.linalg.svd(values)
        #svs[0] is the largest singular value                      
        values = values / svs[0]
        return values

    def __init__( self, inCount = 2, hidCount = 100, outCount = 1, nEpochs = 20000, learningRate = 0.003 ):
        print 'Start initializig NN . . .'
        self.dtype = theano.config.floatX
        n_hidden = n_i = n_c = n_o = n_f = hidCount
        # therefore we use the logistic function
        self.sigma = lambda x: 1 / (1 + T.exp(-x))
        # for the other activation function we use the tanh
        self.act = T.tanh

        print '* Set weights'
        # initialize weights
        # i_t and o_t should be "open" or "closed"
        # f_t should be "open" (don't forget at the beginning of training)
        # we try to archive this by appropriate initialization of the corresponding biases 
        W_xi = theano.shared(self.sampleWeights(inCount, n_i))  
        W_hi = theano.shared(self.sampleWeights(n_hidden, n_i))  
        W_ci = theano.shared(self.sampleWeights(n_c, n_i))  
        b_i  = theano.shared(np.cast[self.dtype](np.random.uniform(-0.5,.5,size = n_i)))
        W_xf = theano.shared(self.sampleWeights(inCount, n_f)) 
        W_hf = theano.shared(self.sampleWeights(n_hidden, n_f))
        W_cf = theano.shared(self.sampleWeights(n_c, n_f))
        b_f  = theano.shared(np.cast[self.dtype](np.random.uniform(0, 1.,size = n_f)))
        W_xc = theano.shared(self.sampleWeights(inCount, n_c))  
        W_hc = theano.shared(self.sampleWeights(n_hidden, n_c))
        b_c  = theano.shared(np.zeros(n_c, dtype=self.dtype))
        self.W_xo = theano.shared(self.sampleWeights(inCount, n_o))
        W_ho = theano.shared(self.sampleWeights(n_hidden, n_o))
        self.W_co = theano.shared(self.sampleWeights(n_c, n_o))
        b_o  = theano.shared(np.cast[self.dtype](np.random.uniform(-0.5,.5,size = n_o)))
        W_hy = theano.shared(self.sampleWeights(n_hidden, outCount))
        b_y  = theano.shared(np.zeros(outCount, dtype=self.dtype))        
        c0   = theano.shared(np.zeros(n_hidden, dtype=self.dtype))
        h0   = T.tanh(c0)
        params = [W_xi, W_hi, W_ci, b_i, W_xf, W_hf, W_cf, b_f, W_xc, W_hc, b_c, self.W_xo, W_ho, self.W_co, b_o, W_hy, b_y, c0]
        print '* Set epoches to - {}'.format(nEpochs)

        #input 
        v = T.matrix(dtype=self.dtype)
        
        # target
        target = T.matrix(dtype=self.dtype)
        
        # hidden and outputs of the entire sequence
        print '* Set architecture'
        [h_vals, _, y_vals], _ = theano.scan(fn=self.oneLSTMstep, 
                                          sequences = dict(input=v, taps=[0]), 
                                          outputs_info = [h0, c0, None ], # corresponds to return type of fn
                                          non_sequences = [W_xi, W_hi, W_ci, b_i, W_xf, W_hf, W_cf, b_f, W_xc, W_hc, b_c, self.W_xo, W_ho, self.W_co, b_o, W_hy, b_y] )
        
        cost = -T.mean(target * T.log(y_vals)+ (1.- target) * T.log(1. - y_vals))
        
        # learning rate
        lr = np.cast[self.dtype](learningRate)
        learningRateShared = theano.shared(lr)
        
        gparams = []
        for param in params:
            gparam = T.grad(cost, param)
            gparams.append(gparam)
        
        updates=[]
        for param, gparam in zip(params, gparams):
            updates.append((param, param - gparam * learningRateShared))
        print '* Set learning function'
        self.learningFunction = theano.function(inputs = [v, target],
                                                outputs = cost,
                                                updates = updates)

        self.epochs = nEpochs       
        self.trainingErrors = np.ndarray(nEpochs)
        print '* Set prediction function'
        self.predictFunction = theano.function(inputs = [v], outputs = y_vals)
        print 'Initialize Done!'
import numpy as np
import theano
import theano.tensor as T
import sys

class NN:

    learningFunction = None
    predictFunction  = None
    StandardDeviation= None
    M                = None
    dtype          = None
    trainingErrors = None
    act            = None
    sigma          = None
    inputDataSet   = None
    outputDataSet  = None
    W_xo           = None
    W_co           = None
    epochs         = 0
    
    W_xi = None
    W_hi = None
    W_ci = None
    b_i  = None
    W_xf = None
    W_hf = None
    W_cf = None
    b_f  = None
    W_xc = None
    W_hc = None
    b_c  = None
    W_ho = None
    b_o  = None 
    W_hy = None 
    b_y  = None 
    c0   = None 
    h0   = None 
    inCount = 2
    hidCount = 100
    outCount = 1
    nEpochs = 20000
    learningRate = 0.003 
	
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
        
    def test(self, symbol):
        testingStartIndex = int(len(self.inputDataSet) * 0.75) + 1
        testingEndIndex = len(self.inputDataSet) - 1
        testingData = self.getData(fromIndex = testingStartIndex, toIndex = testingEndIndex)
        data = testingData
        count = 0
        mse = 0.0
        T = 0
        for inputData, targetData in data:
            currPrediction   = self.predictFunction(inputData)
            mse = mse + ((targetData[0][0] - currPrediction[0][0]) ** 2)
            count += 1
            if ((self.M - (np.log((1 / currPrediction[0][0]) - 1) * self.StandardDeviation)) * (self.M - (np.log((1 / targetData[0][0]) - 1) * self.StandardDeviation)) > 0) :
                T += 1
            print 'Target  : {}'.format(targetData[0][0])
            print 'Predict : {}\n'.format(currPrediction[0][0])
        print '{} MSE: {}\n'.format(symbol, (mse / count))
        print '{0} RTE: {1:.2f}%'.format(symbol, (T * 100 / count))
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

    def oneLSTMstep(self, x_t, h_tm1, c_tm1):
        i_t = self.sigma(theano.dot(x_t, self.W_xi) + theano.dot(h_tm1, self.W_hi) + theano.dot(c_tm1, self.W_ci) + self.b_i)
        f_t = self.sigma(theano.dot(x_t, self.W_xf) + theano.dot(h_tm1, self.W_hf) + theano.dot(c_tm1, self.W_cf) + self.b_f)
        c_t = f_t * c_tm1 + i_t * self.act(theano.dot(x_t, self.W_xc) + theano.dot(h_tm1, self.W_hc) + self.b_c) 
        o_t = self.sigma(theano.dot(x_t, self.W_xo)+ theano.dot(h_tm1, self.W_ho) + theano.dot(c_t, self.W_co)  + self.b_o)
        h_t = o_t * self.act(c_t)
        y_t = self.sigma(theano.dot(h_t, self.W_hy) + self.b_y) 
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
        self.inCount = inCount
        self.hidCount = hidCount
        self.outCount = outCount
        self.nEpochs = nEpochs
        self.learningRate = learningRate 

    def cTor(self, bNewInstance, dataMean, dataStandardDeviation):
        print 'Start initializig NN . . .'
        self.M = dataMean
        self.StandardDeviation = dataStandardDeviation
        self.dtype = theano.config.floatX
        n_hidden = n_i = n_c = n_o = n_f = self.hidCount
        # therefore we use the logistic function
        self.sigma = lambda x: 1 / (1 + T.exp(-x))
        # for the other activation function we use the tanh
        self.act = T.tanh

        if (bNewInstance):
            print '* Set weights'
            # initialize weights
            # i_t and o_t should be "open" or "closed"
            # f_t should be "open" (don't forget at the beginning of training)
            # we try to archive this by appropriate initialization of the corresponding biases 
            self.W_xi = theano.shared(self.sampleWeights(self.inCount, n_i))  
            self.W_hi = theano.shared(self.sampleWeights(n_hidden, n_i))  
            self.W_ci = theano.shared(self.sampleWeights(n_c, n_i))  
            self.b_i  = theano.shared(np.cast[self.dtype](np.random.uniform(-0.5,.5,size = n_i)))
            self.W_xf = theano.shared(self.sampleWeights(self.inCount, n_f)) 
            self.W_hf = theano.shared(self.sampleWeights(n_hidden, n_f))
            self.W_cf = theano.shared(self.sampleWeights(n_c, n_f))
            self.b_f  = theano.shared(np.cast[self.dtype](np.random.uniform(0, 1.,size = n_f)))
            self.W_xc = theano.shared(self.sampleWeights(self.inCount, n_c))  
            self.W_hc = theano.shared(self.sampleWeights(n_hidden, n_c))
            self.b_c  = theano.shared(np.zeros(n_c, dtype=self.dtype))
            self.W_xo = theano.shared(self.sampleWeights(self.inCount, n_o))
            self.W_ho = theano.shared(self.sampleWeights(n_hidden, n_o))
            self.W_co = theano.shared(self.sampleWeights(n_c, n_o))
            self.b_o  = theano.shared(np.cast[self.dtype](np.random.uniform(-0.5,.5,size = n_o)))
            self.W_hy = theano.shared(self.sampleWeights(n_hidden, self.outCount))
            self.b_y  = theano.shared(np.zeros(self.outCount, dtype=self.dtype))        
            self.c0   = theano.shared(np.zeros(n_hidden, dtype=self.dtype))
            self.h0   = T.tanh(self.c0)

        params = [self.W_xi, self.W_hi, self.W_ci, self.b_i, self.W_xf, self.W_hf, self.W_cf, self.b_f, self.W_xc, self.W_hc, self.b_c, self.W_xo, self.W_ho, self.W_co, self.b_o, self.W_hy, self.b_y, self.c0]
        print '* Set epoches to - {}'.format(self.nEpochs)

        #input 
        v = T.matrix(dtype=self.dtype)
        
        # target
        target = T.matrix(dtype=self.dtype)
        
        # hidden and outputs of the entire sequence
        print '* Set architecture'
        [h_vals, _, y_vals], _ = theano.scan(fn=self.oneLSTMstep, 
                                          sequences = dict(input=v, taps=[0]), 
                                          outputs_info = [self.h0, self.c0, None ], # corresponds to return type of fn
                                          non_sequences = [] )
        
        cost = -T.mean(target * T.log(y_vals)+ (1.- target) * T.log(1. - y_vals))
        
        # learning rate
        lr = np.cast[self.dtype](self.learningRate)
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

        self.epochs = self.nEpochs       
        self.trainingErrors = np.ndarray(self.nEpochs)
        print '* Set prediction function'
        self.predictFunction = theano.function(inputs = [v], outputs = y_vals)
        print 'Initialize Done!'
		
    def __getstate__(self):
        state = dict(self.__dict__)
        del state['learningFunction']
        del state['predictFunction']
        del state['sigma']	
        del state['act']		
        return state
    
    def __setstate__(self, d):
        self.__dict__.update(d)
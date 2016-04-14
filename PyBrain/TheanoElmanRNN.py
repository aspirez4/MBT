import numpy as np
import theano
import theano.tensor as T

dtype=theano.config.floatX

import reberGrammar
inputCount  = 2
hiddenCount = 10
outputCount = 1

mbtIn = [
np.asarray([0.44878294049197,0.512683301931116 ]),
np.asarray([0.460807502296696,0.404871748166132]),
np.asarray([0.497647835871556,0.471786112416864]),
np.asarray([0.462318033881321,0.455860142267605]),
np.asarray([0.456281493844074,0.465167561277124]),
np.asarray([0.600576834805904,0.49552634013991 ]),
np.asarray([0.465109993868049,0.496386838454181]),
np.asarray([0.543861306342674,0.505629532548404]),
np.asarray([0.489757024768886,0.511117330725917]),
np.asarray([0.467636475325435,0.51338832702219 ]),
np.asarray([0.528734013662084,0.499019762793426]),
np.asarray([0.24117618831451,0.454233001682718 ]),
np.asarray([0.473898257096934,0.44024039183357 ]),
np.asarray([0.591415833326511,0.460572153545095]),
np.asarray([0.47138771700069,0.461322401880146 ]),
np.asarray([0.321890175340261,0.419953634215781]),
np.asarray([0.457240520552768,0.463166500663433]),
np.asarray([0.466816537633048,0.461750156770656]),
np.asarray([0.449930080105884,0.43345300612653 ]),
np.asarray([0.657975000342615,0.470770462794915]),
np.asarray([0.469116778377823,0.500215783402428]),
np.asarray([0.517827635537753,0.512333206399425]),
np.asarray([0.456313884928258,0.510232675858467]),
np.asarray([0.603761540846504,0.540998968006591]),
np.asarray([0.468389182638261,0.50308180446572 ]),
np.asarray([0.719732384109752,0.553204925612106]),
np.asarray([0.438405873338185,0.537320573172192]),
np.asarray([0.639390990498739,0.573935994286288]),
np.asarray([0.484644921626322,0.550112670442252]),
np.asarray([0.206606144910832,0.497756062896766]),
np.asarray([0.439484534658589,0.441706493006533]),
np.asarray([0.67300603622949,0.488626525584794 ]),
np.asarray([0.476173965536144,0.455983120592275]),
np.asarray([0.488674309469559,0.456788998160923]),
np.asarray([0.449290461000708,0.505325861378898]),
np.asarray([0.493684491243582,0.516165852695897]),
np.asarray([0.434947245475217,0.468554094545042]),
np.asarray([0.288788597182953,0.431077020874404]),
np.asarray([0.524708534907859,0.438283865962064]),
np.asarray([0.323986579374803,0.413223089636883]),
np.asarray([0.4870130997053,0.411888811329226  ]),
np.asarray([0.194753264264732,0.363850015087129]),
np.asarray([0.481503706126052,0.402393036875749]),
np.asarray([0.507689241427912,0.39898917817976 ]),
np.asarray([0.498519450650938,0.433895752434987]),
np.asarray([0.434080332995535,0.423309199093034]),
np.asarray([0.503868892336362,0.48513232470736 ]),
np.asarray([0.471606767594458,0.483152937001041]),
np.asarray([0.473634129541267,0.476341914623712]),
np.asarray([0.693681553945979,0.51537433528272 ]),
np.asarray([0.471128152150629,0.522783899113739]),
np.asarray([0.467096044578213,0.515429329562109]),
np.asarray([0.477180957461985,0.516544167535615]),
np.asarray([0.586421697334258,0.539101681094213]),
np.asarray([0.472649472582673,0.494895264821552]),
np.asarray([0.803210757596926,0.561311785910811]),
np.asarray([0.507146312992514,0.569321839593671]),
np.asarray([0.589448980938669,0.591775444289008]),
np.asarray([0.47891016631293,0.570273138084743 ]),
np.asarray([0.682091722639138,0.612161588096036]),
np.asarray([0.529130826366352,0.557345601849921]),
np.asarray([0.332595241035939,0.522435387458606]),
np.asarray([0.516874265131649,0.507920444297202]),
np.asarray([0.386959597250105,0.489530330484637]),
np.asarray([0.476920153219522,0.448496016600714]),
np.asarray([0.510963961034592,0.444862643534362]),
np.asarray([0.427923393125514,0.463928273952277]),
np.asarray([0.433485707451246,0.447250562416196]),
np.asarray([0.482391024766503,0.466336847919475]),
np.asarray([0.433937214736838,0.457740260222939]),
np.asarray([0.444217712072787,0.444391010430578]),
np.asarray([0.559127238079364,0.470631779421348]),
np.asarray([0.453131957393139,0.474561029409726]),
np.asarray([0.530396371755403,0.484162098807506]),
np.asarray([0.472452991523305,0.4918652541648  ]),
np.asarray([0.446984117679052,0.492418535286053]),
np.asarray([0.48363292251311,0.477319672172802 ]),
np.asarray([0.520439442236232,0.49078116914142 ]),
np.asarray([0.493315323508447,0.483364959492029]),
np.asarray([0.640033062357561,0.51688097365888 ]),
np.asarray([0.417694299977104,0.511023010118491]),
np.asarray([0.30407426976298,0.475111279568465 ]),
np.asarray([0.487863749712926,0.468596141063804]),
np.asarray([0.498560417624459,0.469645159887006]),
np.asarray([0.453885571554771,0.432415661726448]),
np.asarray([0.592920395261011,0.467460880783229]),
np.asarray([0.479893002685226,0.502624627367678]),
np.asarray([0.541799229185804,0.513411723262254]),
np.asarray([0.479639790425188,0.5096275978224  ]),
np.asarray([0.438042764868903,0.506459036485227]),
np.asarray([0.488562762302015,0.485587509893427]),
np.asarray([0.264529839559252,0.442514877268232]),
np.asarray([0.477668147594988,0.429688660950069]),
np.asarray([0.421166638893163,0.417994030643664]),
np.asarray([0.488393867396725,0.428064251149228])]
mbtOut = [
np.asarray([0.404871748166132]),
np.asarray([0.471786112416864]),
np.asarray([0.455860142267605]),
np.asarray([0.465167561277124]),
np.asarray([0.49552634013991 ]),
np.asarray([0.496386838454181]),
np.asarray([0.505629532548404]),
np.asarray([0.511117330725917]),
np.asarray([0.51338832702219 ]),
np.asarray([0.499019762793426]),
np.asarray([0.454233001682718]),
np.asarray([0.44024039183357 ]),
np.asarray([0.460572153545095]),
np.asarray([0.461322401880146]),
np.asarray([0.419953634215781]),
np.asarray([0.463166500663433]),
np.asarray([0.461750156770656]),
np.asarray([0.43345300612653 ]),
np.asarray([0.470770462794915]),
np.asarray([0.500215783402428]),
np.asarray([0.512333206399425]),
np.asarray([0.510232675858467]),
np.asarray([0.540998968006591]),
np.asarray([0.50308180446572 ]),
np.asarray([0.553204925612106]),
np.asarray([0.537320573172192]),
np.asarray([0.573935994286288]),
np.asarray([0.550112670442252]),
np.asarray([0.497756062896766]),
np.asarray([0.441706493006533]),
np.asarray([0.488626525584794]),
np.asarray([0.455983120592275]),
np.asarray([0.456788998160923]),
np.asarray([0.505325861378898]),
np.asarray([0.516165852695897]),
np.asarray([0.468554094545042]),
np.asarray([0.431077020874404]),
np.asarray([0.438283865962064]),
np.asarray([0.413223089636883]),
np.asarray([0.411888811329226]),
np.asarray([0.363850015087129]),
np.asarray([0.402393036875749]),
np.asarray([0.39898917817976 ]),
np.asarray([0.433895752434987]),
np.asarray([0.423309199093034]),
np.asarray([0.48513232470736 ]),
np.asarray([0.483152937001041]),
np.asarray([0.476341914623712]),
np.asarray([0.51537433528272 ]),
np.asarray([0.522783899113739]),
np.asarray([0.515429329562109]),
np.asarray([0.516544167535615]),
np.asarray([0.539101681094213]),
np.asarray([0.494895264821552]),
np.asarray([0.561311785910811]),
np.asarray([0.569321839593671]),
np.asarray([0.591775444289008]),
np.asarray([0.570273138084743]),
np.asarray([0.612161588096036]),
np.asarray([0.557345601849921]),
np.asarray([0.522435387458606]),
np.asarray([0.507920444297202]),
np.asarray([0.489530330484637]),
np.asarray([0.448496016600714]),
np.asarray([0.444862643534362]),
np.asarray([0.463928273952277]),
np.asarray([0.447250562416196]),
np.asarray([0.466336847919475]),
np.asarray([0.457740260222939]),
np.asarray([0.444391010430578]),
np.asarray([0.470631779421348]),
np.asarray([0.474561029409726]),
np.asarray([0.484162098807506]),
np.asarray([0.4918652541648  ]),
np.asarray([0.492418535286053]),
np.asarray([0.477319672172802]),
np.asarray([0.49078116914142 ]),
np.asarray([0.483364959492029]),
np.asarray([0.51688097365888 ]),
np.asarray([0.511023010118491]),
np.asarray([0.475111279568465]),
np.asarray([0.468596141063804]),
np.asarray([0.469645159887006]),
np.asarray([0.432415661726448]),
np.asarray([0.467460880783229]),
np.asarray([0.502624627367678]),
np.asarray([0.513411723262254]),
np.asarray([0.5096275978224  ]),
np.asarray([0.506459036485227]),
np.asarray([0.485587509893427]),
np.asarray([0.442514877268232]),
np.asarray([0.429688660950069]),
np.asarray([0.417994030643664]),
np.asarray([0.428064251149228]),
np.asarray([0.425088546149155])]

def one_mbt(index):
    a = [] 
    b = []
    a.append(mbtIn[index])
    b.append(mbtOut[index])
    return a,b

def get_n_mbt():
    ret = []
    for i in xrange(len(mbtIn)):
        ret.append(one_mbt(i))
    return ret

#first dimension is time
v = T.matrix(dtype=dtype)

def rescale_weights(values, factor=1.):
    factor = np.cast[dtype](factor)
    _,svs,_ = np.linalg.svd(values)
    #svs[0] is the largest singular value                      
    values = values / svs[0]
    return values

# Initialize weights mateix
def sample_weights(numOfInNodes, numOfOutNodes):
    weightsToReturn = np.ndarray([numOfInNodes, numOfOutNodes], dtype=dtype)
    for nCurrWeightsVector in xrange(numOfInNodes):
        vectorWeights = np.random.uniform(low=-1., high=1.,  size=(numOfOutNodes,))
        #vals_norm = np.sqrt((vectorWeights**2).sum())
        #vectorWeights = vectorWeights / vals_norm
        weightsToReturn[nCurrWeightsVector,:] = vectorWeights
    _,svs,_ = np.linalg.svd(weightsToReturn)
    #svs[0] is the largest singular value                      
    weightsToReturn = weightsToReturn / svs[0]
    return weightsToReturn
	

# parameters of the rnn as shared variables
def getElmanArchitecture(inputCount, outputCount, hiddenCount):
    b_h = theano.shared(np.zeros(hiddenCount, dtype=dtype)) 
    b_o = theano.shared(np.zeros(outputCount, dtype=dtype)) 
    h0 = theano.shared(np.zeros(hiddenCount, dtype=dtype))
    W_ih = theano.shared(sample_weights(inputCount, hiddenCount))
    W_hh = theano.shared(sample_weights(hiddenCount, hiddenCount))
    W_ho = theano.shared(sample_weights(hiddenCount, outputCount))
    return W_ih, W_hh, b_h, W_ho, b_o, h0

W_ih, W_hh, b_h, W_ho, b_o, h0 = getElmanArchitecture(inputCount, outputCount, hiddenCount)   
params = [W_ih, W_hh, b_h, W_ho, b_o, h0]


# we could use the fact that maximaly two outputs are on, 
# but for simplicity we assume independent outputs:
def logisticActivationFunction(x):
    return 1./(1 + T.exp(-x))

# sequences: x_t
# prior results: h_tm1
# non-sequences: W_ih, W_hh, W_ho, b_h
def oneStep(x_t, h_tm1, W_ih, W_hh, b_h, W_ho, b_o):
    h_t = theano.dot(x_t, W_ih) + theano.dot(h_tm1, W_hh) + b_h
    h_t = logisticActivationFunction(h_t)
    y_t = theano.dot(h_t, W_ho) + b_o 
    y_t = logisticActivationFunction(y_t) 
    return [h_t, y_t]
	
# hidden and outputs of the entire sequence
[h_vals, o_vals], _ = theano.scan(fn=oneStep, 
                                  sequences = dict(input=v, taps=[0]), 
                                  outputs_info = [h0, None], # corresponds to return type of fn
                                  non_sequences = [W_ih, W_hh, b_h, W_ho, b_o] )
								  
# target values
target = T.matrix(dtype=dtype)

# learning rate
learningRate = np.cast[dtype](0.0001)
sharedLearningRate = theano.shared(learningRate)

cost = -T.mean(target * T.log(o_vals) + (1.- target) * T.log(1. - o_vals))

def get_train_functions(cost, v, target):
    gparams = []
    for param in params:
        gparam = T.grad(cost, param)
        gparams.append(gparam)

    updates=[]
    for param, gparam in zip(params, gparams):
        updates.append((param, param - gparam * sharedLearningRate))
    learn_rnn_fn = theano.function(inputs = [v, target],
                                   outputs = cost,
                                   updates = updates)
    return learn_rnn_fn

learn_rnn_fn = get_train_functions(cost, v, target)

import reberGrammar
#train_data = reberGrammar.get_n_examples(1)
train_data = get_n_mbt()

def train_rnn(train_data, nb_epochs=50):      
  train_errors = np.ndarray(nb_epochs)
  for x in range(nb_epochs):
    error = 0.
    for j in range(len(train_data)):  
        index = np.random.randint(0, len(train_data))
        i, o = train_data[index]
        train_cost = learn_rnn_fn(i, o)
        print train_cost
        error += train_cost
    train_errors[x] = error
  return train_errors

nb_epochs=1000
train_errors = train_rnn(train_data, nb_epochs)

predictions = theano.function(inputs = [v], outputs = o_vals)



def print_results(p, y):
    for p, o in zip(p, y):
        print p # prediction
        print o # target
        print
		
		
#inp, outp = reberGrammar.get_one_example(10)
inp, outp = one_mbt(1)
pre = predictions(inp)
print_results(pre, outp)
inp, outp = one_mbt(2)
pre = predictions(inp)
print_results(pre, outp)
inp, outp = one_mbt(3)
pre = predictions(inp)
print_results(pre, outp)
inp, outp = one_mbt(4)
pre = predictions(inp)
print_results(pre, outp)
inp, outp = one_mbt(5)
pre = predictions(inp)
print_results(pre, outp)
inp, outp = one_mbt(6)
pre = predictions(inp)
print_results(pre, outp)
inp, outp = one_mbt(7)
pre = predictions(inp)
print_results(pre, outp)
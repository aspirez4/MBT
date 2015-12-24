using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MBTrading.Entities;

namespace MBTrading.Utils
{
    public class NeuralNetwork
    {
        public static List<NNOrder> NeuralNetworkSelfAwarenessCollection = new List<NNOrder>();

        public double[][] RawTrainData;
        public double[][] NormalizedTestData;
        public double AccuracyRate;

        private static Random rnd;

        private int numInput;
        private int numHidden;
        private int numOutput;

        private double[] inputs;

        private double[][] ihWeights; // input-hidden
        private double[] hBiases;
        private double[] hOutputs;

        private double[][] hoWeights; // hidden-output
        private double[] oBiases;

        private double[] outputs;

        // back-prop specific arrays (these could be local to method UpdateWeights)
        private double[] oGrads; // output gradients for back-propagation
        private double[] hGrads; // hidden gradients for back-propagation

        // back-prop momentum specific arrays (could be local to method Train)
        private double[][] ihPrevWeightsDelta;  // for momentum with back-propagation
        private double[] hPrevBiasesDelta;
        private double[][] hoPrevWeightsDelta;
        private double[] oPrevBiasesDelta;

        public NeuralNetwork(int numInput, int numHidden, int numOutput)
        {
            rnd = new Random((int)DateTime.Now.Ticks); // for InitializeWeights() and Shuffle()

            this.numInput = numInput;
            this.numHidden = numHidden;
            this.numOutput = numOutput;

            this.inputs = new double[numInput];

            this.ihWeights = MakeMatrix(numInput, numHidden);
            this.hBiases = new double[numHidden];
            this.hOutputs = new double[numHidden];

            this.hoWeights = MakeMatrix(numHidden, numOutput);
            this.oBiases = new double[numOutput];

            this.outputs = new double[numOutput];

            // back-prop related arrays below
            this.hGrads = new double[numHidden];
            this.oGrads = new double[numOutput];

            this.ihPrevWeightsDelta = MakeMatrix(numInput, numHidden);
            this.hPrevBiasesDelta = new double[numHidden];
            this.hoPrevWeightsDelta = MakeMatrix(numHidden, numOutput);
            this.oPrevBiasesDelta = new double[numOutput];
        } // ctor
        private static double[][] MakeMatrix(int rows, int cols) // helper for ctor
        {
            double[][] result = new double[rows][];
            for (int r = 0; r < result.Length; ++r)
                result[r] = new double[cols];
            return result;
        }
        public static double[][] NormalizeMatrix(double[][] dataMatrix, int nCountOfColsToNormlize)
        {
            double[][] matrixToRet = new double[dataMatrix.Length][];
            for (int i = 0; i < dataMatrix.Length; ++i)
                matrixToRet[i] = new double[dataMatrix[i].Length];

            // normalize specified cols by computing (x - mean) / sd for each value
            for (int col = 0; col < nCountOfColsToNormlize; col++)
            {
                double sum = 0.0;
                for (int i = 0; i < dataMatrix.Length; ++i)
                    sum += dataMatrix[i][col];
                double mean = sum / dataMatrix.Length;
                sum = 0.0;
                for (int i = 0; i < dataMatrix.Length; ++i)
                    sum += (dataMatrix[i][col] - mean) * (dataMatrix[i][col] - mean);
                // thanks to Dr. W. Winfrey, Concord Univ., for catching bug in original code
                double sd = Math.Sqrt(sum / (dataMatrix.Length - 1));
                if (sd == 0)
                {
                    for (int i = 0; i < dataMatrix.Length; ++i)
                        matrixToRet[i][col] = 0;
                }
                else
                {
                    for (int i = 0; i < dataMatrix.Length; ++i)
                        matrixToRet[i][col] = (dataMatrix[i][col] - mean) / sd;
                }
            }

            // Copy the rest of the cols without normlize
            for (int col = nCountOfColsToNormlize; col < dataMatrix[0].Length; col++)
            {
                for (int row = 0; row < dataMatrix.Length; row++) 
                {
                    matrixToRet[row][col] = dataMatrix[row][col];
                }
            }
            return (matrixToRet);
        }
        public static void MakeTrainAndTestRandom(double[][] allData, out double[][] trainData, out double[][] testData, int nPrcToTrain)
        {
            // split allData into 80% trainData and 20% testData
            Random rnd = new Random((int)DateTime.Now.Ticks);
            int totRows = allData.Length;
            int numCols = allData[0].Length;

            int trainRows = (int)(totRows * nPrcToTrain / 100.0); // hard-coded 80-20 split
            int testRows = totRows - trainRows;

            trainData = new double[trainRows][];
            testData = new double[testRows][];

            int[] sequence = new int[totRows]; // create a random sequence of indexes
            for (int i = 0; i < sequence.Length; ++i)
                sequence[i] = i;

            for (int i = 0; i < sequence.Length; ++i)
            {
                int r = rnd.Next(i, sequence.Length);
                int tmp = sequence[r];
                sequence[r] = sequence[i];
                sequence[i] = tmp;
            }

            int si = 0; // index into sequence[]
            int j = 0; // index into trainData or testData

            for (; si < trainRows; ++si) // first rows to train data
            {
                trainData[j] = new double[numCols];
                int idx = sequence[si];
                Array.Copy(allData[idx], trainData[j], numCols);
                ++j;
            }

            j = 0; // reset to start of test data
            for (; si < totRows; ++si) // remainder to test data
            {
                testData[j] = new double[numCols];
                int idx = sequence[si];
                Array.Copy(allData[idx], testData[j], numCols);
                ++j;
            }
        }
        public static void MakeTrainAndTest(double[][] allData, out double[][] trainData, out double[][] testData, int nPrcOfAllData, int nPrcToTrain)
        {
            // split allData into 80% trainData and 20% testData
            Random rnd = new Random((int)DateTime.Now.Ticks);
            int totRows = allData.Length * nPrcOfAllData / 100;
            int numCols = allData[0].Length;

            int trainRows = (int)(totRows * nPrcToTrain / 100); // hard-coded 80-20 split
            int testRows = totRows - trainRows;

            trainData = new double[trainRows][];
            testData = new double[testRows][];

            for (int i = 0; i < trainRows; i++)
            {
                trainData[i] = new double[numCols];
                Array.Copy(allData[i], trainData[i], numCols);
            }

            for (int i = 0; i < testRows; i++)
            {
                testData[i] = new double[numCols];
                Array.Copy(allData[trainRows + i], testData[i], numCols);
            }

        } 

        // ----------------------------------------------------------------------------------------

        public void SetWeights(double[] weights)
        {
            // copy weights and biases in weights[] array to i-h weights, i-h biases, h-o weights, h-o biases
            int numWeights = (numInput * numHidden) + (numHidden * numOutput) + numHidden + numOutput;
            if (weights.Length != numWeights)
                throw new Exception("Bad weights array length: ");

            int k = 0; // points into weights param

            for (int i = 0; i < numInput; ++i)
                for (int j = 0; j < numHidden; ++j)
                    ihWeights[i][j] = weights[k++];
            for (int i = 0; i < numHidden; ++i)
                hBiases[i] = weights[k++];
            for (int i = 0; i < numHidden; ++i)
                for (int j = 0; j < numOutput; ++j)
                    hoWeights[i][j] = weights[k++];
            for (int i = 0; i < numOutput; ++i)
                oBiases[i] = weights[k++];
        }
        public void InitializeWeights()
        {
            // initialize weights and biases to small random values
            int numWeights = (numInput * numHidden) + (numHidden * numOutput) + numHidden + numOutput;
            double[] initialWeights = new double[numWeights];
            double lo = -0.01;
            double hi = 0.01;
            for (int i = 0; i < initialWeights.Length; ++i)
                initialWeights[i] = (hi - lo) * rnd.NextDouble() + lo;
            this.SetWeights(initialWeights);
        }
        public double[] GetWeights()
        {
            // returns the current set of wweights, presumably after training
            int numWeights = (numInput * numHidden) + (numHidden * numOutput) + numHidden + numOutput;
            double[] result = new double[numWeights];
            int k = 0;
            for (int i = 0; i < ihWeights.Length; ++i)
                for (int j = 0; j < ihWeights[0].Length; ++j)
                    result[k++] = ihWeights[i][j];
            for (int i = 0; i < hBiases.Length; ++i)
                result[k++] = hBiases[i];
            for (int i = 0; i < hoWeights.Length; ++i)
                for (int j = 0; j < hoWeights[0].Length; ++j)
                    result[k++] = hoWeights[i][j];
            for (int i = 0; i < oBiases.Length; ++i)
                result[k++] = oBiases[i];
            return result;
        }

        // ----------------------------------------------------------------------------------------

        private double[] ComputeOutputs(double[] xValues)
        {
            if (xValues.Length != numInput)
            {
                throw new Exception("Bad xValues array length");
            }

            double[] hSums = new double[numHidden]; // hidden nodes sums scratch array
            double[] oSums = new double[numOutput]; // output nodes sums

            // copy x-values to inputs
            for (int i = 0; i < xValues.Length; ++i)
            {
                this.inputs[i] = xValues[i];
            }

            // compute i-h sum of weights * inputs
            Parallel.For(0, numHidden, j =>
            //for (int j = 0; j < numHidden; ++j)
            {
                for (int i = 0; i < numInput; ++i)
                {
                    hSums[j] += this.inputs[i] * this.ihWeights[i][j]; // note +=
                }
            });

            // add biases to input-to-hidden sums
            for (int i = 0; i < numHidden; ++i)
            {
                hSums[i] += this.hBiases[i];
            }

            // apply activation
            for (int i = 0; i < numHidden; ++i)
            {
                this.hOutputs[i] = HyperTanFunction(hSums[i]); // hard-coded
            }

            // compute h-o sum of weights * hOutputs
            Parallel.For(0, numOutput, j =>
            //for (int j = 0; j < numOutput; ++j)   
            {
                for (int i = 0; i < numHidden; ++i)
                {
                    oSums[j] += hOutputs[i] * hoWeights[i][j];
                }
            });

            // add biases to input-to-hidden sums
            for (int i = 0; i < numOutput; ++i)
            {
                oSums[i] += oBiases[i];
            }

            // softmax activation does all outputs at once for efficiency
            double[] softOut = Softmax(oSums);
            Array.Copy(softOut, outputs, softOut.Length);

            // could define a GetOutputs method instead
            double[] retResult = new double[numOutput];
            Array.Copy(this.outputs, retResult, retResult.Length);
            return retResult;
        }
        private static double HyperTanFunction(double x)
        {
            if (x < -20.0) return -1.0; // approximation is correct to 30 decimals
            else if (x > 20.0) return 1.0;
            else return Math.Tanh(x);
        }
        private static double[] Softmax(double[] oSums)
        {
            // determine max output sum
            // does all output nodes at once so scale doesn't have to be re-computed each time
            double max = oSums[0];
            for (int i = 0; i < oSums.Length; ++i)
                if (oSums[i] > max) max = oSums[i];

            // determine scaling factor -- sum of exp(each val - max)
            double scale = 0.0;
            for (int i = 0; i < oSums.Length; ++i)
                scale += Math.Exp(oSums[i] - max);

            double[] result = new double[oSums.Length];
            for (int i = 0; i < oSums.Length; ++i)
                result[i] = Math.Exp(oSums[i] - max) / scale;

            return result; // now scaled so that xi sum to 1.0
        }

        // ----------------------------------------------------------------------------------------

        private void UpdateWeights(double[] tValues, double learnRate, double momentum, double weightDecay)
        {
            // update the weights and biases using back-propagation, with target values, eta (learning rate),
            // alpha (momentum).
            // assumes that SetWeights and ComputeOutputs have been called and so all the internal arrays
            // and matrices have values (other than 0.0)
            if (tValues.Length != numOutput)
                throw new Exception("target values not same Length as output in UpdateWeights");

            // 1. compute output gradients
            for (int i = 0; i < oGrads.Length; ++i)
            {
                // derivative of softmax = (1 - y) * y (same as log-sigmoid)
                double derivative = (1 - outputs[i]) * outputs[i];
                // 'mean squared error version' includes (1-y)(y) derivative
                oGrads[i] = derivative * (tValues[i] - outputs[i]);
            }

            // 2. compute hidden gradients
            for (int i = 0; i < hGrads.Length; ++i)
            {
                // derivative of tanh = (1 - y) * (1 + y)
                double derivative = (1 - hOutputs[i]) * (1 + hOutputs[i]);
                double sum = 0.0;
                for (int j = 0; j < numOutput; ++j) // each hidden delta is the sum of numOutput terms
                {
                    double x = oGrads[j] * hoWeights[i][j];
                    sum += x;
                }
                hGrads[i] = derivative * sum;
            }

            // 3a. update hidden weights (gradients must be computed right-to-left but weights
            // can be updated in any order)
            Parallel.For(0, ihWeights.Length, i =>
            //for (int i = 0; i < ihWeights.Length; ++i) // 0..2 (3)
            {
                Parallel.For(0, ihWeights[0].Length, j =>
                //for (int j = 0; j < ihWeights[0].Length; ++j) // 0..3 (4)
                {
                    double delta = learnRate * hGrads[j] * inputs[i]; // compute the new delta
                    ihWeights[i][j] += delta; // update. note we use '+' instead of '-'. this can be very tricky.
                    // now add momentum using previous delta. on first pass old value will be 0.0 but that's OK.
                    ihWeights[i][j] += momentum * ihPrevWeightsDelta[i][j];
                    ihWeights[i][j] -= (weightDecay * ihWeights[i][j]); // weight decay
                    ihPrevWeightsDelta[i][j] = delta; // don't forget to save the delta for momentum 
                });
            });

            // 3b. update hidden biases
            for (int i = 0; i < hBiases.Length; ++i)
            {
                double delta = learnRate * hGrads[i] * 1.0; // t1.0 is constant input for bias; could leave out
                hBiases[i] += delta;
                hBiases[i] += momentum * hPrevBiasesDelta[i]; // momentum
                hBiases[i] -= (weightDecay * hBiases[i]); // weight decay
                hPrevBiasesDelta[i] = delta; // don't forget to save the delta
            }

            // 4. update hidden-output weights
            Parallel.For(0, hoWeights.Length, i =>
            //for (int i = 0; i < hoWeights.Length; ++i)
            {
                Parallel.For(0, hoWeights[0].Length, j =>
                //for (int j = 0; j < hoWeights[0].Length; ++j)
                {
                    // see above: hOutputs are inputs to the nn outputs
                    double delta = learnRate * oGrads[j] * hOutputs[i];
                    hoWeights[i][j] += delta;
                    hoWeights[i][j] += momentum * hoPrevWeightsDelta[i][j]; // momentum
                    hoWeights[i][j] -= (weightDecay * hoWeights[i][j]); // weight decay
                    hoPrevWeightsDelta[i][j] = delta; // save
                });
            });

            // 4b. update output biases
            for (int i = 0; i < oBiases.Length; ++i)
            {
                double delta = learnRate * oGrads[i] * 1.0;
                oBiases[i] += delta;
                oBiases[i] += momentum * oPrevBiasesDelta[i]; // momentum
                oBiases[i] -= (weightDecay * oBiases[i]); // weight decay
                oPrevBiasesDelta[i] = delta; // save
            }
        } // UpdateWeights

        // ----------------------------------------------------------------------------------------

        public void Train(double[][] trainData, int maxEprochs, double learnRate, double momentum, double weightDecay, double dErrorBarrier)
        {
            // train a back-prop style NN classifier using learning rate and momentum
            // weight decay reduces the magnitude of a weight value over time unless that value
            // is constantly increased
            int      epoch                  = 0;
            double   dMeanSquaredErrorValue = double.MaxValue;
            double[] xValues                = new double[numInput]; // inputs
            double[] tValues                = new double[numOutput]; // target values

            int[] sequence = new int[trainData.Length];
            for (int i = 0; i < sequence.Length; ++i)
                sequence[i] = i;
               
            // each training tuple
            while (epoch < maxEprochs)
            {
                dMeanSquaredErrorValue = MeanSquaredError(trainData);
                if (dMeanSquaredErrorValue < dErrorBarrier) break; // consider passing value in as parameter
                //if (mse < 0.001) break; // consider passing value in as parameter

                Shuffle(sequence); // visit each training data in random order
                Parallel.For(0, trainData.Length, i =>
                //for (int i = 0; i < trainData.Length; ++i)
                {
                    int idx = sequence[i];
                    Array.Copy(trainData[idx], xValues, numInput);
                    Array.Copy(trainData[idx], numInput, tValues, 0, numOutput);
                    ComputeOutputs(xValues); // copy xValues in, compute outputs (store them internally)
                    UpdateWeights(tValues, learnRate, momentum, weightDecay); // find better weights
                });
                ++epoch;
            }

            dMeanSquaredErrorValue = double.MaxValue;
        }
        private static void Shuffle(int[] sequence)
        {
            for (int i = 0; i < sequence.Length; ++i)
            {
                int r = rnd.Next(i, sequence.Length);
                int tmp = sequence[r];
                sequence[r] = sequence[i];
                sequence[i] = tmp;
            }
        }
        private double MeanSquaredError(double[][] trainData) // used as a training stopping condition
        {
            // average squared error per training tuple
            double sumSquaredError = 0.0;
            double[] xValues = new double[numInput]; // first numInput values in trainData
            double[] tValues = new double[numOutput]; // last numOutput values

            // walk thru each training case. looks like (6.9 3.2 5.7 2.3) (0 0 1)
            for (int i = 0; i < trainData.Length; ++i)
            {
                Array.Copy(trainData[i], xValues, numInput);
                Array.Copy(trainData[i], numInput, tValues, 0, numOutput); // get target values
                double[] yValues = this.ComputeOutputs(xValues); // compute output using current weights
                for (int j = 0; j < numOutput; ++j)
                {
                    double err = tValues[j] - yValues[j];
                    sumSquaredError += err * err;
                }
            }

            return sumSquaredError / trainData.Length;
        }

        // ----------------------------------------------------------------------------------------

        public double Accuracy(double[][] testData, int nPositiveIndex)
        {
            // percentage correct using winner-takes all
            int numCorrect = 0;
            int numWrong = 0;
            int nTruePositive = 0;
            int nFalsePositive = 0;

            double[] xValues = new double[numInput]; // inputs
            double[] tValues = new double[numOutput]; // targets
            double[] yValues; // computed Y

            for (int i = 0; i < testData.Length; ++i)
            {
                Array.Copy(testData[i], xValues, numInput); // parse test data into x-values and t-values
                Array.Copy(testData[i], numInput, tValues, 0, numOutput);
                yValues = this.ComputeOutputs(xValues);
                int maxIndex = MaxIndex(yValues); // which cell in yValues has largest value?

                if (tValues[maxIndex] == 1.0) // ugly. consider AreEqual(double x, double y)
                {
                    ++numCorrect;
                    if (maxIndex == nPositiveIndex) { nTruePositive++; }
                }
                else
                {
                    ++numWrong;
                    if (maxIndex == nPositiveIndex) { nFalsePositive++; }
                }
            }

            double dPossitiveAccuracy = (nTruePositive * 1.0) / (nTruePositive + nFalsePositive);
            double dTotalAccuracyRate = (numCorrect * 1.0) / (numCorrect + numWrong); 
            return (dTotalAccuracyRate); // ugly 2 - check for divide by zero
        }
        public double Predict(double[] testData, int nPositiveIndex)
        {
            double[] xValues = new double[numInput];  // inputs
            double[] tValues = new double[numOutput]; // targets
            double[] yValues; 

            Array.Copy(testData, xValues, numInput); // parse test data into x-values and t-values
            Array.Copy(testData, numInput, tValues, 0, numOutput);
            yValues = this.ComputeOutputs(xValues);
            int maxIndex = MaxIndex(yValues); // which cell in yValues has largest value?

            return (yValues[nPositiveIndex]);

            //if (yValues[maxIndex] > dPossitiveRate)
            //{
            //    return (maxIndex);
            //}

            //return (-1);
        }
        private static int MaxIndex(double[] vector) // helper for Accuracy()
        {
            // index of largest value
            int bigIndex = 0;
            double biggestVal = vector[0];
            for (int i = 0; i < vector.Length; ++i)
            {
                if (vector[i] > biggestVal)
                {
                    biggestVal = vector[i]; bigIndex = i;
                }
            }
            return bigIndex;
        }
    }
}
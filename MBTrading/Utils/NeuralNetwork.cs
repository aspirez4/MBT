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
        public double AccuracyRate;
        public List<double> RawData;
        public List<double> NormalizedData;
        public int NN_MA_Length;
        public string NNPort;

        public NeuralNetwork(int nMA_Length, string strPort)
        {
            this.NNPort = strPort;
            this.NN_MA_Length = nMA_Length;
            this.RawData = new List<double>();
            this.NormalizedData = new List<double>();
        }
        public void KondratenkoKuperinNormalization()
        {
            // Calculate avarage and StandardDeviation
            double M = 0;
            double dStandardDeviation = MathUtils.GetStandardDeviation(this.RawData, out M);

            // Normalization formula by Kondratenko-Kuperin
            foreach (double dCurr in this.RawData)
            {
                this.NormalizedData.Add(1 / (1 + (Math.E * ((M - dCurr) / dStandardDeviation))));
            }
        }
        public static double[][] NormalizeData(double[][] dataMatrix, int nCountOfColsToNormlize)
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
        public double[][][] PrepareElmanDataSet()
        {
            double[][][] arrToReturn = new double[this.NormalizedData.Count - this.NN_MA_Length][][];
            List<double> lstMA = new List<double>();

            // First N values for prepering the inputs (AVG list)
            for (int nInitilizeIndex = 0; nInitilizeIndex < this.NN_MA_Length; nInitilizeIndex++)
			{
                lstMA.Add(this.NormalizedData[nInitilizeIndex]);
			}


            double dInput1 = this.NormalizedData[this.NN_MA_Length - 1];
            double dInput2 = lstMA.Average();
            double dOutput = 0;
            int nFinalArrayIndex = 0;

            // Run over all items in the list
            for (int nIndex = this.NN_MA_Length; nIndex < this.NormalizedData.Count; nIndex++)
            {
                lstMA.RemoveAt(0);
                lstMA.Add(this.NormalizedData[nIndex]);
                
                // Getting the output by the avg
                dOutput = lstMA.Average();

                // Set the final array
                arrToReturn[nFinalArrayIndex++] = new double[][] { new double[] { dInput1, dInput2 }, new double[] { dOutput }};

                // Get the inputValues for the next hop
                dInput1 = this.NormalizedData[nIndex];
                dInput2 = dOutput;
            }

            return arrToReturn;
        }
        public async void Train(double[][] trainData, int maxEprochs, double learnRate, double momentum, double weightDecay, double dErrorBarrier)
        {
            this.KondratenkoKuperinNormalization();
            ElmanDataSet elDataSet = new ElmanDataSet() { dataSet = this.PrepareElmanDataSet() };
            await PythonUtils.CallNN(elDataSet, true, this.NNPort);
        }

        public double Predict(double dValue, double dValueMA)
        {
            ElmanDataSet elDataSet = new ElmanDataSet() { input = new double[] { dValue, dValueMA }};
            return double.Parse(PythonUtils.CallNN(elDataSet, false, this.NNPort).Result);
        }
    }
}
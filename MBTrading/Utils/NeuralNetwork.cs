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
        public double Accuracy;
        public double ErrorRate;
        private List<double> RawData;
        private List<double> TempData;
        private List<double> NormalizedData;
        private int NN_MA_Length;
        private string NNPort;

        public NeuralNetwork(int nMA_Length, string strPort, List<double> lstRowData)
        {
            this.NNPort = strPort;
            this.NN_MA_Length = nMA_Length;
            this.RawData = new List<double>(lstRowData);
            this.TempData = new List<double>(lstRowData);
            for (int i = 0; i < nMA_Length; i++) { this.TempData.Add(0); };
            this.NormalizedData = new List<double>();
        }
        public void KondratenkoKuperinDataSetNormalization()
        {
            // Calculate avarage and StandardDeviation
            double M = 0;
            double dStandardDeviation = MathUtils.GetStandardDeviation(this.RawData, out M);

            // Normalization formula by Kondratenko-Kuperin
            foreach (double dCurr in this.RawData)
            {
                this.NormalizedData.Add(1 / (1 + Math.Pow(Math.E, (M - dCurr) / dStandardDeviation)));
            }
        }
        public void KondratenkoKuperinNormalization(List<double> lstToNormalize)
        {
            // Calculate avarage and StandardDeviation
            double M = 0;

            // Insect the temp lst to Normlize
            for (int nReplaceIndex = 0; nReplaceIndex < lstToNormalize.Count; nReplaceIndex++)
            {
                this.TempData[this.TempData.Count - lstToNormalize.Count + nReplaceIndex] = lstToNormalize[nReplaceIndex];
            }

            double dStandardDeviation = MathUtils.GetStandardDeviation(this.TempData, out M);

            // Normalization formula by Kondratenko-Kuperin
            for (int nNormalizeIndex = 0; nNormalizeIndex < lstToNormalize.Count; nNormalizeIndex++)
            {
                lstToNormalize[nNormalizeIndex] = (1 / (1 + Math.Pow(Math.E, (M - lstToNormalize[nNormalizeIndex]) / dStandardDeviation)));
            }
        }
        public static double[][] NormalizeDataSet(double[][] dataMatrix, int nCountOfColsToNormlize)
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
        public string Train()
        {
            this.KondratenkoKuperinDataSetNormalization();
            ElmanDataSet elDataSet = new ElmanDataSet() { dataSet = this.PrepareElmanDataSet() };
            return (PythonUtils.CallNN(elDataSet, true, this.NNPort).Result);
        }

        public string Predict(double dValue, double dValueMA)
        {
            ElmanDataSet elDataSet = new ElmanDataSet() { input = new double[] { dValue, dValueMA }};
            return (PythonUtils.CallNN(elDataSet, false, this.NNPort).Result);
        }
    }
}
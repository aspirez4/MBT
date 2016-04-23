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
        private List<double> TempDataForCalculationsAsModule;
        private List<double> NormalizedData;
        private int NN_MA_Length;
        private string NNPort;
        private double M;
        private double dStandardDeviation;

        public NeuralNetwork(int nMA_Length, string strPort, List<double> lstRowData)
        {
            this.NNPort = strPort;
            this.NN_MA_Length = nMA_Length;
            this.RawData = new List<double>(lstRowData);
            this.TempDataForCalculationsAsModule = new List<double>(lstRowData);
            for (int i = 0; i < nMA_Length; i++) { this.TempDataForCalculationsAsModule.Add(0); };
            this.NormalizedData = new List<double>();
        }
        
        public List<double> KondratenkoKuperinNormalization(List<double> lstToNormalize)
        {
            // Calculate avarage and StandardDeviation
            this.M = 0;
            this.dStandardDeviation = MathUtils.GetStandardDeviation(this.RawData, out this.M);
            List<double> lstToReturn = new List<double>();

            // Normalization formula by Kondratenko-Kuperin
            foreach (double dCurr in lstToNormalize)
            {
                lstToReturn.Add(1 / (1 + Math.Pow(Math.E, (this.M - dCurr) / this.dStandardDeviation)));
            }
           
            return lstToReturn;
        }

        public List<double> KondratenkoKuperinNormalizeAsModuleTrainingSet(List<double> lstToNormalize)
        {
            // Calculate avarage and StandardDeviation
            

            // Insect the temp lst to Normlize
            for (int nReplaceIndex = 0; nReplaceIndex < lstToNormalize.Count; nReplaceIndex++)
            {
                this.TempDataForCalculationsAsModule[this.TempDataForCalculationsAsModule.Count - lstToNormalize.Count + nReplaceIndex] = lstToNormalize[nReplaceIndex];
            }

            //MathUtils.GetStandardDeviation(this.TempDataForCalculationsAsModule, out M);
            double dStandardDeviation = this.dStandardDeviation;
            double M = this.M;

            List<double> lstToReturn = new List<double>();
                
            // Normalization formula by Kondratenko-Kuperin
            for (int nNormalizeIndex = 0; nNormalizeIndex < lstToNormalize.Count; nNormalizeIndex++)
            {
                lstToReturn.Add(1 / (1 + Math.Pow(Math.E, (M - lstToNormalize[nNormalizeIndex]) / dStandardDeviation)));
            }

            return lstToReturn;
        }
        public double[][][] PrepareElmanDataSet(List<double> lstListToSet)
        {
            double[][][] arrToReturn = new double[lstListToSet.Count - this.NN_MA_Length][][];
            List<double> lstMA = new List<double>();

            // First N values for prepering the inputs (AVG list)
            for (int nInitilizeIndex = 0; nInitilizeIndex < this.NN_MA_Length; nInitilizeIndex++)
			{
                lstMA.Add(lstListToSet[nInitilizeIndex]);
			}

            double dInput1 = lstListToSet[this.NN_MA_Length - 1];
            double dInput2 = lstMA.Average();
            double dOutput = 0;
            int nFinalArrayIndex = 0;

            // Run over all items in the list
            for (int nIndex = this.NN_MA_Length; nIndex < lstListToSet.Count; nIndex++)
            {
                lstMA.RemoveAt(0);
                lstMA.Add(lstListToSet[nIndex]);
                
                // Getting the output by the avg
                dOutput = lstMA.Average();

                // Set the final array
                arrToReturn[nFinalArrayIndex++] = new double[][] { new double[] { dInput1, dInput2 }, new double[] { dOutput }};

                // Get the inputValues for the next hop
                dInput1 = lstListToSet[nIndex];
                dInput2 = dOutput;
            }

            return arrToReturn;
        }
        
        public string Train()
        {
            this.NormalizedData = this.KondratenkoKuperinNormalization(this.RawData);
            ElmanDataSet elDataSet = new ElmanDataSet() { dataSet = this.PrepareElmanDataSet(this.NormalizedData) };
            return (PythonUtils.CallNN(elDataSet, true, this.NNPort, this.M, this.dStandardDeviation).Result);
        }

        public double Predict(double dValue, double dValueMA)
        {
            ElmanDataSet elDataSet = new ElmanDataSet() { input = new double[] { dValue, dValueMA } };
            double dToReturn = 0;
            string strPrediction = PythonUtils.CallNN(elDataSet, false, this.NNPort, this.M, this.dStandardDeviation).Result;
            double dPrediction = double.Parse(strPrediction);
            dToReturn = this.M - (Math.Log((1 / dPrediction) - 1, Math.E) * this.dStandardDeviation);

            return (dToReturn);
        }
    }
}
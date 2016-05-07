using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MBTrading.Entities;
using System.Threading;

namespace MBTrading.Utils
{
    public class NeuralNetwork
    {
        public static Dictionary<string, double> NormalizedData_SDV_List = new Dictionary<string, double>();
        public static int[] ports = { 4567, 4568, 4569, 4570 };

        public double NormalizedData_SDV;
        public double Accuracy;
        public double ErrorRate;
        private List<double> RawData;
        private List<double> TempDataForCalculationsAsModule;
        private List<double> NormalizedData;
        private int NN_MA_Length;
        private string NNPort;
        private double M;
        private double dStandardDeviation;
        private string Symbol;

        public NeuralNetwork(int nMA_Length, List<double> lstRowData, string strSymbol)
        {
            this.NN_MA_Length = Consts.NEURAL_NETWORK_MA_LENGTH;
            this.Symbol = strSymbol;
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
        public void PrepareElmanDataSet(List<double> lstListToSet, out double[][] input, out double[][] target)
        {
            input = new double[lstListToSet.Count - this.NN_MA_Length][];
            target = new double[lstListToSet.Count - this.NN_MA_Length][];

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
                input[nFinalArrayIndex] = new double[] { dInput1, dInput2 };
                target[nFinalArrayIndex] = new double[] { dOutput };
                nFinalArrayIndex++;

                // Get the inputValues for the next hop
                dInput1 = lstListToSet[nIndex];
                dInput2 = dOutput;
            }
        }
        
        public string Train()
        {
            string strToReturn = null;

            // If sortedList is full from the last NNTraining iterations - clear it
            if (NeuralNetwork.NormalizedData_SDV_List.Count == Program.SymbolsNamesList.Count) NeuralNetwork.NormalizedData_SDV_List.Clear();

            // Normlize data
            this.NormalizedData = this.KondratenkoKuperinNormalization(this.RawData);

            // Calculate avarage and StandardDeviation of Normlize data
            double NormalizedData_M = 0;
            List<double> lstCheckProductivity = new List<double>();
            List<double> lstTemp = this.NormalizedData.GetRange(0, this.NN_MA_Length);

            for (int nIndex = this.NN_MA_Length; nIndex < this.NormalizedData.Count; nIndex++) 
            { 
                lstCheckProductivity.Add(lstTemp.Average()); 
                lstTemp.RemoveAt(0); 
                lstTemp.Add(this.NormalizedData[nIndex]); 
            }

            // Get the results and add it to the sortedList
            this.NormalizedData_SDV = MathUtils.GetStandardDeviation(lstCheckProductivity, out NormalizedData_M);
            NeuralNetwork.NormalizedData_SDV_List.Add(this.Symbol, this.NormalizedData_SDV);

            // While sortedList is not full - wait
            while (NeuralNetwork.NormalizedData_SDV_List.Count != Program.SymbolsNamesList.Count)
            {
                Thread.Sleep(1000);
            }

            // If this NN NormlizedData standardDeviation was one of first 'X' - Start Train this NN
            int nPortIndex = NeuralNetwork.IsSymbolIsOneOfKFirsts(this.Symbol);
            if (nPortIndex != -1)
            {
                this.NNPort = ports[nPortIndex].ToString();

                double[][] i = null;
                double[][] t = null;
                this.PrepareElmanDataSet(this.NormalizedData, out i, out t);
                ElmanDataSet elDataSet = new ElmanDataSet() { input = i, target = t };
                strToReturn = (PythonUtils.CallNN(elDataSet, true, this.NNPort, this.M, this.dStandardDeviation).Result);
            }

            return strToReturn;
        }
        public double Predict(double dValue, double dValueMA)
        {
            double dToReturn = -1;
            
            ElmanDataSet input = new ElmanDataSet() { input = new double[][] { new double[] { dValue, dValueMA } } };
            string strPrediction = PythonUtils.CallNN(input, false, this.NNPort, this.M, this.dStandardDeviation).Result;
            double dPrediction = double.Parse(strPrediction);

            // Reverse normalization
            dToReturn = this.M - (Math.Log((1 / dPrediction) - 1, Math.E) * this.dStandardDeviation);

            return (dToReturn);
        }

        private static int IsSymbolIsOneOfKFirsts( string strSymbol)
        {
            int nIndexToRetun = -1;
            var orderByVal = NormalizedData_SDV_List.OrderByDescending(v => v.Value);

            for (int nIndex = 0; nIndex < NeuralNetwork.ports.Length; nIndex++)
			{
                if (orderByVal.ElementAt(nIndex).Key.Equals(strSymbol))
                {
                    nIndexToRetun = nIndex;
                    break;
                }
			}
            
            return nIndexToRetun;

        }
    }
}
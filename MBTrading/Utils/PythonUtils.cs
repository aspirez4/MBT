using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
namespace MBTrading.Utils
{
    public class PythonUtils
    {
        public static async Task<string> CallNN(ElmanDataSet elmanData, bool bTrain, string strPort, double M, double standard)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = new TimeSpan(48, 0, 0);
                client.DefaultRequestHeaders.ExpectContinue = false;
                client.BaseAddress = new Uri(string.Format("http://127.0.0.1:{0}", strPort)); 
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.PostAsync(bTrain ? "/train" : "/predict", new StringContent(ElmanDataSet.JsonSerializer(elmanData), Encoding.UTF8, "application/json"));
                return await response.Content.ReadAsStringAsync();
            }
        }

        public static void StartPythonInstances_SeparateProcesses()
        {
            foreach (int nPort in NeuralNetwork.ports)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.FileName = "cmd.exe";

                // startInfo.Arguments = string.Format("/c C:\\Users\\Or\\AppData\\Local\\Enthought\\Canopy\\User\\Scripts\\python.exe ..\\..\\..\\..\\PyBrain\\pyBrainServer.py {0}", nPort.ToString());
                startInfo.Arguments = string.Format("/c ..\\..\\..\\..\\PyBrain\\TheanoEnv.bat {0} {1}", nPort.ToString(), 500);

                // Start the NN process
                Process.Start(startInfo);
            }   
        }
    }



    [DataContract]
    public class ElmanDataSet
    {
        [DataMember(Order = 1)]
        public double[][] input { get; set; }
        [DataMember(Order = 2)]
        public double[][] target { get; set; }
        [DataMember(Order = 3)]
        public string symbol { get; set; }
        [DataMember(Order = 4)]
        public int chunkIndex { get; set; }
        [DataMember(Order = 5)]
        public int period { get; set; }
        [DataMember(Order = 6)]
        public double dataMean { get; set; }
        [DataMember(Order = 7)]
        public double dataStandardDeviation { get; set; }

        public static string JsonSerializer(ElmanDataSet objectToSerialize)
        {
            if (objectToSerialize == null)
            {
                throw new ArgumentException("objectToSerialize must not be null");
            }
            MemoryStream ms = null;

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(objectToSerialize.GetType());
            ms = new MemoryStream();
            serializer.WriteObject(ms, objectToSerialize);
            ms.Seek(0, SeekOrigin.Begin);
            StreamReader sr = new StreamReader(ms);
            return sr.ReadToEnd();
        }
    }
}

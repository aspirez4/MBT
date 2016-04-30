﻿using System;
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

                //if (bTrain)
                //{
                //    StringBuilder sbInput = new StringBuilder("[");
                //    StringBuilder sbTarget = new StringBuilder("[");
                //    for (int i = 0; i < elmanData.dataSet.Length; i++)
                //    {
                //        sbInput.Append("np.asarray([");
                //        sbTarget.Append("np.asarray([");
                //
                //        for (int x = 0; x < 2; x++)
                //        {
                //            sbInput.Append(elmanData.dataSet[i][0][x] + ",");
                //        }
                //
                //        sbInput.Remove(sbInput.Length - 1, 1);
                //
                //        for (int y = 0; y < 1; y++)
                //        {
                //            sbTarget.Append(elmanData.dataSet[i][1][y]);
                //        }
                //
                //        sbInput.Append("]),");
                //        sbTarget.Append("]),");
                //    }
                //    sbInput.Remove(sbInput.Length - 1, 1);
                //    sbTarget.Remove(sbTarget.Length - 1, 1);
                //    sbInput.Append("]");
                //    sbTarget.Append("]");
                //
                //    str = sbInput.ToString() + "," + sbTarget.ToString();
                //}

                bool bServerIsUp = false;
                while ((bTrain) && (!bServerIsUp))
                {
                    try 
                    { 
                        var res = await client.PostAsync("/check", new StringContent("{ check : 'bIsOk?'}", Encoding.UTF8, "application/json")); 
                        bServerIsUp = true; 
                    }
                    catch (Exception e)
                    {
                        bServerIsUp = false;
                        Thread.Sleep(1000);
                    }
                }

                var response = await client.PostAsync(bTrain ? "/train" : "/predict", new StringContent(ElmanDataSet.JsonSerializer(elmanData), Encoding.UTF8, "application/json"));
                return await response.Content.ReadAsStringAsync();
            }
        }

        public static void StartPythonInstance()
        {
            int nPort = 4567;
            Program.SymbolsPorts = new Dictionary<string, string>();
            foreach (string strSymbol in Program.SharesList.Keys)
            {
                // Save the ports in dictionary
                Program.SymbolsPorts.Add(strSymbol, nPort.ToString());
                nPort++;
            }

            StartPythonSingleInstance(nPort);
        }
        public static void StartPythonInstances_SeparateProcesses(Boolean bLazy)
        {
            int nPort = 4567;
            Program.SymbolsPorts = new Dictionary<string,string>();
            foreach (string strSymbol in Program.SharesList.Keys)
            {
                // Save the ports in dictionary
                Program.SymbolsPorts.Add(strSymbol, nPort.ToString());
                nPort++;

                if (!bLazy) { StartPythonSingleInstance(nPort); }
            }   
        }
        public static void StartPythonSingleInstance(int nPort)
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



    [DataContract]
    public class ElmanDataSet
    {
        [DataMember(Order = 1)]
        public double[][] input { get; set; }
        [DataMember(Order = 2)]
        public double[][] target { get; set; }


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

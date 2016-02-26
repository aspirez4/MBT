using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Net.Http.Headers;
namespace MBTrading.Utils
{
    public class PythonUtils
    {
        public static async void  callTrainer()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.ExpectContinue = false;
                client.BaseAddress = new Uri("http://127.0.0.1:4567"); 
                double[] a = new double[] { 1, 2 };
                double[] a1 = new double[] { 1 };
                double[][] b = new double[][] { a, a1 };
                double[][][] c = new double[][][] { b, b, b };
                string s = c.ToString();

                ElmanDataSet elmanData = new ElmanDataSet { dataSet = c };
                string postBody = ElmanDataSet.JsonSerializer(elmanData);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                //var response = await client.GetAsync("/train?data=[[[1,1,],[2]]]");
                var response = await client.PostAsync("/train", new StringContent(postBody, Encoding.UTF8, "application/json"));

                var responseString = await response.Content.ReadAsStringAsync();
            }
        }
    }



    [DataContract]
    public class ElmanDataSet
    {
        [DataMember(Order = 1)]
        public double[][][] dataSet { get; set; }
        [DataMember(Order = 2)]
        public double[] input { get; set; }


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

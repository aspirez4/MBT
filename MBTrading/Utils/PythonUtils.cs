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
namespace MBTrading.Utils
{
    public class PythonUtils
    {
        public static async Task<string> CallNN(ElmanDataSet elmanData, bool bTrain, string strPort)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.ExpectContinue = false;
                client.BaseAddress = new Uri(string.Format("http://127.0.0.1:{0}", strPort)); 
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.PostAsync(bTrain ? "/train" : "/predict", new StringContent(ElmanDataSet.JsonSerializer(elmanData), Encoding.UTF8, "application/json"));
                return await response.Content.ReadAsStringAsync();
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

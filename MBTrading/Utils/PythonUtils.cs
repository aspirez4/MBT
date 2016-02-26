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
                int[] a = new int[] { 1, 2 };
                int[] a1 = new int[] { 1 };
                int[][] b = new int[][] { a, a1 };
                int[][][] c = new int[][][] { b, b, b };
                string s = c.ToString();

                Person p = new Person { Name = "d", Age = 2 };
                string postBody = PythonUtils.JsonSerializer(p);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.GetAsync("/train?data=[[[1,1,],[2]]]");
                var response1 = await client.PostAsync("/train", new StringContent(postBody, Encoding.UTF8, "application/json"));

                var responseString = await response.Content.ReadAsStringAsync();
            }
        }

        public static string JsonSerializer(Person objectToSerialize)
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



   [DataContract]
   public class Person
    {
        [DataMember(Order=1)]
        public string Name { get; set; }
        [DataMember(Order=2)]
        public int Age { get; set; }
    }


}

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net.Http;

namespace MBTrading.Utils
{
    public class PythonUtils
    {
        public static async void  callTrainer()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://localhost:4567"); 
                int[] a = new int[] { 1, 2 };
                int[] a1 = new int[] { 1 };
                int[][] b = new int[][] { a, a1 };
                int[][][] c = new int[][][] { b, b, b };
                string s = c.ToString();

                var values = new Dictionary<string,string>
                {
                   { "data", "6" }
                };

                var content = new FormUrlEncodedContent(values);

                var response = await client.GetAsync("/train?data=[[[1,1,],[2]]]");
                //var response = await client.PostAsync("/train", content);

                var responseString = await response.Content.ReadAsStringAsync();
            }
        }



    }
}

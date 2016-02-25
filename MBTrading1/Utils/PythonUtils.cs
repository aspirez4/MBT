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
                int[] a = new int[] { 1, 2 };
                int[] a1 = new int[] { 1 };
                int[][] b = new int[][] { a, a1 };
                int[][][] c = new int[][][] { b, b, b };
                string s = c.ToString();
                var values = new Dictionary<string,string>
                {
                   { "data", s }
                };

                var content = new FormUrlEncodedContent(values);

                var response = await client.PostAsync("localhost:4567/train", content);

                var responseString = await response.Content.ReadAsStringAsync();
            }
        }



    }
}

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Boggle
{
    class Network
    {
        /// <summary>
        /// Initializes Http client for other Network methods.
        /// </summary>
        /// <returns></returns>
        public static HttpClient CreateClient()
        {
            //Create Client
            HttpClient client = new HttpClient();

            //Attaching Base Address to client.
            client.BaseAddress = new Uri("http://bogglecs3500s16.azurewebsites.net/BoggleService.svc/");

            //Clear headers, expect json format.
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            return client;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="playerToken"></param>
        /// <param name="word"></param>
        /// <returns></returns>
        internal static void Word(string playerToken, string word)
        {
            using (HttpClient client = CreateClient())
            {
                dynamic data = new ExpandoObject();
                data.UserToken = playerToken;
                data.Word = word;

                String url = String.Format("GameID");
                StringContent content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                HttpResponseMessage response = client.PostAsync(url, content).Result;
            }
        }

        /// <summary>
        /// POSTs time to server.
        /// If successful : returns GameID.
        /// Otherwise : returns null;
        /// </summary>
        /// <param name="playerToken"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string JoinGame(string playerToken, int time)
        {
            // Starts a HTTP client
            using (HttpClient client = CreateClient())
            {
                // Creates an ExpandoObject
                dynamic data = new ExpandoObject();

                // POSTs userToken and Time to the server.
                data.UserToken = playerToken;
                data.TimeLimit = time;

                // Retreives response from the server.
                StringContent content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                HttpResponseMessage response = client.PostAsync("games", content).Result;

                // Converts response from the server.
                string result = response.Content.ReadAsStringAsync().Result;
                dynamic expando = JsonConvert.DeserializeObject(result);

                // return the GameID if successfull, otherwise return null
                return response.IsSuccessStatusCode ? expando.GameID : null;
            }
        }

        /// <summary>
        /// POSTs nickname to server.
        /// If successful : returns user token.
        /// Otherwise : returns null;
        /// </summary>
        /// <param name="nickname"></param>
        /// <returns></returns>
        public static string CreateName(string nickname)
        {
            using (HttpClient client = CreateClient())
            {
                // Creates an ExpandoObject
                dynamic data = new ExpandoObject();
                data.Nickname = nickname;

                // Retreives response from the server.
                StringContent content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                HttpResponseMessage response = client.PostAsync("users", content).Result;

                // Converts response from the server.
                string result = response.Content.ReadAsStringAsync().Result;
                dynamic expando = JsonConvert.DeserializeObject(result);

                // return the userToken if successfull, otherwise return null
                return response.IsSuccessStatusCode ? expando.UserToken : null;
            }
        }
    }
}

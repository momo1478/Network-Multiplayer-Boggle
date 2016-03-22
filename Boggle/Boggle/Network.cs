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

        public static dynamic GetStatus(string GID)
        {
            using (HttpClient client = CreateClient())
            {
                HttpResponseMessage response = client.GetAsync("games/" + GID).Result;

                string result = response.Content.ReadAsStringAsync().Result;
                dynamic expando = JsonConvert.DeserializeObject(result);

                return expando;
            }
        }

        /// <summary>
        /// Using a PUT we send a word to the server, and wait for a response.
        /// </summary>
        /// <param name="playerToken"></param>
        /// <param name="word"></param>
        /// <returns></returns>
        internal static int PlayWord(string playerToken, string word , string GID)
        {
            using (HttpClient client = CreateClient())
            {
                dynamic data = new ExpandoObject();
                data.UserToken = playerToken;
                data.Word = word;

                StringContent content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                HttpResponseMessage response = client.PostAsync("games/" + GID, content).Result;

                string result = response.Content.ReadAsStringAsync().Result;
                dynamic expando = JsonConvert.DeserializeObject(result);

                return response.IsSuccessStatusCode ? expando.Score : 0;
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

                //Construct Expando
                data.UserToken = playerToken;
                data.TimeLimit = time;

                // Convert the data expando object into to a json object.
                StringContent content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                // Waits for a response from the server after POSTing.
                HttpResponseMessage response = client.PostAsync("games", content).Result;

                // Saves the response as a serialized string.
                string result = response.Content.ReadAsStringAsync().Result;
                // Converts the response from the server.
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

                //Construct Expando
                data.Nickname = nickname;

                
                StringContent content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                HttpResponseMessage response = client.PostAsync("users", content).Result;

               
                string result = response.Content.ReadAsStringAsync().Result;
                dynamic expando = JsonConvert.DeserializeObject(result);

                // return the userToken if successfull, otherwise return null
                return response.IsSuccessStatusCode ? expando.UserToken : null;
            }
        }
    }
}

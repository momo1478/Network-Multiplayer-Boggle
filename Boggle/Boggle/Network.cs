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

        public static string JoinGame(string playerToken, int time)
        {
            using (HttpClient client = CreateClient())
            {
                dynamic data = new ExpandoObject();
                data.UserToken = playerToken;
                data.TimeLimit = time;

                StringContent content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                HttpResponseMessage response = client.PostAsync("games", content).Result;

                string result = response.Content.ReadAsStringAsync().Result;
                dynamic expando = JsonConvert.DeserializeObject(result);

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
                dynamic data = new ExpandoObject();
                data.Nickname = nickname;

                StringContent content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                HttpResponseMessage response = client.PostAsync("users", content).Result;

                string result = response.Content.ReadAsStringAsync().Result;
                dynamic expando = JsonConvert.DeserializeObject(result);

                return response.IsSuccessStatusCode ? expando.UserToken : null;
            }
        }
    }
}

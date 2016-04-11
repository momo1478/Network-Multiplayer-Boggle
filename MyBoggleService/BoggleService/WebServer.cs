using CustomNetworking;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace Boggle
{
    public class WebServer
    {
        public static void Main()
        {
            new WebServer();
            Console.Read();
        }

        private TcpListener server;

        public WebServer()
        {
            server = new TcpListener(IPAddress.Any, 60000);//54321
            server.Start();
            server.BeginAcceptSocket(ConnectionRequested, null);
        }

        private void ConnectionRequested(IAsyncResult ar)
        {
            Socket s = server.EndAcceptSocket(ar);
            server.BeginAcceptSocket(ConnectionRequested, null);
            new HttpRequest(new StringSocket(s, new UTF8Encoding()));
        }
    }

    class HttpRequest
    {
        BoggleService Service = new BoggleService();
        private StringSocket ss;
        private int lineCount;
        private int contentLength;
        private string type;
        private string URL;
        private string GID;
        private string brief;

        public HttpRequest(StringSocket stringSocket)
        {
            this.ss = stringSocket;
            ss.BeginReceive(LineReceived, null);
        }

        private void LineReceived(string s, Exception e, object payload)
        {
            lineCount++;
            Console.WriteLine(s);
            if (s != null)
            {
                if (lineCount == 1)
                {
                    Regex r = new Regex(@"^(\S+)\s+(\S+)");
                    Match m = r.Match(s);
                    type = m.Groups[1].Value;
                    Console.WriteLine("Method: " + type);
                    URL = m.Groups[2].Value;
                    Console.WriteLine("URL: " + URL);
                }
                if (s.StartsWith("Content-Length:"))
                {
                    contentLength = Int32.Parse(s.Substring(16).Trim());
                }
                if (s == "\r")
                {
                    ss.BeginReceive(ContentReceived, null, contentLength);
                }
                else
                {
                    ss.BeginReceive(LineReceived, null);
                }
            }
        }

        private void ContentReceived(string s, Exception e, object payload)
        {
            //TODO: Fill in cases with actual methods.
            if (s != null)
            {
                string method = methodChooser();

                switch (method)
                {
                    case "CreateUser":
                        break;

                    case "JoinGame":
                        break;

                    case "CancelJoin":
                        break;

                    case "PlayWord":
                        break;

                    case "Status":
                        break;

                    case "StatusBrief":
                        break;

                    default:
                        break;
                }
            }
        }

        public string methodChooser()
        {
            

            if (type.Equals("POST"))
            {
                if (Regex.IsMatch(URL, "/BoggleService.svc/users")) { return "CreateUser"; }

                if (Regex.IsMatch(URL, "/BoggleService.svc/games")) { return "JoinGame"; }
            }
            else if (type.Equals("PUT"))
            {
                Match URLParams = Regex.Match(URL, "/BoggleService.svc/games/([0-9]+)");

                if (Regex.IsMatch(URL, "/BoggleService.svc/games/[0-9]+")) { GID = URLParams.Groups[1]?.Success == true ? URLParams.Groups[1].Value : "" ; return "PlayWord"; }

                if (Regex.IsMatch(URL, "/BoggleService.svc/games")) { return "CancelJoin"; }
            }
            else if (type.Equals("GET"))
            {
                Match URLParams = Regex.Match(URL, @"\/BoggleService.svc\/games\/([0-9]+)\?(brief=yes)?");

                if (URLParams.Groups[1]?.Success == true)
                {
                    GID = URLParams.Groups[1].Value;

                    if (URLParams.Groups[2]?.Success == true)
                    {
                        brief = URLParams.Groups[2].Value;
                        return "StatusBrief";
                    }

                    return "Status";
                }
            }

                return "";
        }

        private void Ignore(Exception e, object payload)
        {
        }

        // Helper methods
        private void CreateUser(string s)
        {
            UserInfo user = JsonConvert.DeserializeObject<UserInfo>(s);
            Console.WriteLine("Nickname = " + user.Nickname);



            // Call service method
            CreateUserReturn result = Service.CreateUser(user);

            string jsonResult =
                JsonConvert.SerializeObject(
                        new CreateUserReturn { UserToken = result.UserToken },
                        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            ss.BeginSend("HTTP/1.1 " + BoggleService.StatusString + "\n", Ignore, null);
            ss.BeginSend("Content-Type: application/json\n", Ignore, null);
            ss.BeginSend("Content-Length: " + jsonResult.Length + "\n", Ignore, null);
            ss.BeginSend("\r\n", Ignore, null);
            ss.BeginSend(jsonResult, (ex, py) => { ss.Shutdown(); }, null);
        }
        private void JoinGame(string s)
        {
            JoinGameArgs joinGame = JsonConvert.DeserializeObject<JoinGameArgs>(s);
            Console.WriteLine("UserToken = " + joinGame.UserToken);
            Console.WriteLine("TimeLimit = " + joinGame.TimeLimit);

            // Call service method
            JoinGameReturn result = Service.JoinGame(joinGame);

            string jsonResult =
                JsonConvert.SerializeObject(
                        new JoinGameReturn { GameID = result.GameID },
                        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            ss.BeginSend("HTTP/1.1 " + BoggleService.StatusString + "\n", Ignore, null);
            ss.BeginSend("Content-Type: application/json\n", Ignore, null);
            ss.BeginSend("Content-Length: " + jsonResult.Length + "\n", Ignore, null);
            ss.BeginSend("\r\n", Ignore, null);
            ss.BeginSend(jsonResult, (ex, py) => { ss.Shutdown(); }, null);
        }
        private void CancelJoinRequest(string s)
        {
            CancelGameArgs cancelJoin = JsonConvert.DeserializeObject<CancelGameArgs>(s);
            Console.WriteLine("UserToken = " + cancelJoin.UserToken);

            // Call service method
            Service.CancelJoinRequest(cancelJoin);

            ss.BeginSend("HTTP/1.1 " + BoggleService.StatusString + "\n", Ignore, null);
            ss.BeginSend("Content-Type: application/json\n", Ignore, null);
            // TODO : Find out what Content-Length should be.
            ss.BeginSend("Content-Length: " + 0 + "\n", Ignore, null);
            ss.BeginSend("\r\n", Ignore, null);
            // TODO : Find out if we should pass in null as the first parameter
            ss.BeginSend(null, (ex, py) => { ss.Shutdown(); }, null);
        }
        // TODO : Add a way to get the game ID out of the URL inside the ContentRecieved or method chooser.  
        private void PlayWord(string s, string GID)
        {
            PlayWordArgs playWord = JsonConvert.DeserializeObject<PlayWordArgs>(s);
            Console.WriteLine("UserToken = " + playWord.UserToken);
            Console.WriteLine("Word = " + playWord.Word);

            // Call service method
            PlayWordReturn result = Service.PlayWord(playWord,GID);

            string jsonResult =
                JsonConvert.SerializeObject(
                        new PlayWordReturn { Score = result.Score },
                        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            ss.BeginSend("HTTP/1.1 " + BoggleService.StatusString + "\n", Ignore, null);
            ss.BeginSend("Content-Type: application/json\n", Ignore, null);
            ss.BeginSend("Content-Length: " + jsonResult.Length + "\n", Ignore, null);
            ss.BeginSend("\r\n", Ignore, null);
            ss.BeginSend(jsonResult, (ex, py) => { ss.Shutdown(); }, null);
        }
        private void GameSatus(string GID)
        {
            // Call service method
            GetStatusReturn result = Service.Status(GID);

            string jsonResult =
                JsonConvert.SerializeObject(
                        result,
                        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            ss.BeginSend("HTTP/1.1 " + BoggleService.StatusString + "\n", Ignore, null);
            ss.BeginSend("Content-Type: application/json\n", Ignore, null);
            ss.BeginSend("Content-Length: " + jsonResult.Length + "\n", Ignore, null);
            ss.BeginSend("\r\n", Ignore, null);
            ss.BeginSend(jsonResult, (ex, py) => { ss.Shutdown(); }, null);
        }
        private void GameSatus(string GID, string Brief)
        {
            GetStatusReturn result = Service.StatusBrief(GID,Brief);

            string jsonResult =
                JsonConvert.SerializeObject(
                        result,
                        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            ss.BeginSend("HTTP/1.1 " + BoggleService.StatusString + "\n", Ignore, null);
            ss.BeginSend("Content-Type: application/json\n", Ignore, null);
            ss.BeginSend("Content-Length: " + jsonResult.Length + "\n", Ignore, null);
            ss.BeginSend("\r\n", Ignore, null);
            ss.BeginSend(jsonResult, (ex, py) => { ss.Shutdown(); }, null);
        }
    }

}
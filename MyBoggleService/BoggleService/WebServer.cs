using CustomNetworking;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using static System.Net.HttpStatusCode;

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
        private readonly object sync = new object();

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

                    Console.WriteLine();
                    Console.WriteLine("Method : " + type);

                    URL = m.Groups[2].Value;

                    if (URL.Equals("/"))
                    {
                        ContentReceived("", null, null);
                        Console.WriteLine("Omg it's THE API.");
                    }

                    Console.WriteLine();
                    Console.WriteLine("URL : " + URL);

                }
                if (s.StartsWith("Content-Length:"))
                {
                    contentLength = Int32.Parse(s.Substring(16).Trim());
                }
                if (s == "\r")
                {
                        if (type.Equals("GET"))
                        {
                        ContentReceived("", null, null);
                        Console.WriteLine("Omg it's a GET.");
                        }
                        else
                        {
                        ss.BeginReceive(ContentReceived, null, contentLength);
                        Console.WriteLine("Omg it's NOT A GET.");
                        }
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
            string method = methodChooser();
            if (s != null || method.Contains("Status"))
            {
                switch (method)
                {
                    case "CreateUser":
                        CreateUser(s);
                        break;

                    case "JoinGame":
                        JoinGame(s);
                        break;

                    case "CancelJoin":
                        CancelJoinRequest(s);
                        break;

                    case "PlayWord":
                        PlayWord(s, GID);
                        break;

                    case "Status":
                        GameSatus(GID);
                        break;

                    case "StatusBrief":
                        GameSatus(GID, brief);
                        break;
                    default:
                        if (Regex.IsMatch(URL, "/http://localhost:60000/", RegexOptions.IgnoreCase) || URL.Equals("/")) API();
                        else { Blank(); }
                        break;
                }
            }
        }

        public string methodChooser()
        {
            if (type.Equals("POST"))
            {
                if (Regex.IsMatch(URL, "/BoggleService.svc/users", RegexOptions.IgnoreCase)) { return "CreateUser"; }

                if (Regex.IsMatch(URL, "/BoggleService.svc/games", RegexOptions.IgnoreCase)) { return "JoinGame"; }
            }
            else if (type.Equals("PUT"))
            {
                Match URLParams = Regex.Match(URL, "/BoggleService.svc/games/([0-9]+)", RegexOptions.IgnoreCase);

                if (Regex.IsMatch(URL, "/BoggleService.svc/games/[0-9]+", RegexOptions.IgnoreCase)) { GID = URLParams.Groups[1]?.Success == true ? URLParams.Groups[1].Value : ""; return "PlayWord"; }

                if (Regex.IsMatch(URL, "/BoggleService.svc/games", RegexOptions.IgnoreCase)) { return "CancelJoin"; }
            }
            else if (type.Equals("GET"))
            {
                Match URLParams = Regex.Match(URL, @"\/BoggleService.svc\/games\/([A-Za-z0-9]+)(\?brief=([A-Za-z1-9]*))?$", RegexOptions.IgnoreCase);

                if (URLParams.Groups[1]?.Success == true)
                {
                    GID = URLParams.Groups[1].Value;

                    if (URLParams.Groups[2]?.Success == true)
                    {
                        brief = URLParams.Groups[3].Value;
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
            UserInfo user = null;
            CreateUserReturn result = null;
            try
            {
                user = JsonConvert.DeserializeObject<UserInfo>(s);
                Console.WriteLine("Nickname = " + user.Nickname);
                // Call service method
                result = Service.CreateUser(user);
            }
            catch (Exception)
            {
                BoggleService.SetStatus(BadRequest);
            }


            string jsonResult =
                JsonConvert.SerializeObject(
                        new CreateUserReturn { UserToken = result?.UserToken },
                        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            ss.BeginSend("HTTP/1.1 " + BoggleService.StatusString + "\r\n", Ignore, null);
            ss.BeginSend("Content-Type: application/json\r\n", Ignore, null);
            ss.BeginSend("Content-Length: " + jsonResult.Length + "\r\n", Ignore, null);
            ss.BeginSend("\r\n", Ignore, null);
            ss.BeginSend(jsonResult, (ex, py) => { ss.Shutdown(); }, null);
        }
        private void JoinGame(string s)
        {
            JoinGameArgs joinGame = null;
            JoinGameReturn result = null;
            try
            {
                joinGame = JsonConvert.DeserializeObject<JoinGameArgs>(s);
                Console.WriteLine("UserToken = " + joinGame.UserToken);
                Console.WriteLine("TimeLimit = " + joinGame.TimeLimit);
                // Call service method
                result = Service.JoinGame(joinGame);
            }
            catch (Exception)
            {
                BoggleService.SetStatus(BadRequest);
            }
            string jsonResult =
                JsonConvert.SerializeObject(
                        new JoinGameReturn { GameID = result?.GameID },
                        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            ss.BeginSend("HTTP/1.1 " + BoggleService.StatusString + "\r\n", Ignore, null);
            ss.BeginSend("Content-Type: application/json\r\n", Ignore, null);
            ss.BeginSend("Content-Length: " + jsonResult.Length + "\r\n", Ignore, null);
            ss.BeginSend("\r\n", Ignore, null);
            ss.BeginSend(jsonResult, (ex, py) => { ss.Shutdown(); }, null);
        }
        private void CancelJoinRequest(string s)
        {
            CancelGameArgs cancelJoin = null;
            try
            {
                cancelJoin = JsonConvert.DeserializeObject<CancelGameArgs>(s);
                Console.WriteLine("UserToken = " + cancelJoin.UserToken);
                // Call service method
                Service.CancelJoinRequest(cancelJoin);
            }
            catch (Exception)
            {
                BoggleService.SetStatus(BadRequest);
            }

            ss.BeginSend("HTTP/1.1 " + BoggleService.StatusString + "\r\n", Ignore, null);
            ss.BeginSend("Content-Type: application/json\r\n", Ignore, null);
            // TODO : Find out what Content-Length should be.
            ss.BeginSend("Content-Length: " + 0 + "\r\n", Ignore, null);
            ss.BeginSend("\r\n", Ignore, null);
            // TODO : Find out if we should pass in null as the first parameter
        }
        // TODO : Add a way to get the game ID out of the URL inside the ContentRecieved or method chooser.  
        private void PlayWord(string s, string GID)
        {
            PlayWordArgs playWord = null;
            PlayWordReturn result = null;
            try
            {
                playWord = JsonConvert.DeserializeObject<PlayWordArgs>(s);
                Console.WriteLine("UserToken = " + playWord.UserToken);
                Console.WriteLine("Word = " + playWord.Word);
                result = Service.PlayWord(playWord, GID);
            }
            catch (Exception)
            {
                BoggleService.SetStatus(BadRequest);
            }

            // Call service method

            string jsonResult =
                JsonConvert.SerializeObject(
                        new PlayWordReturn { Score = result?.Score },
                        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            ss.BeginSend("HTTP/1.1 " + BoggleService.StatusString + "\r\n", Ignore, null);
            ss.BeginSend("Content-Type: application/json\r\n", Ignore, null);
            ss.BeginSend("Content-Length: " + jsonResult.Length + "\r\n", Ignore, null);
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

            Console.WriteLine();
            Console.WriteLine("GameStatus was called");

            ss.BeginSend("HTTP/1.1 " + BoggleService.StatusString + "\r\n", Ignore, null);
            ss.BeginSend("Content-Type: application/json\r\n", Ignore, null);
            ss.BeginSend("Content-Length: " + jsonResult.Length + "\r\n", Ignore, null);
            ss.BeginSend("\r\n", Ignore, null);
            ss.BeginSend(jsonResult, (ex, py) => { ss.Shutdown(); }, null);
        }
        private void GameSatus(string GID, string Brief)
        {
            GetStatusReturn result = Service.StatusBrief(GID, Brief);

            string jsonResult =
                JsonConvert.SerializeObject(
                        result,
                        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            Console.WriteLine();
            Console.WriteLine("GameStatusBrief was called");

            ss.BeginSend("HTTP/1.1 " + BoggleService.StatusString + "\r\n", Ignore, null);
            ss.BeginSend("Content-Type: application/json\r\n", Ignore, null);
            ss.BeginSend("Content-Length: " + jsonResult.Length + "\r\n", Ignore, null);
            ss.BeginSend("\r\n", Ignore, null);
            ss.BeginSend(jsonResult, (ex, py) => { ss.Shutdown(); }, null);
        }
        private void API()
        {
            string api = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"\index.html");
            BoggleService.SetStatus(OK);
            ss.BeginSend("HTTP/1.1 " + BoggleService.StatusString + "\r\n", Ignore, null);
            ss.BeginSend("Content-Type: text/html\r\n", Ignore, null);
            ss.BeginSend("\r\n", Ignore, null);
            ss.BeginSend(api, (ex, py) => { ss.Shutdown(); }, null);
        }
        private void Blank()
        {
            BoggleService.SetStatus(BadRequest);
            ss.BeginSend("HTTP/1.1 " + BoggleService.StatusString + "\r\n", Ignore, null);
            ss.BeginSend("Content-Type: text/html\r\n", Ignore, null);
            ss.BeginSend("\r\n", Ignore, null);
        }

    }

}
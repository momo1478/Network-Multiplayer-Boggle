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
            if (s != null)
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
        }

        private void Ignore(Exception e, object payload)
        {
        }
    }

    public class Person
    {
        public String Name { get; set; }
        public String Eyes { get; set; }
    }
}
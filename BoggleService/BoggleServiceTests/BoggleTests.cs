using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Dynamic;
using static System.Net.HttpStatusCode;
using System.Diagnostics;

namespace Boggle
{
    /// <summary>
    /// Provides a way to start and stop the IIS web server from within the test
    /// cases. If something prevents the test cases from stopping the web server,
    /// subsequent tests may not work properly until the stray process is killed
    /// manually.
    /// </summary>
    public static class IISAgent
    {
        // Reference to the running process
        private static Process process = null;

        /// <summary>
        /// Starts IIS
        /// </summary>
        public static void Start(string arguments)
        {
            if (process == null)
            {
                ProcessStartInfo info = new ProcessStartInfo(Properties.Resources.IIS_EXECUTABLE, arguments);
                info.WindowStyle = ProcessWindowStyle.Minimized;
                info.UseShellExecute = false;
                process = Process.Start(info);
            }
        }

        /// <summary>
        ///  Stops IIS
        /// </summary>
        public static void Stop()
        {
            if (process != null)
            {
                process.Kill();
            }
        }
    }

    [TestClass]
    public class BoggleTests
    {
        /// <summary>
        /// This is automatically run prior to all the tests to start the server
        /// </summary>
        [ClassInitialize()]
        public static void StartIIS(TestContext testContext)
        {
            IISAgent.Start(@"/site:""BoggleService"" /apppool:""Clr4IntegratedAppPool"" /config:""..\..\..\.vs\config\applicationhost.config""");
        }

        /// <summary>
        /// This is automatically run when all tests have completed to stop the server
        /// </summary>
        [ClassCleanup()]
        public static void StopIIS()
        {
            IISAgent.Stop();
        }

        //private RestTestClient client = new RestTestClient("http://localhost:60000/");
        private RestTestClient client = new RestTestClient("http://bogglecs3500s16.azurewebsites.net/");

        /// <summary>
        /// Create User POST test for response status on valid Nickname.
        /// </summary>
        [TestMethod]
        public void CreateUserTest()
        {
            dynamic expando = new ExpandoObject();
            expando.Nickname = "lol";
            Response r = client.DoPostAsync("Users",expando).Result;
            Assert.AreEqual(Created, r.Status);
        }
        /// <summary>
        /// Create User POST test for response status on null.
        /// </summary>
        [TestMethod]
        public void CreateUserTestNull()
        {
            dynamic expando = new ExpandoObject();
            expando.Nickname = "";
            Response r = client.DoPostAsync("Users", expando).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        /// <summary>
        /// Join game POST test for response status on valid UserToken and Time Limit.
        /// </summary>
        [TestMethod]
        public void JoinGame()
        {
            bool success = false;
            dynamic expandoUser = new ExpandoObject();
            expandoUser.Nickname = "Player1";
            Response r_player1 = client.DoPostAsync("Users", expandoUser).Result;
            Assert.AreEqual(Created, r_player1.Status);
            expandoUser.Nickname = "Player2";
            Response r_player2 = client.DoPostAsync("Users", expandoUser).Result;
            Assert.AreEqual(Created, r_player2.Status);

            dynamic expandoJoinGame = new ExpandoObject();
            expandoJoinGame.UserToken = r_player1.Data.UserToken;
            expandoJoinGame.TimeLimit = 25;
            Response r_JoinGame1 = client.DoPostAsync("Users", expandoJoinGame).Result;
            if(r_JoinGame1.Status.)
            Assert.IsTrue(Created, r_JoinGame1.Status);
        }
        /// <summary>
        /// Join game POST test for response status on null.
        /// </summary>
        [TestMethod]
        public void JoinGameNull()
        {
            dynamic expando = new ExpandoObject();
            expando.Nickname = "";
            Response r = client.DoPostAsync("Users", expando).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }
    }
    
}

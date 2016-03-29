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

        private RestTestClient client = new RestTestClient("http://localhost:60000/");
        //private RestTestClient client = new RestTestClient("http://bogglecs3500s16.azurewebsites.net/");

        /// <summary>
        /// Create User POST test for response status on valid Nickname.
        /// </summary>
        [TestMethod]
        public void CreateUserTest()
        {
            dynamic expando = new ExpandoObject();
            expando.Nickname = "nickName";
            Response r = client.DoPostAsync("Users",expando).Result;
            Assert.AreEqual(Created, r.Status);
        }
        /// <summary>
        /// Create User POST test for response status on null.
        /// </summary>
        [TestMethod]
        public void CreateUserNullTest()
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
        public void JoinGameTest()
        {
            bool success = false;
            dynamic expandoUser = new ExpandoObject();
            expandoUser.Nickname = "Player1";
            Response r_player1 = client.DoPostAsync("Users", expandoUser).Result;
            Assert.AreEqual(Created, r_player1.Status);

            dynamic expandoJoinGame = new ExpandoObject();
            expandoJoinGame.UserToken = r_player1.Data.UserToken;
            expandoJoinGame.TimeLimit = 25;
            Response r_JoinGame1 = client.DoPostAsync("games", expandoJoinGame).Result;
            if (r_JoinGame1.Status == Accepted || r_JoinGame1.Status == Created) success = true;
            Assert.IsTrue(success);
        }
        /// <summary>
        /// Join game POST test for response status on invalid TimeLimit.
        /// </summary>
        [TestMethod]
        public void JoinGameInvalidTimeTest()
        {
            dynamic expandoUser = new ExpandoObject();
            expandoUser.Nickname = "Player1";
            Response r_player1 = client.DoPostAsync("Users", expandoUser).Result;
            Assert.AreEqual(Created, r_player1.Status);

            expandoUser.Nickname = "Player2";
            Response r_player2 = client.DoPostAsync("Users", expandoUser).Result;
            Assert.AreEqual(Created, r_player1.Status);

            dynamic expandoJoinGame = new ExpandoObject();
            expandoJoinGame.UserToken = r_player1.Data.UserToken;
            expandoJoinGame.TimeLimit = 4;
            Response r_JoinGame1 = client.DoPostAsync("games", expandoJoinGame).Result;
            Assert.AreEqual(Forbidden, r_JoinGame1.Status);
            expandoJoinGame.UserToken = r_player2.Data.UserToken;
            expandoJoinGame.TimeLimit = 121;
            Response r_JoinGame2 = client.DoPostAsync("games", expandoJoinGame).Result;
            Assert.AreEqual(Forbidden, r_JoinGame2.Status);
        }
        /// <summary>
        /// Join game POST test for response status on existing player in game.
        /// </summary>
        [TestMethod]
        public void JoinGameExistingPlayerTest()
        {
            bool success = false;
            dynamic expandoUser = new ExpandoObject();
            expandoUser.Nickname = "Player1";
            Response r_player1 = client.DoPostAsync("Users", expandoUser).Result;
            Assert.AreEqual(Created, r_player1.Status);

            dynamic expandoJoinGame = new ExpandoObject();
            expandoJoinGame.UserToken = r_player1.Data.UserToken;
            expandoJoinGame.TimeLimit = 5;
            Response r_JoinGame1 = client.DoPostAsync("games", expandoJoinGame).Result;
            Response r_JoinGame2 = client.DoPostAsync("games", expandoJoinGame).Result;
            Response r_JoinGame3 = client.DoPostAsync("games", expandoJoinGame).Result;
            if (r_JoinGame1.Status == Conflict || r_JoinGame2.Status == Conflict || r_JoinGame3.Status == Conflict ) success = true;
            Assert.IsTrue(success);
        }

        /// <summary>
        /// Cancel join game PUT test.
        /// </summary>
        [TestMethod]
        public void CancelJoinRequestTest()
        {
            dynamic expandoUser = new ExpandoObject();
            expandoUser.Nickname = "Player1";
            Response r_player1 = client.DoPostAsync("Users", expandoUser).Result;
            Assert.AreEqual(Created, r_player1.Status);

            expandoUser.Nickname = "Player2";
            Response r_player2 = client.DoPostAsync("Users", expandoUser).Result;
            Assert.AreEqual(Created, r_player2.Status);


            dynamic expandoJoinGame = new ExpandoObject();
            expandoJoinGame.UserToken = r_player1.Data.UserToken;
            expandoJoinGame.TimeLimit = 5;
            Response r_JoinGame1 = client.DoPostAsync("games", expandoJoinGame).Result;
            Response r_CancelGame1 = NotFound;
            if (r_JoinGame1.Status == Accepted)
            {
                dynamic expandoCancelJoin = new ExpandoObject();
                expandoCancelJoin.UserToken = r_player1.Data.UserToken;
                r_CancelGame1 = client.DoPutAsync(expandoCancelJoin, "games").Result;
            }

            expandoJoinGame.UserToken = r_player2.Data.UserToken;
            expandoJoinGame.TimeLimit = 5;
            Response r_JoinGame2 = client.DoPostAsync("games", expandoJoinGame).Result;
            Response r_CancelGame2 = NotFound;
            if (r_JoinGame2.Status == Accepted)
            {
                dynamic expandoCancelJoin = new ExpandoObject();
                expandoCancelJoin.UserToken = r_player2.Data.UserToken;
                r_CancelGame2 = client.DoPutAsync(expandoCancelJoin, "games").Result;
            }

            Assert.IsTrue(r_CancelGame1.Status == OK || r_CancelGame2.Status == OK);
        }
        /// <summary>
        /// Cancel join game PUT test on invalid player.
        /// </summary>
        [TestMethod]
        public void CancelJoinRequestInvalidPlayerTest()
        {
            dynamic expandoUser = new ExpandoObject();
            expandoUser.Nickname = "Player1";
            Response r_player1 = client.DoPostAsync("Users", expandoUser).Result;
            Assert.AreEqual(Created, r_player1.Status);

            dynamic expandoCancelJoin = new ExpandoObject();
            expandoCancelJoin.UserToken = r_player1.Data.UserToken;
            Response r_CancelGame = client.DoPutAsync(expandoCancelJoin, "games").Result;
            Assert.AreEqual(Forbidden, r_CancelGame.Status);
        }
        /// <summary>
        /// Play word PUT test.
        /// </summary>
        [TestMethod]
        public void PlayWordTest()
        {
            bool success = false;

            dynamic expandoUser = new ExpandoObject();
            expandoUser.Nickname = "Player1";
            Response r_player1 = client.DoPostAsync("Users", expandoUser).Result;
            Assert.AreEqual(Created, r_player1.Status);

            expandoUser.Nickname = "Player2";
            Response r_player2 = client.DoPostAsync("Users", expandoUser).Result;
            Assert.AreEqual(Created, r_player1.Status);

            dynamic expandoJoinGame = new ExpandoObject();
            expandoJoinGame.UserToken = r_player1.Data.UserToken;
            expandoJoinGame.TimeLimit = 5;
            //Join first player
            Response r_JoinGame1 = client.DoPostAsync("games", expandoJoinGame).Result;
            if(r_JoinGame1.Status == Created)
            {
                dynamic expandoPlayWord = new ExpandoObject();
                expandoPlayWord.UserToken = r_player1.Data.UserToken;
                expandoPlayWord.Word = "Hi";
                Response r_PlayWord = client.DoPutAsync(expandoPlayWord,"games/" + r_JoinGame1.Data.GameID).Result;
                if(r_PlayWord.Status == OK) success = true;
            }

            //Join second player
            expandoJoinGame.UserToken = r_player2.Data.UserToken;
            expandoJoinGame.TimeLimit = 120;
            Response r_JoinGame2 = client.DoPostAsync("games", expandoJoinGame).Result;
            if (r_JoinGame2.Status == Created)
            {
                dynamic expandoPlayWord = new ExpandoObject();
                expandoPlayWord.UserToken = r_player2.Data.UserToken;
                expandoPlayWord.Word = "Hi";
                Response r_PlayWord = client.DoPutAsync(expandoPlayWord, "games/" + r_JoinGame2.Data.GameID).Result;
                if (r_PlayWord.Status == OK) success = true;
            }
            Assert.IsTrue(success);
        }
        /// <summary>
        /// Play word PUT test on null word.
        /// </summary>
        [TestMethod]
        public void PlayWordNullWordTest()
        {
            bool success = false;

            dynamic expandoUser = new ExpandoObject();
            expandoUser.Nickname = "Player1";
            Response r_player1 = client.DoPostAsync("Users", expandoUser).Result;
            Assert.AreEqual(Created, r_player1.Status);

            expandoUser.Nickname = "Player2";
            Response r_player2 = client.DoPostAsync("Users", expandoUser).Result;
            Assert.AreEqual(Created, r_player1.Status);

            dynamic expandoJoinGame = new ExpandoObject();
            expandoJoinGame.UserToken = r_player1.Data.UserToken;
            expandoJoinGame.TimeLimit = 5;
            //Join first player
            Response r_JoinGame1 = client.DoPostAsync("games", expandoJoinGame).Result;
            if (r_JoinGame1.Status == Created)
            {
                dynamic expandoPlayWord = new ExpandoObject();
                expandoPlayWord.UserToken = r_player1.Data.UserToken;
                expandoPlayWord.Word = "";
                Response r_PlayWord = client.DoPutAsync(expandoPlayWord, "games/" + r_JoinGame1.Data.GameID).Result;
                if (r_PlayWord.Status == Forbidden) success = true;
            }

            //Join second player
            expandoJoinGame.UserToken = r_player2.Data.UserToken;
            expandoJoinGame.TimeLimit = 120;
            Response r_JoinGame2 = client.DoPostAsync("games", expandoJoinGame).Result;
            if (r_JoinGame2.Status == Created)
            {
                dynamic expandoPlayWord = new ExpandoObject();
                expandoPlayWord.UserToken = r_player2.Data.UserToken;
                expandoPlayWord.Word = "";
                Response r_PlayWord = client.DoPutAsync(expandoPlayWord, "games/" + r_JoinGame2.Data.GameID).Result;
                if (r_PlayWord.Status == Forbidden) success = true;
            }
            Assert.IsTrue(success);
        }

        /// <summary>
        /// Play word PUT test on invalid UserToken.
        /// </summary>
        [TestMethod]
        public void PlayWordInvalidUserTokenTest()
        {
            bool success = false;

            dynamic expandoUser = new ExpandoObject();
            expandoUser.Nickname = "Player1";
            Response r_player1 = client.DoPostAsync("Users", expandoUser).Result;
            Assert.AreEqual(Created, r_player1.Status);

            expandoUser.Nickname = "Player2";
            Response r_player2 = client.DoPostAsync("Users", expandoUser).Result;
            Assert.AreEqual(Created, r_player1.Status);

            dynamic expandoJoinGame = new ExpandoObject();
            expandoJoinGame.UserToken = r_player1.Data.UserToken;
            expandoJoinGame.TimeLimit = 5;
            //Join first player
            Response r_JoinGame1 = client.DoPostAsync("games", expandoJoinGame).Result;
            if (r_JoinGame1.Status == Created)
            {
                dynamic expandoPlayWord = new ExpandoObject();
                expandoPlayWord.UserToken = "";
                expandoPlayWord.Word = "Hi";
                Response r_PlayWord = client.DoPutAsync(expandoPlayWord, "games/" + r_JoinGame1.Data.GameID).Result;
                if (r_PlayWord.Status == Forbidden) success = true;
            }

            //Join second player
            expandoJoinGame.UserToken = r_player2.Data.UserToken;
            expandoJoinGame.TimeLimit = 120;
            Response r_JoinGame2 = client.DoPostAsync("games", expandoJoinGame).Result;
            if (r_JoinGame2.Status == Created)
            {
                dynamic expandoPlayWord = new ExpandoObject();
                expandoPlayWord.UserToken = "";
                expandoPlayWord.Word = "Hi";
                Response r_PlayWord = client.DoPutAsync(expandoPlayWord, "games/" + r_JoinGame2.Data.GameID).Result;
                if (r_PlayWord.Status == Forbidden) success = true;
            }
            Assert.IsTrue(success);
        }
        /// <summary>
        /// Play word PUT test on invalid GameID.
        /// </summary>
        [TestMethod]
        public void PlayWordInvalidGameIDTest()
        {
            bool success = false;

            dynamic expandoUser = new ExpandoObject();
            expandoUser.Nickname = "Player1";
            Response r_player1 = client.DoPostAsync("Users", expandoUser).Result;
            Assert.AreEqual(Created, r_player1.Status);

            expandoUser.Nickname = "Player2";
            Response r_player2 = client.DoPostAsync("Users", expandoUser).Result;
            Assert.AreEqual(Created, r_player1.Status);

            dynamic expandoJoinGame = new ExpandoObject();
            expandoJoinGame.UserToken = r_player1.Data.UserToken;
            expandoJoinGame.TimeLimit = 5;
            //Join first player
            Response r_JoinGame1 = client.DoPostAsync("games", expandoJoinGame).Result;
            if (r_JoinGame1.Status == Created)
            {
                dynamic expandoPlayWord = new ExpandoObject();
                expandoPlayWord.UserToken = r_player1.Data.UserToken;
                expandoPlayWord.Word = "Hi";
                Response r_PlayWord = client.DoPutAsync(expandoPlayWord, "games/" + "invalidID").Result;
                if (r_PlayWord.Status == Forbidden) success = true;
            }

            //Join second player
            expandoJoinGame.UserToken = r_player2.Data.UserToken;
            expandoJoinGame.TimeLimit = 120;
            Response r_JoinGame2 = client.DoPostAsync("games", expandoJoinGame).Result;
            if (r_JoinGame2.Status == Created)
            {
                dynamic expandoPlayWord = new ExpandoObject();
                expandoPlayWord.UserToken = r_player2.Data.UserToken;
                expandoPlayWord.Word = "Hi";
                Response r_PlayWord = client.DoPutAsync(expandoPlayWord, "games/" + "invalidID").Result;
                if (r_PlayWord.Status == Forbidden) success = true;
            }
            Assert.IsTrue(success);
        }
        
        /// <summary>
        /// Game Status Get test.
        /// </summary>
        [TestMethod]
        public void GameStatusTest()
        { 
            dynamic expandoUser = new ExpandoObject();
            expandoUser.Nickname = "Player1";
            Response r_player1 = client.DoPostAsync("Users", expandoUser).Result;
            Assert.AreEqual(Created, r_player1.Status);

            expandoUser.Nickname = "Player2";
            Response r_player2 = client.DoPostAsync("Users", expandoUser).Result;
            Assert.AreEqual(Created, r_player1.Status);

            dynamic expandoJoinGame = new ExpandoObject();
            expandoJoinGame.UserToken = r_player1.Data.UserToken;
            expandoJoinGame.TimeLimit = 5;
            Response r_JoinGame1 = client.DoPostAsync("games", expandoJoinGame).Result;

            Response r_GameStatus = client.DoGetAsync("games/" + r_JoinGame1.Data.GameID).Result;
            Assert.AreEqual(OK, r_GameStatus.Status);
        }
        /// <summary>
        /// Game Status Get test on invalidGameID.
        /// </summary>
        [TestMethod]
        public void GameStatusInvalidGameIDTest()
        {
            dynamic expandoUser = new ExpandoObject();
            expandoUser.Nickname = "Player1";
            Response r_player1 = client.DoPostAsync("Users", expandoUser).Result;
            Assert.AreEqual(Created, r_player1.Status);

            expandoUser.Nickname = "Player2";
            Response r_player2 = client.DoPostAsync("Users", expandoUser).Result;
            Assert.AreEqual(Created, r_player1.Status);

            dynamic expandoJoinGame = new ExpandoObject();
            expandoJoinGame.UserToken = r_player1.Data.UserToken;
            expandoJoinGame.TimeLimit = 5;
            Response r_JoinGame1 = client.DoPostAsync("games", expandoJoinGame).Result;

            Response r_GameStatus = client.DoGetAsync("games/" + "invalidID").Result;
            Assert.AreEqual(Forbidden, r_GameStatus.Status);
        }


    }

}

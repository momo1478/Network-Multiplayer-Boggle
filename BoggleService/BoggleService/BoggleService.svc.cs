using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Net;
using System.ServiceModel.Web;
using static System.Net.HttpStatusCode;

namespace Boggle
{
    public class BoggleService : IBoggleService
    {
        private static readonly Dictionary<String, UserInfo> users = new Dictionary<String, UserInfo>();
        private static readonly Dictionary<int, BoggleGame> games = new Dictionary<int, BoggleGame>();
        private static readonly object sync = new object();

        private static int GameIDCounter = 1;

        /// <summary>
        /// The most recent call to SetStatus determines the response code used when
        /// an http response is sent.
        /// </summary>
        /// <param name="status"></param>
        private static void SetStatus(HttpStatusCode status)
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = status;
        }

        /// <summary>
        /// Returns a Stream version of index.html.
        /// </summary>
        /// <returns></returns>
        public Stream API()
        {
            SetStatus(OK);
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";
            return File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "index.html");
        }

        public CreateUserReturn CreateUser(UserInfo user)
        {
            lock (sync)
            {
                if (user.Nickname == null || user.Nickname.Trim().Length == 0)
                {
                    SetStatus(Forbidden);
                    return null;
                }
                else
                {
                    SetStatus(Created);
                    string userID = Guid.NewGuid().ToString();
                    users.Add(userID, user);

                    return new CreateUserReturn() { UserToken = userID };
                }
            }
        }

        public JoinGameReturn JoinGame(JoinGameArgs args)
        {

            lock (sync)
            {
                if (!games.ContainsKey(GameIDCounter))
                {
                    //create pending game.
                    BoggleGame createdGame = new BoggleGame();
                    createdGame.GameState = "pending";
                    createdGame.GameID = GameIDCounter;

                    games.Add(GameIDCounter, createdGame);
                }

                Guid outR;
                //See if the userToken is Valid and timeLimit is within bounds
                if (Guid.TryParseExact(args.UserToken, "D", out outR) && users.ContainsKey(args.UserToken) && args.TimeLimit >= 5 && args.TimeLimit <= 120)
                {
                    //If game exists and Player 1 is in. 
                    if (games.ContainsKey(GameIDCounter) && games[GameIDCounter]?.Player1 != null)
                    {
                        //If UserToken is identical to one that is in the game we have CONFLICT.
                        if (games[GameIDCounter].Player1.UserToken.Equals(args.UserToken))
                        {
                            SetStatus(Conflict);
                            return null;
                        }
                        //If UserToken is not identical to the one in the game we have CREATED the game.
                        else
                        {
                            SetStatus(Created);

                            games[GameIDCounter].Player2 = new Player() { UserToken = args.UserToken, Nickname = users[args.UserToken].Nickname };
                            games[GameIDCounter].TimeLimit = (games[GameIDCounter].TimeLimit + args.TimeLimit) / 2;
                            games[GameIDCounter].GameState = "active";

                            games[GameIDCounter].GameTimer.Start();

                            return new JoinGameReturn() { GameID = GameIDCounter++.ToString() };
                        }
                    }
                    else
                    {
                        //If there are no players in the game. We ACCEPTED him as the first player.
                        SetStatus(Accepted);

                        games[GameIDCounter].Player1 = new Player() { UserToken = args.UserToken, Nickname = users[args.UserToken].Nickname };
                        games[GameIDCounter].TimeLimit = args.TimeLimit;

                        return new JoinGameReturn() { GameID = GameIDCounter.ToString() };
                    }
                }
                else
                {
                    //If the userToken is invalid or the timelimit is out of bounds, this request is FORBIDDEN.
                    SetStatus(Forbidden);
                    return null;
                }

            }

        }

        public PlayWordReturn PlayWord(PlayWordArgs args)
        {
            lock (sync)
            {
                SetStatus(OK);
                return null;
            }
        }

        ///// <summary>
        ///// Demo.  You can delete this.
        ///// </summary>
        //public int GetFirst(IList<int> list)
        //{
        //    SetStatus(OK);
        //    return list[0];
        //}

        ///// <summary>
        ///// Demo.  You can delete this.
        ///// </summary>
        ///// <returns></returns>
        //public IList<int> Numbers(string n)
        //{
        //    int index;
        //    if (!Int32.TryParse(n, out index) || index < 0)
        //    {
        //        SetStatus(Forbidden);
        //        return null;
        //    }
        //    else
        //    {
        //        List<int> list = new List<int>();
        //        for (int i = 0; i < index; i++)
        //        {
        //            list.Add(i);
        //        }
        //        SetStatus(OK);
        //        return list;
        //    }
        //}


    }
}

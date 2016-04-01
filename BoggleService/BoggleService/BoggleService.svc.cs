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

                            //games[ActiveGameID].GameTimer.Start();
                            // TODO: Set the timelimit.

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

        public void CancelJoinRequest(JoinGameArgs args)
        {
            lock (sync)
            {
                Guid outR;
                if (Guid.TryParseExact(args.UserToken, "D", out outR) && users.ContainsKey(args.UserToken))
                {
                    if (games.ContainsKey(GameIDCounter) && games[GameIDCounter].GameState.Equals("pending") && games[GameIDCounter].Player1.UserToken.Equals(args.UserToken))
                    {
                        games.Remove(GameIDCounter);
                        SetStatus(OK);
                        return;
                    }
                }
                SetStatus(Forbidden);
                return;
            }
        }

        public PlayWordReturn PlayWord(PlayWordArgs args, string GameID)
        {
            int intID;
            lock (sync)
            {
                args.Word = args.Word?.ToUpper() ?? "";

                // checks for forbidden
                if (args?.Word != null && args.Word.Trim().Length != 0 && int.TryParse(GameID, out intID) && games.ContainsKey(intID) && (games[intID].Player1.UserToken.Equals(args.UserToken) || games[intID].Player2.UserToken.Equals(args.UserToken)))
                {
                    //who is submiting player 1 or 2
                    int player = games[intID].Player1.UserToken.Equals(args.UserToken) ? 1 : 2;

                    if (games[intID].GameState.Equals("active"))
                    {
                        int wordScore;
                        // can be formed and in dictionary
                        if (games[intID].Board.CanBeFormed(args.Word) && isWord(args.Word))
                        {
                            SetStatus(OK);
                            if (player == 1)
                            {
                                if (games[intID].Player1.WordsPlayed.Exists(x => x.Word.Equals(args.Word)))
                                {
                                    wordScore = 0;
                                }
                                else
                                {
                                    wordScore = games[intID].Player1.WordScore(args.Word);
                                    games[intID].Player1.Score += wordScore;
                                }
                                games[intID].Player1.WordsPlayed.Add(new Words() { Word = args.Word, Score = wordScore });
                            }
                            else //player 2
                            {
                                if (games[intID].Player2.WordsPlayed.Exists(x => x.Word.Equals(args.Word)))
                                {
                                    wordScore = 0;
                                }
                                else
                                {
                                    wordScore = games[intID].Player2.WordScore(args.Word);
                                    games[intID].Player2.Score += wordScore;
                                }
                                games[intID].Player2.WordsPlayed.Add(new Words() { Word = args.Word, Score = wordScore });
                            }

                            return new PlayWordReturn() { Score = wordScore };

                        }
                        else
                        {
                            SetStatus(OK);

                            if (player == 1)
                            {
                                if (games[intID].Player1.WordsPlayed.Exists(x => x.Word.Equals(args.Word)))
                                {
                                    wordScore = 0;
                                }
                                else
                                {
                                    wordScore = -1;
                                    games[intID].Player1.Score += wordScore;
                                }
                                games[intID].Player1.WordsPlayed.Add(new Words() { Word = args.Word, Score = wordScore });
                            }
                            else // Player 2
                            {

                                if (games[intID].Player2.WordsPlayed.Exists(x => x.Word.Equals(args.Word)))
                                {
                                    wordScore = 0;
                                }
                                else
                                {

                                    wordScore = -1;
                                    games[intID].Player2.Score += wordScore;
                                }
                                games[intID].Player2.WordsPlayed.Add(new Words() { Word = args.Word, Score = wordScore });
                            }

                            return new PlayWordReturn() { Score = wordScore };
                        }

                    }
                    else
                    {
                        SetStatus(Conflict);
                        return null;
                    }
                }
                SetStatus(Forbidden);
                return null;
            }

        }
        private static bool isWord(string word)
        {
            using (TextReader reader = new StreamReader(File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "dictionary.txt")))
            {
                while (!((StreamReader)reader).EndOfStream)
                {
                    if (reader.ReadLine().Equals(word.ToUpper()))
                        return true;               
                }
                return false;
            }
        }

        public GetStatusReturn Status(string GameID)
        {
            lock (sync)
            {
            int intID;

                if (int.TryParse(GameID, out intID) && games.ContainsKey(intID))
                {
                    int? updateTimeLeft = games[intID].TimeLeft;
                    if (games[intID].GameState.Equals("pending"))
                    {
                        SetStatus(OK);
                        return new GetStatusReturn() { GameState = games[intID].GameState };
                    }
                else if (games[intID].GameState.Equals("active"))
                {
                    SetStatus(OK);
                    return new GetStatusReturn()
                    {
                        Board = games[intID].Board.ToString(),
                        TimeLimit = games[intID].TimeLimit,
                        TimeLeft = games[intID].TimeLeft,

                        GameState = games[intID].GameState,
                        Player1 = new PlayerDump() { Nickname = games[intID].Player1.Nickname, Score = games[intID].Player1.Score },
                        Player2 = new PlayerDump() { Nickname = games[intID].Player2.Nickname, Score = games[intID].Player2.Score },
                    };
                }
                else if (games[intID].GameState.Equals("completed"))
                {
                    SetStatus(OK);
                    return new GetStatusReturn()
                    {
                        Board = games[intID].Board.ToString(),
                        TimeLimit = games[intID].TimeLimit,
                        TimeLeft = games[intID].TimeLeft,

                        GameState = games[intID].GameState,
                        Player1 = new PlayerDump() { Nickname = games[intID].Player1.Nickname, Score = games[intID].Player1.Score, WordsPlayed = games[intID].Player1.WordsPlayed },
                        Player2 = new PlayerDump() { Nickname = games[intID].Player2.Nickname, Score = games[intID].Player2.Score, WordsPlayed = games[intID].Player2.WordsPlayed }
                    };
                }
            }
            SetStatus(Forbidden);
            return null;
            }
        }



        public GetStatusReturn StatusBrief(string GameID, string brief)
        {
            lock (sync)
            {
                int intID;
                if (int.TryParse(GameID, out intID) && games.ContainsKey(intID))
                {
                    int? updateTimeLeft = games[intID].TimeLeft;
                    if (games[intID].GameState.Equals("pending"))
                    {
                        SetStatus(OK);
                        return new GetStatusReturn() { GameState = games[intID].GameState };
                    }
                    else if (brief != null && brief.ToLower().Equals("yes"))
                    {
                        if (games[intID].GameState.Equals("active") || games[intID].GameState.Equals("completed"))
                        {
                            SetStatus(OK);
                            return new GetStatusReturn()
                            {
                                TimeLeft = games[intID].TimeLeft,
                                GameState = games[intID].GameState,
                                Player1 = new PlayerDump() { Score = games[intID].Player1.Score },
                                Player2 = new PlayerDump() { Score = games[intID].Player2.Score }
                            };
                        }
                    }
                    else if (games[intID].GameState.Equals("active"))
                    {
                        SetStatus(OK);
                        return new GetStatusReturn()
                        {
                            Board = games[intID].Board.ToString(),
                            TimeLimit = games[intID].TimeLimit,
                            TimeLeft = games[intID].TimeLeft,

                            GameState = games[intID].GameState,
                            Player1 = new PlayerDump() { Nickname = games[intID].Player1.Nickname, Score = games[intID].Player1.Score },
                            Player2 = new PlayerDump() { Nickname = games[intID].Player2.Nickname, Score = games[intID].Player2.Score },
                        };
                    }
                    else if (games[intID].GameState.Equals("completed"))
                    {
                        SetStatus(OK);
                        return new GetStatusReturn()
                        {
                            Board = games[intID].Board.ToString(),
                            TimeLimit = games[intID].TimeLimit,
                            TimeLeft = games[intID].TimeLeft,

                            GameState = games[intID].GameState,
                            Player1 = new PlayerDump() { Nickname = games[intID].Player1.Nickname, Score = games[intID].Player1.Score, WordsPlayed = games[intID].Player1.WordsPlayed },
                            Player2 = new PlayerDump() { Nickname = games[intID].Player2.Nickname, Score = games[intID].Player2.Score, WordsPlayed = games[intID].Player2.WordsPlayed }
                        };
                    }
                }
                SetStatus(Forbidden);
                return null;
            }
        }

    }
}

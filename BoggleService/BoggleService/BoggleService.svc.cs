using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Net;
using System.ServiceModel.Web;
using static System.Net.HttpStatusCode;
using System.Diagnostics;

namespace Boggle
{
    public class BoggleService : IBoggleService
    {
        // Connetion string to teh database
        private static string BoggleServiceDB;
        /// <summary>
        /// Poor mans data base static variables.
        /// </summary>
        private static readonly Dictionary<String, UserInfo> users = new Dictionary<String, UserInfo>();
        private static readonly Dictionary<int, BoggleGame> games = new Dictionary<int, BoggleGame>();
        private static readonly object sync = new object();
        private static int GameIDCounter = 1;

        static BoggleService()
        {
            // Create connection string
            //BoggleServiceDB = "Data Source = (LocalDB)\\MSSQLLocalDB; AttachDbFilename = |DataDirectory|\\BoggleDB.mdf; Integrated Security = True";
            BoggleServiceDB = ConfigurationManager.ConnectionStrings["BoggleDB"].ConnectionString;
        }
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
            if (user.Nickname == null || user.Nickname.Trim().Length == 0)
            {
                SetStatus(Forbidden);
                return null;
            }
            using (SqlConnection conn = new SqlConnection(BoggleServiceDB))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    using (SqlCommand command = new SqlCommand("insert into Users (UserID, Nickname) values(@UserID, @Nickname)", conn, trans))
                    {
                        // Generate user token.
                        string userID = Guid.NewGuid().ToString();

                        // Replace placeholders to add into the SQL database.
                        command.Parameters.AddWithValue("@UserID", userID);
                        command.Parameters.AddWithValue("@Nickname", user.Nickname.Trim());

                        //Execute command with the transaction over the connection.
                        command.ExecuteNonQuery();
                        SetStatus(Created);

                        // Commit the transaction immediatly before returning.
                        trans.Commit();
                        return new CreateUserReturn() { UserToken = userID };
                    }
                }
            }
        }

        // TODO : JoinGame implement DB.
        public JoinGameReturn JoinGame(JoinGameArgs args)
        {
            if ((args.TimeLimit >= 5 && args.TimeLimit <= 120))
            {
                using (SqlConnection conn = new SqlConnection(BoggleServiceDB))
                {
                    conn.Open();
                    SqlCommand topGID = new SqlCommand("SELECT TOP 1 * FROM Games ORDER BY GameID DESC ", conn);
                    using (SqlDataReader reader = topGID.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // if game doesn't exists.
                            if (reader["GameID"] == null || reader["Player1"] == null)
                            {
                                string IDSave = reader["GameID"].ToString();
                                
                                SqlCommand createGame = new SqlCommand("insert into Games(GameState, TimeLimit, Player1) values(@GameState, @TimeLimit, @Player1)");
                                using (SqlTransaction trans = conn.BeginTransaction())
                                {
                                    // Replace placeholders to add into the SQL database.
                                    createGame.Parameters.AddWithValue("@GameState", "pending");
                                    createGame.Parameters.AddWithValue("@TimeLimit", args.TimeLimit);
                                    createGame.Parameters.AddWithValue("@Player1", args.UserToken);

                                    try
                                    {
                                        createGame.ExecuteNonQuery();
                                        SetStatus(Accepted);

                                        trans.Commit();
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.WriteLine(e.Message);
                                        SetStatus(HttpStatusCode.ExpectationFailed);
                                    }
                                    return new JoinGameReturn() { GameID = IDSave };
                                }
                            }
                            else
                            {
                                string IDSave = reader["GameID"].ToString();

                                if (args.UserToken.Equals( GetFromGamesTable(GetLastGID(), "Player2")?.ToString() ))
                                {
                                    SetStatus(Conflict);
                                    return null;
                                }
                                SqlCommand addPlayer2 = new SqlCommand("insert into Games(GameState, TimeLimit, Player2, StartTime, Board) values(@GameState, @TimeLimit, @Player2 , @StartTime, @Board)");
                                using (SqlTransaction trans = conn.BeginTransaction())
                                {
                                    // Replace placeholders to add into the SQL database.
                                    addPlayer2.Parameters.AddWithValue("@GameState", "active");
                                    int previousTimeLimit = Convert.ToInt32(GetFromGamesTable(GetLastGID(), "TimeLimit"));

                                    addPlayer2.Parameters.AddWithValue("@TimeLimit", (args.TimeLimit + previousTimeLimit)/2);
                                    addPlayer2.Parameters.AddWithValue("@Player2", args.UserToken);
                                    addPlayer2.Parameters.AddWithValue("@StartTime", DateTime.Now);
                                    addPlayer2.Parameters.AddWithValue("@Board", new BoggleBoard().ToString());

                                    try
                                    {
                                        addPlayer2.ExecuteNonQuery();
                                        SetStatus(Created);

                                        trans.Commit();
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.WriteLine(e.Message);
                                        SetStatus(HttpStatusCode.ExpectationFailed);
                                    }
                                    return new JoinGameReturn() { GameID = IDSave };
                                }
                            }


                            // TODO: Store Game number from DB into createdGame.GameID property.
                            // TODO: Add game new game number to the database.
                        }

                    }
                }
            }
            SetStatus(Forbidden);
            return null;
        }
        string GetLastGID()
        {
            using (SqlConnection conn = new SqlConnection(BoggleServiceDB))
            {
                conn.Open();
                SqlCommand Game = new SqlCommand("SELECT TOP 1 * FROM Games ORDER BY GameID DESC", conn);
                using (SqlDataReader reader = Game.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        return reader["GameID"].ToString();
                    }
                }
            }
            return null;
        }

        object GetFromGamesTable(string GID, string column)
        {
            using (SqlConnection conn = new SqlConnection(BoggleServiceDB))
            {
                conn.Open();
                SqlCommand Game = new SqlCommand("SELECT * FROM Games WHERE GameID =" + column, conn);
                using (SqlDataReader reader = Game.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        return reader[column];
                    }
                }
            }
            return null;
        }

        //    Guid outR;
        //    //See if the userToken is Valid and timeLimit is within bounds
        //    if (Guid.TryParseExact(args.UserToken, "D", out outR) && users.ContainsKey(args.UserToken) && args.TimeLimit >= 5 && args.TimeLimit <= 120)
        //    {
        //        //If game exists and Player 1 is in. 
        //        if (games.ContainsKey(GameIDCounter) && games[GameIDCounter]?.Player1 != null)
        //        {
        //            //If UserToken is identical to one that is in the game we have CONFLICT.
        //            if (games[GameIDCounter].Player1.UserToken.Equals(args.UserToken))
        //            {
        //                SetStatus(Conflict);
        //                return null;
        //            }
        //            //If UserToken is not identical to the one in the game we have CREATED the game.
        //            else
        //            {
        //                SetStatus(Created);

        //                games[GameIDCounter].Player2 = new Player() { UserToken = args.UserToken, Nickname = users[args.UserToken].Nickname };
        //                games[GameIDCounter].TimeLimit = (games[GameIDCounter].TimeLimit + args.TimeLimit) / 2;

        //                games[GameIDCounter].GameState = "active";

        //                //games[ActiveGameID].GameTimer.Start();
        //                // TODO: Set the timelimit.

        //                return new JoinGameReturn() { GameID = GameIDCounter++.ToString() };
        //            }
        //        }
        //        else
        //        {
        //            //If there are no players in the game. We ACCEPTED him as the first player.
        //            SetStatus(Accepted);

        //            games[GameIDCounter].Player1 = new Player() { UserToken = args.UserToken, Nickname = users[args.UserToken].Nickname };
        //            games[GameIDCounter].TimeLimit = args.TimeLimit;

        //            return new JoinGameReturn() { GameID = GameIDCounter.ToString() };
        //        }
        //    }
        //    else
        //    {
        //        //If the userToken is invalid or the timelimit is out of bounds, this request is FORBIDDEN.
        //        SetStatus(Forbidden);
        //        return null;
        //    }
        //    }
        //}

        // TODO : CancelJoinRequest implement DB.
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

        // TODO : PlayWord implement DB.
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
                        if (isWord(args.Word) && games[intID].Board.CanBeFormed(args.Word))
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

        // TODO : Status implement DB.
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

        // TODO : StatusBrief implement DB.
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

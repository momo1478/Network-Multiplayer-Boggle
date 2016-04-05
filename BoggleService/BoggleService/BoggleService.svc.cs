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
using System.Linq;

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
            lock (sync)
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

        }
        // TODO : Accepted & Created Status needs to be implemented always returns status(forbiddin)
        public JoinGameReturn JoinGame(JoinGameArgs args)
        {
            lock (sync)
            {
                if ((args.TimeLimit >= 5 && args.TimeLimit <= 120))
                {
                    string currentGID = GetLastGID();

                    if (currentGID == null || GetFromGamesTable(currentGID, "Player2") != null)
                    {
                        //Creates Game if none exists
                        currentGID = CreateEmptyGame(args).ToString();
                    }
                    if (GetFromGamesTable(currentGID, "Player1") == null)
                    {
                        //If a Game Exists and no one has joined we ACCEPT the player
                        Player1Join(currentGID, args);
                        SetStatus(Accepted);
                        return new JoinGameReturn { GameID = currentGID };
                    }
                    else
                    {
                        //If a player in the game is trying to join we have a CONFLICT
                        if (GetFromGamesTable(currentGID, "Player1").Equals(args.UserToken))
                        {
                            SetStatus(Conflict);
                            return null;
                        }

                        //If a new player is joining a game with an existing one we CREATED a game.
                        Player2Join(currentGID, args);
                        SetStatus(Created);
                        return new JoinGameReturn { GameID = currentGID };
                    }
                }
                //If the timelimit (along with other conditions to be put) is out of bounds this request is FORBIDDEN
                SetStatus(Forbidden);
                return null;
            }
        }
        void Player1Join(string GameID, JoinGameArgs args)
        {
            using (SqlConnection conn = new SqlConnection(BoggleServiceDB))
            {
                conn.Open();

                using (SqlTransaction createGameTrans = conn.BeginTransaction())
                {
                    SqlCommand updateTable = new SqlCommand("UPDATE Games SET Player1 = @Player1, TimeLimit = @TimeLimit WHERE GameID = " + GameID, conn, createGameTrans);
                    updateTable.Parameters.AddWithValue("@Player1", args.UserToken);
                    updateTable.Parameters.AddWithValue("@TimeLimit", args.TimeLimit);
                    try
                    {
                        updateTable.ExecuteNonQuery();
                        createGameTrans.Commit();
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
            }
        }
        void Player2Join(string GameID, JoinGameArgs args)
        {
            using (SqlConnection conn = new SqlConnection(BoggleServiceDB))
            {
                conn.Open();
                using (SqlTransaction createGameTrans = conn.BeginTransaction())
                {
                    SqlCommand updateTable = new SqlCommand("UPDATE Games SET Player2 = @Player2, TimeLimit = @TimeLimit, Board = @Board, StartTime = @StartTime, GameState = @GameState WHERE GameID = " + GameID, conn, createGameTrans);
                    updateTable.Parameters.AddWithValue("@Player2", args.UserToken);
                    updateTable.Parameters.AddWithValue("@TimeLimit", (args.TimeLimit + Convert.ToInt32(GetFromGamesTable(GameID, "TimeLimit"))) / 2);
                    updateTable.Parameters.AddWithValue("@Board", new BoggleBoard().ToString());
                    updateTable.Parameters.AddWithValue("@StartTime", DateTime.Now);
                    updateTable.Parameters.AddWithValue("@GameState", "active");
                    try
                    {
                        updateTable.ExecuteNonQuery();
                        createGameTrans.Commit();
                    }
                    catch (Exception e)
                    {
                        // TODO : fix bug where entering in a invalid User token.
                        throw e;
                    }
                }
            }
        }
        int CreateEmptyGame(JoinGameArgs args)
        {
            using (SqlConnection conn = new SqlConnection(BoggleServiceDB))
            {
                conn.Open();

                using (SqlTransaction createGameTrans = conn.BeginTransaction())
                {
                    SqlCommand createGame = new SqlCommand("Insert into Games(GameState) values(@GameState)", conn, createGameTrans);
                    createGame.Parameters.AddWithValue("@GameState", "pending");

                    try
                    {
                        createGame.ExecuteNonQuery();
                        createGameTrans.Commit();
                        return int.Parse(GetLastGID());
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
            }
        }


        /// <summary>
        /// Gets last GameID added to games list.
        /// </summary>
        /// <returns></returns>
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
                        return reader["GameID"]?.ToString();
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Gets a column from a game given a GameID
        /// Columns are : GameID, Player1, Player2, TimeStart, TimeLimit, GameState, and Board.
        /// </summary>
        /// <param name="GID"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        object GetFromGamesTable(string GID, string column)
        {
            using (SqlConnection conn = new SqlConnection(BoggleServiceDB))
            {
                conn.Open();
                SqlCommand Game = new SqlCommand("SELECT * FROM Games WHERE GameID =" + GID, conn);
                using (SqlDataReader reader = Game.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        return reader[column] is DBNull ? null : reader[column];
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns all columns of a game in an object 
        /// </summary>
        /// <param name="GID"></param>
        /// <returns></returns>
        DBGameInfo GetGameInfo(string GID)
        {
            using (SqlConnection conn = new SqlConnection(BoggleServiceDB))
            {
                conn.Open();
                SqlCommand Game = new SqlCommand("SELECT * FROM Games WHERE GameID = @GameId", conn);
                Game.Parameters.AddWithValue("@GameId", GID);

                using (SqlDataReader reader = Game.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        return new DBGameInfo
                        {
                            Board = reader["Board"] is DBNull ? null : reader["Board"].ToString(),
                            GameID = (int)reader["GameID"],
                            GameState = reader["GameState"] is DBNull ? null : reader["GameState"].ToString(),
                            Player1 = reader["Player1"] is DBNull ? null : reader["Player1"].ToString(),
                            Player2 = reader["Player2"] is DBNull ? null : reader["Player2"].ToString(),
                            StartTime = reader["StartTime"] is DBNull ? default(DateTime) : Convert.ToDateTime(reader["StartTime"]),
                            TimeLimit = (int)reader["TimeLimit"]
                        };
                    }
                }
            }
            return null;
        }



        // TODO : CancelJoinRequest implement DB.
        public void CancelJoinRequest(JoinGameArgs args)
        {
            lock (sync)
            {
                DBGameInfo GameInfo = GetGameInfo(GetLastGID());

                if (GameInfo?.Player1 != null && GameInfo.GameState.Equals("pending") && GameInfo.Player1.Equals(args.UserToken))
                {
                    RemovePlayer1(GameInfo.GameID.ToString());
                    SetStatus(OK);
                    return;
                }

                SetStatus(Forbidden);
                return;
            }
        }
        void RemovePlayer1(string GameID)
        {
            using (SqlConnection conn = new SqlConnection(BoggleServiceDB))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    SqlCommand removeP1 = new SqlCommand("UPDATE Games SET Player1 = @Player1 WHERE GameID =" + GameID, conn, trans);
                    removeP1.Parameters.AddWithValue("@Player1", null ?? Convert.DBNull);

                    try
                    {
                        removeP1.ExecuteNonQuery();
                        trans.Commit();
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                    
                }
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

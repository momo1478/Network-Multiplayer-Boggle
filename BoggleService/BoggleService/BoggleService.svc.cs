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

        public JoinGameReturn JoinGame(JoinGameArgs args)
        {
            lock (sync)
            {
                Guid outR;
                if ((args.TimeLimit >= 5 && args.TimeLimit <= 120) && Guid.TryParse(args.UserToken, out outR) && GetNickname(args.UserToken) != null)
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
        // JoinGame Helper methods
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
                    args.TimeLimit = (args.TimeLimit + Convert.ToInt32(GetFromGamesTable(GameID, "TimeLimit"))) / 2;
                    SqlCommand updateTable = new SqlCommand("UPDATE Games SET Player2 = @Player2, TimeLimit = @TimeLimit, Board = @Board, StartTime = @StartTime, GameState = @GameState WHERE GameID = " + GameID, conn, createGameTrans);
                    updateTable.Parameters.AddWithValue("@Player2", args.UserToken);
                    updateTable.Parameters.AddWithValue("@TimeLimit", args.TimeLimit);
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
        /// Retrieves a Nickname given a UserToken
        /// </summary>
        /// <param name="UserToken"></param>
        /// <returns></returns>
        string GetNickname(string UserToken)
        {
            using (SqlConnection conn = new SqlConnection(BoggleServiceDB))
            {
                conn.Open();
                SqlCommand Game = new SqlCommand("Select Nickname from Users Where UserID = '" + UserToken + "'", conn);
                using (SqlDataReader reader = Game.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        return reader["Nickname"] is DBNull ? null : reader["Nickname"].ToString();
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Gets all words 
        /// </summary>
        /// <param name="GID"></param>
        /// <param name="UserToken"></param>
        public IEnumerable<DBWord> GetWords(string GID, string UserToken)
        {
            using (SqlConnection conn = new SqlConnection(BoggleServiceDB))
            {
                conn.Open();
                SqlCommand Game = new SqlCommand("Select * from Words Where GameID = " + GID + " AND Player = '" + UserToken + "'", conn);
                using (SqlDataReader reader = Game.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return new DBWord() { Word = reader["Word"].ToString(), Score = Convert.ToInt32(reader["Score"]) };
                    }
                }
            }
        }

        public int GetPlayerScore(string GID, string UserToken)
        { 
            using (SqlConnection conn = new SqlConnection(BoggleServiceDB))
            {
                conn.Open();
                SqlCommand Game = new SqlCommand("Select Sum(Score) AS 'TotalScore' from Words Where GameID =" + GID + " AND Player ='" + UserToken + "'", conn);
                using (SqlDataReader reader = Game.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        return reader["TotalScore"] is DBNull ? default(int) : Convert.ToInt32(reader["TotalScore"]);
                    }
                }
            }
            return default(int);
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
                            Player1 = new DBPlayerInfo()
                            {
                                UserToken = reader["Player1"] is DBNull ? null : reader["Player1"].ToString(),
                                Nickname = GetNickname(reader["Player1"] is DBNull ? "" : reader["Player1"].ToString()),
                                Score = GetPlayerScore(reader["GameID"].ToString(), reader["Player1"] is DBNull ? "" : reader["Player1"].ToString()),
                                WordsPlayed = GetWords(reader["GameID"].ToString(), reader["Player1"] is DBNull ? "" : reader["Player1"].ToString()).ToList()
                            },
                            Player2 = new DBPlayerInfo()
                            {
                                UserToken = reader["Player2"] is DBNull ? null : reader["Player2"].ToString(),
                                Nickname = GetNickname(reader["Player2"] is DBNull ? "" : reader["Player2"].ToString()),
                                Score = GetPlayerScore(reader["GameID"].ToString(), reader["Player2"] is DBNull ? "" : reader["Player2"].ToString()),
                                WordsPlayed = GetWords(reader["GameID"].ToString(), reader["Player2"] is DBNull ? "" : reader["Player2"].ToString()).ToList()
                            },
                            StartTime = reader["StartTime"] is DBNull ? default(DateTime) : Convert.ToDateTime(reader["StartTime"]),
                            TimeLimit = (int)reader["TimeLimit"],
                            TimeLeft = -300
                            };
                        }
                    }
                }
                return null;
            }

        public void CancelJoinRequest(JoinGameArgs args)
        {
            lock (sync)
            {
                DBGameInfo GameInfo = GetGameInfo(GetLastGID());

                if (GameInfo?.Player1 != null && GameInfo.GameState.Equals("pending") && GameInfo.Player1.UserToken.Equals(args.UserToken))
                {
                    RemovePlayer1(GameInfo.GameID.ToString());
                    SetStatus(OK);
                    return;
                }

                SetStatus(Forbidden);
                return;
            }
        }
        //Cancel Join Request Helper Method
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
            lock (sync)
            {
                int intID;
                args.Word = args.Word?.ToUpper() ?? "";
                DBGameInfo currentGameInfo = GetGameInfo(GameID);
                // checks for forbidden
                if (args?.Word != null && args.Word.Trim().Length != 0 && int.TryParse(GameID, out intID) && currentGameInfo != null && GetNickname(currentGameInfo.Player1.UserToken) != null && GetNickname(currentGameInfo.Player2.UserToken) != null && (args.UserToken== currentGameInfo.Player1.UserToken || args.UserToken == currentGameInfo.Player2.UserToken)) //GetNickname != null && currentGID = Game ID)
                {
                    //who is submiting player 1 or 2
                    int player = currentGameInfo.Player1.UserToken.Equals(args.UserToken) ? 1 : 2;

                    if (currentGameInfo.GameState.Equals("active"))
                    {
                        int wordScore;
                        BoggleBoard CurrentGameBoard = new BoggleBoard(currentGameInfo.Board);
                        // can be formed and in dictionary
                        if (isWord(args.Word) && CurrentGameBoard.CanBeFormed(args.Word))
                        {
                            SetStatus(OK);
                            if (player == 1)
                            {
                                if (GetWords(currentGameInfo.GameID.ToString(), currentGameInfo.Player1.UserToken.ToString()).ToList().Exists(s => s.Word.Equals(args.Word)))
                                {
                                    wordScore = AddPlayedWord(currentGameInfo.GameID.ToString(), currentGameInfo.Player1.UserToken.ToString(), args.Word, 0.ToString());
                                }
                                else
                                {
                                    wordScore = AddPlayedWord(currentGameInfo.GameID.ToString(), currentGameInfo.Player1.UserToken.ToString(), args.Word, currentGameInfo.Player1.WordScore(args.Word).ToString());
                                }
                            }
                            else //player 2
                            {
                                if (GetWords(currentGameInfo.GameID.ToString(), currentGameInfo.Player2.UserToken.ToString()).ToList().Exists(s => s.Word.Equals(args.Word)))
                                {
                                    wordScore = AddPlayedWord(currentGameInfo.GameID.ToString(), currentGameInfo.Player2.UserToken.ToString(), args.Word, 0.ToString());
                                }
                                else
                                {
                                    wordScore = AddPlayedWord(currentGameInfo.GameID.ToString(), currentGameInfo.Player2.UserToken.ToString(), args.Word, currentGameInfo.Player2.WordScore(args.Word).ToString());
                                }
                            }
                            return new PlayWordReturn() { Score = wordScore };
                        }
                        else
                        {
                            SetStatus(OK);

                            if (player == 1)
                            {
                                if (GetWords(currentGameInfo.GameID.ToString(), currentGameInfo.Player1.UserToken.ToString()).ToList().Exists(s => s.Word.Equals(args.Word)))
                                {
                                    wordScore = AddPlayedWord(currentGameInfo.GameID.ToString(), currentGameInfo.Player1.UserToken.ToString(), args.Word, (0).ToString());
                                }
                                else
                                {
                                    wordScore = AddPlayedWord(currentGameInfo.GameID.ToString(), currentGameInfo.Player1.UserToken.ToString(), args.Word, (-1).ToString());
                                }
                            }
                            else // Player 2
                            {
                                if (GetWords(currentGameInfo.GameID.ToString(), currentGameInfo.Player2.UserToken.ToString()).ToList().Exists(s => s.Word.Equals(args.Word)))
                                {
                                    wordScore = AddPlayedWord(currentGameInfo.GameID.ToString(), currentGameInfo.Player2.UserToken.ToString(), args.Word, (0).ToString());
                                }
                                else
                                {
                                    wordScore = AddPlayedWord(currentGameInfo.GameID.ToString(), currentGameInfo.Player2.UserToken.ToString(), args.Word, (-1).ToString());
                                }
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
        //Playword helper method.
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
        public int AddPlayedWord(string GID, string UserToken, string PlayedWord, string Score)
        {
            using (SqlConnection conn = new SqlConnection(BoggleServiceDB))
            {
                conn.Open();

                using (SqlTransaction addPlayedWordTrans = conn.BeginTransaction())
                {
                    SqlCommand addPlayedWord = new SqlCommand("Insert into Words(Word, GameID, Player, Score) values(@Word, @GameID, @Player, @Score)", conn, addPlayedWordTrans);
                    addPlayedWord.Parameters.AddWithValue("@Word", PlayedWord);
                    addPlayedWord.Parameters.AddWithValue("@GameID", GID);
                    addPlayedWord.Parameters.AddWithValue("@Player", UserToken);
                    addPlayedWord.Parameters.AddWithValue("@Score", Score);
                    try
                    {
                        addPlayedWord.ExecuteNonQuery();
                        addPlayedWordTrans.Commit();
                        return int.Parse(Score);
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
            }
        }

        // TODO : Status implement DB.
        public GetStatusReturn Status(string GameID)
        {
            lock (sync)
            {
                DBGameInfo gameInfo = GetGameInfo(GameID);

                if (gameInfo.GameID != 0)

                    if (gameInfo.GameState.Equals("pending"))
                {
                        SetStatus(OK);
                        return new GetStatusReturn() { GameState = gameInfo.GameState };
                    }
                    else if (gameInfo.GameState.Equals("active"))
                    {
                        SetStatus(OK);
                        return new GetStatusReturn()
                        {
                            Board = gameInfo.Board.ToString(),
                            TimeLimit = gameInfo.TimeLimit,
                            TimeLeft = gameInfo.TimeLeft,

                            GameState = gameInfo.GameState,
                            Player1 = new PlayerDump() { Nickname = gameInfo.Player1.Nickname, Score = gameInfo.Player1.Score },
                            Player2 = new PlayerDump() { Nickname = gameInfo.Player2.Nickname, Score = gameInfo.Player2.Score },
                        };
                    }
                    else if (gameInfo.GameState.Equals("completed"))
                    {
                        SetStatus(OK);
                        return new GetStatusReturn()
                        {
                            Board = gameInfo.Board.ToString(),
                            TimeLimit = gameInfo.TimeLimit,
                            TimeLeft = gameInfo.TimeLeft,

                            GameState = gameInfo.GameState,
                            Player1 = new PlayerDump() { Nickname = gameInfo.Player1.Nickname, Score = gameInfo.Player1.Score, WordsPlayed = gameInfo.Player1.WordsPlayed },
                            Player2 = new PlayerDump() { Nickname = gameInfo.Player2.Nickname, Score = gameInfo.Player2.Score, WordsPlayed = gameInfo.Player2.WordsPlayed }
                        };
                    }
                }
                SetStatus(Forbidden);
                return null;
            }

        // TODO : StatusBrief implement DB.
        public GetStatusReturn StatusBrief(string GameID, string brief)
        {
            lock (sync)
            {
                DBGameInfo gameInfo = GetGameInfo(GameID);

                if (gameInfo.GameID != 0)
                {
                    int? updateTimeLeft = gameInfo.TimeLeft;
                    if (gameInfo.GameState.Equals("pending"))
                    {
                        SetStatus(OK);
                        return new GetStatusReturn() { GameState = gameInfo.GameState };
                    }
                    else if (brief != null && brief.ToLower().Equals("yes"))
                    {
                        if (gameInfo.GameState.Equals("active") || gameInfo.GameState.Equals("completed"))
                        {
                            SetStatus(OK);
                            return new GetStatusReturn()
                            {
                                TimeLeft = gameInfo.TimeLeft,
                                GameState = gameInfo.GameState,
                                Player1 = new PlayerDump() { Score = gameInfo.Player1.Score },
                                Player2 = new PlayerDump() { Score = gameInfo.Player2.Score }
                            };
                        }
                    }
                    else if (gameInfo.GameState.Equals("active"))
                    {
                        SetStatus(OK);
                        return new GetStatusReturn()
                        {
                            Board = gameInfo.Board.ToString(),
                            TimeLimit = gameInfo.TimeLimit,
                            TimeLeft = gameInfo.TimeLeft,

                            GameState = gameInfo.GameState,
                            Player1 = new PlayerDump() { Nickname = gameInfo.Player1.Nickname, Score = gameInfo.Player1.Score },
                            Player2 = new PlayerDump() { Nickname = gameInfo.Player2.Nickname, Score = gameInfo.Player2.Score },
                        };
                    }
                    else if (gameInfo.GameState.Equals("completed"))
                    {
                        SetStatus(OK);
                        return new GetStatusReturn()
                        {
                            Board = gameInfo.Board.ToString(),
                            TimeLimit = gameInfo.TimeLimit,
                            TimeLeft = gameInfo.TimeLeft,

                            GameState = gameInfo.GameState,
                            Player1 = new PlayerDump() { Nickname = gameInfo.Player1.Nickname, Score = gameInfo.Player1.Score, WordsPlayed = gameInfo.Player1.WordsPlayed },
                            Player2 = new PlayerDump() { Nickname = gameInfo.Player2.Nickname, Score = gameInfo.Player2.Score, WordsPlayed = gameInfo.Player2.WordsPlayed }
                        };
                    }
                }
                SetStatus(Forbidden);
                return null;
            }
        }

    }
}

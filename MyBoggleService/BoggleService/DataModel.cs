using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Boggle
{
    [DataContract]
    public class BoggleGame
    {

        [DataMember(EmitDefaultValue = false)]
        public BoggleBoard Board { get; } = new BoggleBoard();

        [DataMember(EmitDefaultValue = true)]
        public string GameState { get; set; }

        /// <summary>
        /// Gets the DateTime.Now
        /// </summary>
        private int TimeNow
        {
            get
            {
                unchecked
                {
                    return Convert.ToInt32((DateTime.Now.Ticks - 635949596620000000) / TimeSpan.TicksPerSecond);
                }
            }
        }

        /// <summary>
        /// Set to DateTime.Now when we set the TimeLimit.
        /// </summary>
        private int TimeStart { get; set; }

        /// <summary>
        /// gets and sets the timeLimit.
        /// Sets the TimeStart property to DateTime.Now.
        /// </summary>
        int timeLimit;
        [DataMember(EmitDefaultValue = false)]
        public int TimeLimit
        {
            get
            {
                return timeLimit;
            }
            set
            {
                TimeStart = TimeNow;
                timeLeft = value;
                timeLimit = value;
            }
        }

        private int timeLeft;
        /// <summary>
        /// Calculates the time left based on TimeStart, TimeNow and TimeLimit.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? TimeLeft
        {
            get
            {
                int timePast = TimeNow - TimeStart;
                TimeStart = TimeNow;

                timeLeft -= timePast;

                if (timeLeft <= 0)
                {
                    timeLeft = 0;
                    this.GameState = "completed";
                }
                return timeLeft;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public int GameID { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public Player Player1 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public Player Player2 { get; set; }
    }

    [DataContract]
    public class Player
    {
        public string UserToken { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Nickname { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? Score { get; set; } = 0;

        [DataMember(EmitDefaultValue = false)]
        public List<Words> WordsPlayed { get; set; } = new List<Words>();

        public int WordScore(string word)
        {
            foreach (Words werd in WordsPlayed)
            {
                if (werd.Word.Equals(word))
                    return 0;
            }

            if (word.Length < 3) return 0;
            else if (word.Length == 3 || word.Length == 4) return 1;
            else if (word.Length == 5) return 2;
            else if (word.Length == 6) return 3;
            else if (word.Length == 7) return 5;
            else if (word.Length > 7) return 11;
            else
            {
                return 0;
            }
        }
    }
    [DataContract]
    public class Words : IEqualityComparer<Words>
    {
        [DataMember(EmitDefaultValue = false)]
        public string Word { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? Score { get; set; }

        public bool Equals(Words x, Words y)
        {
            return x.Word.Equals(y.Word);
        }

        public int GetHashCode(Words obj)
        {
            return base.GetHashCode();
        }
    }

    [DataContract]
    public class UserInfo
    {
        [DataMember(EmitDefaultValue = false)]
        public string Nickname { get; set; }
    }

    [DataContract]
    public class CreateUserReturn
    {
        [DataMember(EmitDefaultValue = false)]
        public string UserToken { get; set; }
    }

    [DataContract]
    public class JoinGameReturn
    {
        [DataMember(EmitDefaultValue = false)]
        public string GameID { get; set; }
    }

    [DataContract]
    public class PlayWordReturn
    {
        [DataMember(EmitDefaultValue = false)]
        public int? Score { get; set; }
    }

    public class JoinGameArgs
    {
        public string UserToken { get; set; }

        public int TimeLimit { get; set; }
    }

    public class CancelGameArgs
    {
        public string UserToken { get; set; }
    }

    public class PlayWordArgs
    {
        public string UserToken { get; set; }
        public string Word { get; set; }
    }

    [DataContract]
    public class GetStatusReturn
    {
        [DataMember]
        public string GameState { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? TimeLeft { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public PlayerDump Player1 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public PlayerDump Player2 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Board { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? TimeLimit { get; set; }

    }

    [DataContract]
    public class PlayerDump
    {

        [DataMember(EmitDefaultValue = false)]
        public string Nickname { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? Score { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public List<DBWord> WordsPlayed { get; set; }
    }

    public class DBGameInfo
    {
        public int GameID { get; set; }
        public DBPlayerInfo Player1 { get; set; }
        public DBPlayerInfo Player2 { get; set; }
        public string Board { get; set; }
        public int TimeLimit { get; set; }
        public DateTime StartTime { get; set; }
        public string GameState { get; set; }
        int? timeLeft;
        public int? TimeLeft
        {
            get
            {
                return ComputeTimeLeft();
            }
            set
            {
                timeLeft = ComputeTimeLeft();
            }
        }
        /// <summary>
        /// Gets the DateTime.Now
        /// </summary>

        public int ComputeTimeLeft()
        {
            TimeSpan timeDifference = DateTime.Now.Subtract(StartTime);

            if (TimeLimit - timeDifference.TotalSeconds <= 0 && !StartTime.Equals(default(DateTime)))
            {
                GameState = "completed";
                UpdateGameStatusToCompleted();
                return 0;
            }

            return Convert.ToInt32(TimeLimit - (timeDifference.TotalSeconds > int.MaxValue ? 0 : timeDifference.TotalSeconds));
        }

        public void UpdateGameStatusToCompleted()
        {
            using (SqlConnection conn = new SqlConnection(BoggleService.BoggleServiceDB))
            {
                conn.Open();

                using (SqlTransaction GameTrans = conn.BeginTransaction())
                {
                    SqlCommand updateTable = new SqlCommand("UPDATE Games SET GameState = @GameState WHERE GameID = " + GameID, conn, GameTrans);
                    updateTable.Parameters.AddWithValue("@GameState", "completed");

                    try
                    {
                        updateTable.ExecuteNonQuery();
                        GameTrans.Commit();
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
            }
        }
    }

    public class DBPlayerInfo
    {
        public string UserToken { get; set; }

        public string Nickname { get; set; }

        public int? Score { get; set; } = 0;

        public List<DBWord> WordsPlayed { get; set; } = new List<DBWord>();

        public int WordScore(string word)
        {
            if (word.Length < 3) return 0;
            else if (word.Length == 3 || word.Length == 4) return 1;
            else if (word.Length == 5) return 2;
            else if (word.Length == 6) return 3;
            else if (word.Length == 7) return 5;
            else if (word.Length > 7) return 11;
            else
            {
                return 0;
            }
        }
    }

    public class DBWord
    {
        public string Word { get; set; }

        public int? Score { get; set; }

        public static int WordScore(string word, List<DBWord> WordsPlayed)
        {
            foreach (DBWord werd in WordsPlayed)
            {
                if (werd.Word.Equals(word))
                    return 0;
            }

            if (word.Length < 3) return 0;
            else if (word.Length == 3 || word.Length == 4) return 1;
            else if (word.Length == 5) return 2;
            else if (word.Length == 6) return 3;
            else if (word.Length == 7) return 5;
            else if (word.Length > 7) return 11;
            else
            {
                return 0;
            }
        }
    }
}

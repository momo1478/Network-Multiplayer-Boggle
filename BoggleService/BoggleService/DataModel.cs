using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boggle
{
    public class BoggleGame
    {
        public BoggleBoard Board { get; set; }

        public string GameState { get; set; }

        public int TimeLimit { get; set; }

        public int GameID { get; set; }

        public Player Player1 { get; set; }

        public Player Player2 { get; set; }
    }

    public class Player
    { 
        public string Nickname { get; set; }

        public int Score { get; set; }

        public List<Words> WordsPlayed { get; set; }
    }

    public class Words
    {
        public string Word { get; set; }

        public int Score { get; set; }
    }

    public class UserInfo
    {
        public string Nickname { get; set; }
    }
}

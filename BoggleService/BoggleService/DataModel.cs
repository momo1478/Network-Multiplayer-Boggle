﻿using System;
using System.Collections.Generic;
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
        public BoggleGame()
        {
            GameTimer.Elapsed += GameTimer_Tick;
        }

        private void GameTimer_Tick(object sender, ElapsedEventArgs e)
        {
            if (TimeLeft-- <= 0)
            {
                GameTimer.Stop();
                GameState = "completed";
            }
        }

        public Timer GameTimer = new Timer() { Interval = 1000 };

        [DataMember(EmitDefaultValue = true)]
        public string GameState { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public BoggleBoard Board { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? TimeLeft { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int TimeLimit { get; set; }

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
        public List<Words> WordsPlayed { get; set; }
    }

    [DataContract]
    public class Words
    {
        [DataMember(EmitDefaultValue = false)]
        public string Word { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? Score { get; set; }
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
        public int Score { get; set; }
    }

    public class JoinGameArgs
    {
        public string UserToken { get; set; }

        public int TimeLimit { get; set; }
    }
    public class PlayWordArgs
    {
        public string UserToken { get; set; }
        public string Word { get; set; }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Boggle
{
    [DataContract]
    public class BoggleGame
    {
        [DataMember(EmitDefaultValue = false)]
        public string GameState { get; set; }

        [DataMember(EmitDefaultValue = true)]
        public BoggleBoard Board { get; set; }

        [DataMember(EmitDefaultValue = true)]
        public int TimeLimit { get; set; }

        [DataMember(EmitDefaultValue = true)]
        public int GameID { get; set; }

        [DataMember(EmitDefaultValue = true)]
        public Player Player1 { get; set; }

        [DataMember(EmitDefaultValue = true)]
        public Player Player2 { get; set; }
    }

    [DataContract]
    public class Player
    {
        [DataMember(EmitDefaultValue = true)]
        public string Nickname { get; set; }

        [DataMember(EmitDefaultValue = true)]
        public int Score { get; set; }

        [DataMember(EmitDefaultValue = true)]
        public List<Words> WordsPlayed { get; set; }
    }

    [DataContract]
    public class Words
    {
        [DataMember(EmitDefaultValue = true)]
        public string Word { get; set; }

        [DataMember(EmitDefaultValue = true)]
        public int Score { get; set; }
    }

    [DataContract]
    public class UserInfo
    {
        [DataMember(EmitDefaultValue = true)]
        public string Nickname { get; set; }
    }

    [DataContract]
    public class CreateUserReturn
    {
        [DataMember(EmitDefaultValue = true)]
        public string UserToken { get; set; }
    }
}

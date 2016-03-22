using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boggle
{
    class Controller
    {
        BoggleGUI View;

        /// <summary>
        /// Gets and sets the players token
        /// </summary>
        public string PlayerToken { get; set; }

        /// <summary>
        /// Gets and sets the players GameID
        /// </summary>
        public string GameID { get; set; }
       
        /// <summary>
        /// Gets and sets the Score
        /// </summary>
        public int Score { get; set; }


        /// <summary>
        /// Begins controlling window.
        /// </summary>
        public Controller(BoggleGUI view)
        {
            View = view;

            view.CreateName += View_CreateName;
            view.JoinGame += View_JoinGame;
            view.Word += View_Word;
        }

        /// <summary>
        /// Handles a request to play a word in a game using the Network class.
        /// Returns the Score if successfull.
        /// </summary>
        /// <param name="word"></param>
        private void View_Word(string word)
        {
            int score = Network.PlayWord(PlayerToken, word);
            if (score != 0)
            {
                Score = score;
            }
        }


        /// <summary>
        /// Handles a request to Join a Game using the Network class.
        /// Returns the Game ID if successfull.
        /// </summary>
        /// <param name="time"></param>
        private void View_JoinGame(int time)
        {
            string GID = Network.JoinGame(PlayerToken, time);
            if (GID != null)
            {
                GameID = GID;
                View.Message = "Game joined!\nID : " + GID;
            }
            else
            {
                View.Message = "Could not join game";
            }
        }

        /// <summary>
        /// Handles a request to create a name using the Network class.
        /// Returns the PlayerToken if successfull.
        /// </summary>
        /// <param name="nickname"></param>
        private void View_CreateName(string nickname)
        {
            string token = Network.CreateName(nickname);
            if (token != null)
            {
                PlayerToken = token;
                View.Message = "User : " + nickname + "\nCreated with token : " + token;
            }
            else
            {
                View.Message = "Unable to create username";
            }
        }

    }
}

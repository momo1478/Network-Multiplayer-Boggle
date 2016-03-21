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


        public string PlayerToken { get; set; }
        public string GameID { get; set; }

        public Controller(BoggleGUI view)
        {
            View = view;

            view.CreateName += View_CreateName;
            view.JoinGame += View_JoinGame;
        }

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
        /// Creates a name using the Network class.
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

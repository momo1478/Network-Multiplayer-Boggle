using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Boggle
{
    /// <summary>
    /// Class used to control the model/BoggleAPI
    /// </summary>
    class Controller
    {
        BoggleGUI View;

        // Properties
        /// <summary>
        /// Gets and sets the players token
        /// </summary>
        public string PlayerToken { get; set; }

        /// <summary>
        /// Gets and sets the players GameID
        /// </summary>
        public string GameID { get; set; }

        /// <summary>
        /// Gets and sets the game 
        /// </summary>
        public dynamic GameStatus { get; set; }


        // Controller Constructor
        /// <summary>
        /// Begins controlling window.
        /// </summary>
        public Controller(BoggleGUI view)
        {
            View = view;

            view.CreateName += View_CreateName;

            view.JoinGame += View_JoinGame;

            view.CancelJoin += View_CancelJoin;

            view.UpdateStatus += View_UpdateStatus;
            
            view.Word += View_Word;

            view.UpdateNameLabels += View_UpdateNameLabels;

            view.UpdateTimeBox += View_UpdateTimeBox;

            view.UpdateScoreBoxes += View_UpdateScoreBoxes;

            view.UpdateLetterBoxes += View_UpdateLetterBoxes;

            view.UpdatePlayer1Words += View_UpdatePlayer1Words;

            view.UpdatePlayer2Words += View_UpdatePlayer2Words;

            view.ActiveUpdate += View_ActiveUpdate;

        }

        private void View_ActiveUpdate()
        {
            if (GameID != null)
            {
                GameStatus = Network.GetStatus(GameID);

                if (GameStatus != null)
                {
                    //UpdateScore()
                    View.Player1ScoreBoxText = GameStatus.Player1.Score;
                    View.Player2ScoreBoxText = GameStatus.Player2.Score;

                    //UpdateTime()
                    View.TimeBoxText = GameStatus.TimeLeft;

                    //UpdateStatus()
                    View.JoinStatusBoxText = GameStatus.GameState;
                }
            }
        }

        private void View_UpdatePlayer1Words()
        {
            View.Player1PlayedBoxText = GameStatus.Player1.WordsPlayed?.ToString() ?? "";
        }

        private void View_UpdatePlayer2Words()
        {
            View.Player2PlayedBoxText = GameStatus.Player2.WordsPlayed?.ToString() ?? "";
        }


        // Handler methods 
        /// <summary>
        /// Handles a request to update the board in the GUI using the server.
        /// </summary>
        private void View_UpdateLetterBoxes()
        {
            GameStatus = Network.GetStatus(GameID);

            string board = GameStatus.Board;
            int index = 0;

            foreach (TextBox box in View.Letters.Controls)
            {
                if (index > 16)
                    break;
                box.Text = board[index].ToString().Equals("Q") ? "Qu" : board[index].ToString();
                index++;
            }
        }

        /// <summary>
        /// Handles a request to update the player1 and player2 ScoreBoxes in GUI, through the network
        /// </summary>
        private void View_UpdateScoreBoxes()
        {
            GameStatus = Network.GetStatus(GameID);

            View.Player1ScoreBoxText = GameStatus.Player1.Score;
            View.Player2ScoreBoxText = GameStatus.Player2.Score;
        }

        /// <summary>
        /// Handles a request to update the time box with the updated time and adjusts JoinStatusBox when there is 0 seconds left.
        /// </summary>
        private void View_UpdateTimeBox()
        {
            GameStatus = Network.GetStatus(GameID);
            View.TimeBoxText = GameStatus.TimeLeft;
            View.TimeLeft = GameStatus.TimeLeft;
        }

        /// <summary>
        /// Handles a request to update the player1 and player2 names in GUI, through the network
        /// </summary>
        private void View_UpdateNameLabels()
        {
            GameStatus = Network.GetStatus(GameID);

            View.Player1ScoreLabelText = GameStatus.Player1.Nickname;
            View.Player2ScoreLabelText = GameStatus.Player2.Nickname;
        }

        /// <summary>
        /// Handles a request to update the JoinStatusBox with the Game State
        /// </summary>
        private void View_UpdateStatus()
        {
            if (GameID != null)
                GameStatus = Network.GetStatus(GameID);

            if (GameStatus != null)
                View.JoinStatusBoxText = GameStatus.GameState;
        }

        /// <summary>
        /// Handles a request to play a word in a game using the Network class.
        /// Returns the Score if successfull.
        /// </summary>
        /// <param name="word"></param>
        private async void View_Word(string word)
        {
            int score = await Network.PlayWord(PlayerToken, word , GameID);

            View.WordScoreBoxText = score > 0 ? "+" + score : score.ToString();

        }

        /// <summary>
        /// Handles a request to Join a Game using the Network class.
        /// Returns the Game ID if successfull.
        /// </summary>
        /// <param name="time"></param>
        private async void View_JoinGame(int time)
        {
            string GID;

            if (Network.SetBaseAddress(View.JoinDomainBoxText))
            {
                GID = await Network.JoinGame(PlayerToken, time);
            }
            else
            {
                MessageBox.Show("Invalid URL.");
                GID = null;
            }

            if (GID != null)
            {
                GameID = GID;
                View.Message = "Game joined!\nID : " + GID;
                if (GameID != null)
                    GameStatus = Network.GetStatus(GameID);

                View.JoinTime.Invoke((MethodInvoker)(() =>
                 {
                     if (GameStatus != null)
                         View.JoinStatusBoxText = GameStatus.GameState;
                 }));
            }
            else
            {
                View.Message = "Could not join game";
            }
        }

        /// <summary>
        /// Handles a request to Cancel Join
        /// </summary>
        private void View_CancelJoin()
        {
            Network.CancelJoin(PlayerToken);
        }

        /// <summary>
        /// Handles a request to create a name using the Network class.
        /// Returns the PlayerToken if successfull.
        /// </summary>
        /// <param name="nickname"></param>
        private async void View_CreateName(string nickname)
        {
            string token;

            if (Network.SetBaseAddress(View.JoinDomainBoxText))
            {
                token = await Network.CreateName(nickname);
            }
            else
            {
                token = null;
            }

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

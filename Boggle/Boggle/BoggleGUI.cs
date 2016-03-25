using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Boggle
{
    /// <summary>
    /// View class
    /// Implement the GUI
    /// </summary>
    public partial class BoggleGUI : Form
    {
        // Properties to communicate with the controller and model/BoggleAPI.
        /// <summary>
        /// Sets the Message for a pop up window.
        /// </summary>
        public string Message { set { MessageBox.Show(value); } }

        /// <summary>
        /// Gets the groupbox LettersGroup
        /// </summary>
        public GroupBox Letters
        {
            get
            {
                return LettersGroup;
            }
        }

        /// <summary>
        /// Gets and sets the JoinTimeBox
        /// </summary>
        public int JoinTimeBoxText
        {
            get
            {
                int result;
                return int.TryParse(JoinTimeBox.Text, out result) ? result : 0;
            }
            set
            {
                JoinTimeBox.Text = value.ToString();
            }
        }

        public TextBox JoinTime
        {
            get { return JoinTimeBox; }
        }

        /// <summary>
        /// Gets and sets the CreateNameBox
        /// </summary>
        public string CreateNameBoxText
        {
            get
            {
                return CreateNameBox.Text;
            }
            set
            {
                CreateNameBox.Text = value;
            }
        }

        /// <summary>
        /// Gets and sets the WordBox
        /// </summary>
        public string WordBoxText
        {
            get
            {
                return WordBox.Text;
            }
            set
            {
                WordBox.Text = value;
            }
        }

        /// <summary>
        /// Gets and sets the Player1ScoreLabelText
        /// </summary>
        public string Player1ScoreLabelText
        {
            get
            {
                return Player1ScoreLabel.Text;
            }
            set
            {
                Player1ScoreLabel.Text = value;
            }
        }

        /// <summary>
        /// Gets and sets the Player2ScoreLabelText
        /// </summary>
        public string Player2ScoreLabelText
        {
            get
            {
                return Player2ScoreLabel.Text;
            }
            set
            {
                Player2ScoreLabel.Text = value;
            }
        }
        
        /// <summary>
        /// Sets the Player1ScoreBox.Text
        /// </summary>
        public string Player1ScoreBoxText
        {
            set { Player1ScoreBox.Text = value; }
        }

        /// <summary>
        /// Sets the Player2ScoreBox.Text
        /// </summary>
        public string Player2ScoreBoxText
        {
            set { Player2ScoreBox.Text = value; }
        }
        
        /// <summary>
        /// Sets the WordScoreBox.Text
        /// </summary>
        public string WordScoreBoxText
        {
            set
            {
                WordScoreBox.Text = value;
            }
        }

        /// <summary>
        /// Sets JoinStatusBox.Text
        /// </summary>
        public string JoinStatusBoxText
        {
            get
            {
                return JoinStatusBox.Text;
            }
            set
            {
                JoinStatusBox.Text = value.ToString();
            }
        }

        /// <summary>
        /// Gets and sets the TimeBox.Text
        /// </summary>
        public string TimeBoxText
        {
            get { return TimeBox.Text; }
            set { TimeBox.Text = value; }
        }

        public string JoinDomainBoxText
        {
            get { return JoinDomainBox.Text; }
            set { JoinDomainBox.Text = value; }
        }

        public string Player1PlayedBoxText
        {
            get
            {
                return Player1PlayedBox.Text;
            }
            set
            {
                Player1PlayedBox.Text = value;
            }
        }

        public string Player2PlayedBoxText
        {
            get
            {
                return Player2PlayedBox.Text;
            }
            set
            {
                Player2PlayedBox.Text = value;
            }
        }

        public int TimeLeft { get; set; }



        // Actions to communicate with the controller and model/BoggleAPI.
        /// <summary>
        /// Fired when we generate a username.
        /// The parameter is the user name.
        /// </summary>
        public event Action<string> CreateName;

        /// <summary>
        /// Fired when we generate a word.
        /// The parameter is the word.
        /// </summary>
        public event Action<string> Word;

        /// <summary>
        /// Fired when we join a game.
        /// The parameter is the join time.
        /// </summary>
        public event Action<int> JoinGame;

        public event Action CancelJoin;

        /// <summary>
        /// Fired to update the game status.
        /// </summary>
        public event Action UpdateStatus;

        /// <summary>
        /// Fired to update the players names.
        /// </summary>
        public event Action UpdateNameLabels;

        /// <summary>
        /// Fired to update the game Time.
        /// </summary>
        public event Action UpdateTimeBox;

        /// <summary>
        /// Fired to update the scores in the player scoreboxes
        /// </summary>
        public event Action UpdateScoreBoxes;

        /// <summary>
        /// Fired to update the board
        /// </summary>
        public event Action UpdateLetterBoxes;

        public event Action UpdatePlayer1Words;

        public event Action UpdatePlayer2Words;

        public event Action ActiveUpdate;


        // Constructor for BoggleGUI.
        /// <summary>
        /// Initializes a new instance of the <see cref="BoggleGUI"/> class.
        /// </summary>
        public BoggleGUI()
        {
            new Controller(this);
            InitializeComponent();
        }


        // Methods called by the GUI to fire events tied to the controller.
        /// <summary>
        /// On enter pressed...
        /// If Successful : Prompts player that username has been generated.
        /// Otherwise :     Prompts player that username has not been generated.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateNameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !JoinStatusBoxText.Equals("active"))
            {
                BackgroundWorkerName.RunWorkerAsync();
            }
        }


        private void CreateButton_Click(object sender, EventArgs e)
        {
            CreateNameBox_KeyDown(sender, new KeyEventArgs(Keys.Enter));
        }


        private void JoinTimeBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                JoinButton_Click(sender, e);
            }
        }

        /// <summary>
        /// On JoinButton clicked
        /// Join game 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void JoinButton_Click(object sender, EventArgs e)
        {
            int temp;
            if (JoinGame != null && UpdateStatus != null && int.TryParse(JoinTimeBox.Text, out temp))
            {
                BackgroundWorker.RunWorkerAsync();
            }
            PendingTimer.Start();
        }


        private void CancelButton_Click(object sender, EventArgs e)
        {
            if (CancelJoin != null)
                CancelJoin();

            if (UpdateStatus != null)
                UpdateStatus();

            StatusCancled();
        }

        // Helper methods to set property values and Start and stop timers
        /// <summary>
        /// Helper method to set gui to a Join Game state.
        /// </summary>
        void StatusJoinGame()
        {
            if (ActiveUpdate != null)
                ActiveUpdate();

            // Setting TextBox properties values.
            
            JoinTimeBox.Invoke((MethodInvoker)(() =>
            {
            JoinTimeBox.ReadOnly = false;
            JoinTimeBox.Enabled = true;
            CreateNameBox.ReadOnly = true;
            CreateNameBox.Enabled = false;
            JoinDomainBox.ReadOnly = true;
            JoinDomainBox.Enabled = false;
            WordBox.ReadOnly = true;
            WordBox.Enabled = false;

            // Setting button properties values.
            JoinButton.Enabled = true;
            CancelButton.Enabled = false;
            ExitGameToolStrip.Enabled = false;
            CreateButton.Enabled = false;

            // Set values to empty
            Player1PlayedBoxText = "";
            Player2PlayedBoxText = "";
            WordBoxText = "";
            WordScoreBoxText = "";
            Player1ScoreBoxText = "";
            Player2ScoreBoxText = "";
            Player1ScoreLabelText = "Player 1 Score";
            Player2ScoreLabelText = "Player 2 Score";
            TimeBoxText = "";

            TimeLeft = 0;
                JoinStatusBoxText = "Set Time & Join";
            }));

        }

        /// <summary>
        /// Helper method to set gui to a canceled state.
        /// </summary>
        void StatusCancled()
        {
            // Setting TextBox properties values.
            JoinTimeBox.ReadOnly = false;
            JoinTimeBox.Enabled = true;
            CreateNameBox.ReadOnly = false;
            CreateNameBox.Enabled = true;
            JoinDomainBox.ReadOnly = false;
            JoinDomainBox.Enabled = true;
            WordBox.ReadOnly = true;
            WordBox.Enabled = false;

            // Setting button properties values.
            JoinButton.Enabled = true;
            CancelButton.Enabled = false;
            ExitGameToolStrip.Enabled = false;
            CreateButton.Enabled = true;
            JoinStatusBoxText = "canceled";
            Player1ScoreBoxText = "";
            Player2ScoreBoxText = "";
            WordScoreBoxText = "";
            WordBoxText = "";
            TimeBoxText = "0";
            TimeLeft = 0;

            PendingTimer.Stop();
        }

        /// <summary>
        /// Helper method to set gui to a pending state.
        /// </summary>
        void StatusPending()
        {
            PendingTimer.Start();

            CreateNameBox.ReadOnly = true;
            CreateNameBox.Enabled = false;
            JoinTimeBox.ReadOnly = true;
            JoinTimeBox.Enabled = false;
            JoinDomainBox.ReadOnly = true;
            JoinDomainBox.Enabled = false;

            JoinButton.Enabled = false;
            ExitGameToolStrip.Enabled = false;
            CancelButton.Enabled = true;
            CreateButton.Enabled = false;
        }

        /// <summary>
        /// Helper method to set gui to a active state.
        /// </summary>
        void StatusActive()
        {
            // Setting TextBox properties to true or false.
            JoinTimeBox.ReadOnly = true;
            JoinTimeBox.Enabled = false;
            CreateNameBox.ReadOnly = true;
            CreateNameBox.Enabled = false;
            JoinDomainBox.ReadOnly = true;
            JoinDomainBox.Enabled = false;
            WordBox.Enabled = true;
            WordBox.ReadOnly = false;

            // Setting Button properties to true or false.
            JoinButton.Enabled = false;
            CancelButton.Enabled = false;
            ExitGameToolStrip.Enabled = true;
            CreateButton.Enabled = false;

            // Firing Events
            if (UpdateNameLabels != null)
                UpdateNameLabels();

            if (UpdateScoreBoxes != null)
                UpdateScoreBoxes();

            if (UpdateLetterBoxes != null)
                UpdateLetterBoxes();

            if (UpdateTimeBox != null)
                UpdateTimeBox();

            // TODO : Restore values

            ActiveTimer.Start();
            TimeLeftTimer.Start();
        }

        /// <summary>
        /// Helper method to set gui to a completed state.
        /// </summary>
        void StatusCompleted()
        {
            ExitGameToolStrip.Enabled = false;

            if (ActiveUpdate != null)
                ActiveUpdate();

            // Setting TextBox properties values.
            JoinTimeBox.ReadOnly = true;
            JoinTimeBox.Enabled = false;
            CreateNameBox.ReadOnly = false;
            CreateNameBox.Enabled = true;
            JoinDomainBox.ReadOnly = false;
            JoinDomainBox.Enabled = true;
            WordBox.ReadOnly = true;
            WordBox.Enabled = false;

            // Setting button properties values.
            JoinButton.Enabled = false;
            CancelButton.Enabled = false;
            ExitGameToolStrip.Enabled = false;
            CreateButton.Enabled = true;

            if (UpdatePlayer1Words != null)
                UpdatePlayer1Words();
            if (UpdatePlayer2Words != null)
                UpdatePlayer2Words();

            TimeLeft = 0;
            JoinStatusBoxText = "completed";

            ActiveTimer.Stop();
            TimeLeftTimer.Stop();
        }

        /// <summary>
        /// On enter pressed...
        /// If Successful : Adjusts score.
        /// Otherwise :     Does nothing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && JoinStatusBoxText.Equals("active"))
            {
                if (Word != null)
                {
                    Word(WordBoxText);

                    if (UpdateScoreBoxes != null)
                        UpdateScoreBoxes();

                    WordBoxText = "";
                }
            }
        }

        private void ExitGameToolStrip_Click(object sender, EventArgs e)
        {
            JoinStatusBoxText = "completed";
            TimeLeft = 0;
            TimeBoxText = TimeLeft.ToString();
            CreateNameBox.Focus();

            StatusCompleted();

            MessageBox.Show("Game Exited, YOU QUITER!");
            JoinStatusBoxText = "Quit";
        }

        // Timers
        private void PendingTimer_Tick(object sender, EventArgs e)
        {
            if (UpdateStatus != null)
                UpdateStatus();

            if (JoinStatusBoxText.Equals("active"))
            {
                if (ActiveUpdate != null)
                {
                    ActiveUpdate();
                    PendingTimer.Stop();
                    StatusActive();
                }
            }
            if (JoinStatusBoxText.Equals("pending"))
            {
                StatusPending();
            }
        }

        private void ActiveTimer_Tick(object sender, EventArgs e)
        {
            if (JoinStatusBoxText.Equals("active"))
            {
                if (ActiveUpdate != null)
                {
                    ActiveUpdate();
                }
            }
        }

        private void TimeLeftTimer_Tick(object sender, EventArgs e)
        {
            if (TimeLeft-- >= 0)
            {
                if (UpdateScoreBoxes != null)
                    UpdateScoreBoxes();
            }
            else
            {
                StatusCompleted();
            }
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.MessageBox.Show(@"
                            Welcome to Boggle!
To start a game create a username with a valid 
domain to connect to an API. After creating a user 
name you can join a game by entering a time and 
then pressing join.

To cancel a pending game.
    click the cancel button.
To exit an ongoing game.
    click the Exit Game in the file menu.
To submit words while playing the game press 
    enter while in the words text field.

"
);
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            JoinGame(JoinTimeBoxText);
        }

        private void BackgroundWorkerName_DoWork(object sender, DoWorkEventArgs e)
        {
            JoinButton.Invoke((MethodInvoker)(() =>
            {
                JoinButton.Enabled = false;
            }));

            CreateName(CreateNameBoxText);
            if (Network.SetBaseAddress(JoinDomainBoxText))
            {
                StatusJoinGame();
            }
        }
    }
}

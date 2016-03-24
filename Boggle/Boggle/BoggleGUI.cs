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
            if (e.KeyCode == Keys.Enter && !JoinStatusBox.Text.Equals("active"))
            {
                JoinButton_Click(sender, e);
            }
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
            if (e.KeyCode == Keys.Enter && JoinStatusBox.Text.Equals("active"))
            {
                if (Word != null)
                {
                    Word(WordBoxText);

                    if(UpdateScoreBoxes != null)
                        UpdateScoreBoxes();

                    WordBox.Text = "";
                }
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
            //StatusTimer.Start();
            Player1PlayedBoxText = "";
            Player2PlayedBoxText = "";

            int temp;
            if (CreateName != null)
            {
                CreateName(CreateNameBoxText);
            }
            if (JoinGame != null && UpdateStatus != null && int.TryParse(JoinTimeBox.Text, out temp))
            {
                JoinGame(JoinTimeBoxText);
                UpdateStatus();
            }

            if (JoinStatusBox.Text.Equals("pending"))
            {
                PendingTimer.Start();

                ExitGameToolStrip.Enabled = false;
                CancelButton.Enabled = true;
            }

            if (JoinStatusBox.Text.Equals("active"))
            {
                StatusActive();
            }
        }
        void StatusActive()
        {
            // Setting TextBox properties to true or false.
            JoinTimeBox.ReadOnly = true;
            CreateNameBox.ReadOnly = true;
            JoinDomainBox.ReadOnly = true;
            // Setting Button properties to true or false.
            JoinButton.Enabled = false;
            CancelButton.Enabled = false;

            // Setting Wordbox properties to true or false.
            WordBox.Enabled = true;
            WordBox.ReadOnly = false;
            ExitGameToolStrip.Enabled = true;

            // Firing Events
            if (UpdateNameLabels != null)
                UpdateNameLabels();

            if (UpdateScoreBoxes != null)
                UpdateScoreBoxes();

            if (UpdateLetterBoxes != null)
                UpdateLetterBoxes();

            if (UpdateTimeBox != null)
                UpdateTimeBox();

            ActiveTimer.Start();
            TimeLeftTimer.Start();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            if (CancelJoin != null)
                CancelJoin();

            if (UpdateStatus != null)
                UpdateStatus();

            // Setting TextBox properties to true or false.
            JoinTimeBox.ReadOnly = false;
            CreateNameBox.ReadOnly = false;
            JoinDomainBox.ReadOnly = false;
            // Setting Button properties to true or false.
            JoinButton.Enabled = true;
            CancelButton.Enabled = false;
            ExitGameToolStrip.Enabled = false;

            // Setting Wordbox properties to true or false.
            WordBox.Enabled = false;
            WordBox.ReadOnly = true;

            JoinStatusBox.Text = "canceled";
            TimeBox.Text = "0";

            PendingTimer.Stop();
        }

        /// <summary>
        /// Activates on timer tick which interval is set to every second.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void StatusTimer_Tick(object sender, EventArgs e)
        //{
        //    if (JoinStatusBox.Text.Equals("active"))
        //    {
        //        if (ActiveUpdate != null)
        //            ActiveUpdate();
        //    }
        //    if (JoinStatusBox.Text.Equals("completed"))
        //    {
        //        StatusCompleted();
        //        StatusTimer.Stop();
        //    }
        //    if (JoinStatusBox.Text.Equals("canceled"))
        //    {
        //        StatusTimer.Stop();
        //    }
        //    if (UpdateStatus != null)
        //        UpdateStatus();
        //}
        void StatusCompleted()
        {
            ExitGameToolStrip.Enabled = false;

            if (ActiveUpdate != null)
                ActiveUpdate();

            // Setting TextBox properties to true or false.
            JoinTimeBox.ReadOnly = false;
            CreateNameBox.ReadOnly = false;
            JoinDomainBox.ReadOnly = false;
            // Setting Button properties to true or false.
            JoinButton.Enabled = true;
            CancelButton.Enabled = false;

            // Setting Wordbox properties to true or false.
            WordBox.Enabled = false;
            WordBox.ReadOnly = true;

            if (UpdatePlayer1Words != null)
                UpdatePlayer1Words();
            if (UpdatePlayer2Words != null)
                UpdatePlayer2Words();

            TimeLeft = 0;
            JoinStatusBoxText = "completed";

            ActiveTimer.Stop();
            TimeLeftTimer.Stop();
        }

        private void ExitGameToolStrip_Click(object sender, EventArgs e)
        {
            JoinStatusBox.Text = "completed";
            TimeLeft = 0;
            TimeBox.Text = TimeLeft.ToString();
            CreateNameBox.Focus();

            StatusCompleted();

            MessageBox.Show("Game Exited, YOU QUITER!");
        }

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
                JoinStatusBoxText = "completed";
                StatusCompleted();
            }
        }
    }
}

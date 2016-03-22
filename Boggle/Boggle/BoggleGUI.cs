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
    //View Class
    public partial class BoggleGUI : Form
    {
        // Properties to communicate with the controller and model/BoggleAPI.

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
        /// Sets the Message for a pop up window.
        /// </summary>
        public string Message { set { MessageBox.Show(value); } }

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

        public int WordScoreBoxText
        {
            set
            {
                WordScoreBox.Text = value.ToString();
            }
        }

        public string JoinStatusBoxText
        {
            set
            {
                JoinStatusBox.Text = value.ToString();
            }
        }

        public string TimeBoxText
        {
            get { return TimeBox.Text; }
            set { TimeBox.Text = value; }
        }

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

        public event Action UpdateStatus;

        public event Action UpdateNameLabels;

        public event Action UpdateTimeBox;

        // Constructor for BoggleGUI.
        /// <summary>
        /// Initializes a new instance of the <see cref="BoggleGUI"/> class.
        /// </summary>
        public BoggleGUI()
        {
            new Controller(this);
            InitializeComponent();
        }

        // Methods called by the GUI.
        /// <summary>
        /// On enter pressed...
        /// If Successful : Prompts player that username has been generated.
        /// Otherwise :     Prompts player that username has not been generated.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateNameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (CreateName != null)
                    CreateName(CreateNameBoxText);
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
            if (e.KeyCode == Keys.Enter)
            {
                if (Word != null)
                    Word(WordBoxText);
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
                JoinGame(JoinTimeBoxText);
                UpdateStatus();
            }

            if (JoinStatusBox.Text.Equals("active"))
            {
                timer.Start();

                JoinTimeBox.ReadOnly = true;
                CreateNameBox.ReadOnly = true;
                JoinButton.Enabled = false;
                CancelButton.Enabled = false;

                WordBox.Enabled = true;
                WordBox.ReadOnly = false;
            }

            if (JoinStatusBox.Text.Equals("active"))
            {
                if (UpdateNameLabels != null)
                    UpdateNameLabels();

            }
        }

        /// <summary>
        /// Activates on timer tick which interval is set to every second.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Tick(object sender, EventArgs e)
        {
            if (JoinStatusBox.Text.Equals("active"))
            {
                if (UpdateTimeBox != null)
                    UpdateTimeBox();
            }
        }
    }
}

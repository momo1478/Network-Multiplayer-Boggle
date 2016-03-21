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
        public int Time
        {
            get
            {
                int result;
                return int.TryParse(TimeBox.Text, out result) ? result : 0;
            }
            set
            {
                TimeBox.Text = value.ToString();
            }
        }
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

        public string Message { set { MessageBox.Show(value); } }

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


        public event Action<string> CreateName;

        public event Action<int> JoinGame;

        public BoggleGUI()
        {
            new Controller(this);
            InitializeComponent();
        }

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

        private void JoinButton_Click(object sender, EventArgs e)
        {
            timer.Start();
            TimeBox.Text = Time.ToString();

            int temp;
            if (JoinGame != null && int.TryParse(JoinTimeBox.Text , out temp) )
            {
                JoinGame(JoinTimeBoxText);
            }
        }
        private void timer_Tick(object sender, EventArgs e)
        {
            TimeBox.Text = Time.ToString();
            if (Time == 0)
            {
                timer.Stop();
            }
        }


    }
}

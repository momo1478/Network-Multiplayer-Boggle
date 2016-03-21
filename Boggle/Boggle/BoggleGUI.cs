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
        //Properties to comunicate with the server
        public int Time { get; set; }
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


        public event Action<string> CreateName;



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

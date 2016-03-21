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
        public string word { get; set; }
        public string Status { get; set; }
        public string Letter { get; set; }



        public BoggleGUI()
        {
            InitializeComponent();
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

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

        public Controller(BoggleGUI view)
        {
            View = view;

            view.CreateName += View_CreateName;
        }

        private void View_CreateName(string nickname)
        {
            string token = Network.CreateName(nickname);

            if (token != null)
            {
                View.Message = "User : " + nickname + "\nCreated with token : " + token;
            }
            else
            {
                View.Message = "Unable to create username";
            }

        }
    }
}

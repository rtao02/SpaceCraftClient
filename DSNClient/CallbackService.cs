using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using DSNInterfaces;

namespace SpaceCraftsClient
{
    public class DSNCallback : ICallbackService
    {
        public void SendCallbackMessage(string message)
        {
            //MessageBox.Show(message);
            SpaceCraftsClientForm.GetCMD(message);
        }
    }
}
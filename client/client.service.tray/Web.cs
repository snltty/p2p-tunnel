using System;
using System.Windows.Forms;

namespace client.service.tray
{
    public partial class Web : Form
    {
        public Web(string url)
        {
            MaximizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
          
            InitializeComponent();
            webView21.Source = new Uri(url);
        }
    }
}

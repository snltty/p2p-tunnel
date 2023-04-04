using System;
using System.Windows;
using System.Windows.Forms;

namespace client.service.tray
{
    /// <summary>
    /// Web.xaml 的交互逻辑
    /// </summary>
    public partial class Web : Window
    {

        Microsoft.Web.WebView2.Wpf.WebView2 webview;
        public Web(string url)
        {
            InitializeComponent();

            webview = new Microsoft.Web.WebView2.Wpf.WebView2();
            webview.Source = new Uri(url);
            grid.Children.Add(webview);
        }

        private new void Closing(object sender, FormClosingEventArgs e)
        {
            webview.Source = null;
            webview.Dispose();
            webview = null;
            grid.Children.Clear();
            GC.Collect();
            GC.SuppressFinalize(this);
        }
    }
}

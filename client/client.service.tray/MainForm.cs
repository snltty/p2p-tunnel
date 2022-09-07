using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace client.service.tray
{
    public partial class MainForm : Form
    {
        private NotifyIcon notifyIcon = null;
        private Process proc;
        public MainForm()
        {
            ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.Hide();
            InitializeComponent();
            InitialTray();
            OpenExe();
        }

        private void InitialTray()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.BalloonTipText = "p2p-tunnel托盘程序已启动";
            notifyIcon.Text = "p2p-tunnel托盘程序";

            notifyIcon.Icon = new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream(@"client.service.tray.logo.ico"));
            notifyIcon.Visible = true;

            notifyIcon.ContextMenuStrip = new ContextMenuStrip();
            notifyIcon.ContextMenuStrip.Items.Add("web管理页面");
            notifyIcon.ContextMenuStrip.Items.Add("退出");
            notifyIcon.ContextMenuStrip.ItemClicked += ContextMenuStrip_ItemClicked;
            notifyIcon.ContextMenuStrip.MouseDoubleClick += ContextMenuStrip_MouseDoubleClick;
        }

        private void ContextMenuStrip_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            OpenWeb();
        }
        private void ContextMenuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            switch (e.ClickedItem.Text)
            {
                case "退出":
                    this.Close();
                    break;
                case "web管理页面":
                    OpenWeb();
                    break;
                default:
                    break;
            }
        }

        private void OpenExe()
        {
            try
            {
                string dir = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "../");
                string file = Path.Combine(dir, "client.service.exe");

                proc = new Process();
                proc.StartInfo.WorkingDirectory = dir;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                proc.StartInfo.FileName = file;
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;

                proc.Start();
                // proc.StandardInput.AutoFlush = true;
                //proc.StandardInput.WriteLine("client.service.exe");
            }
            catch (System.Exception ex)
            {
                notifyIcon.BalloonTipText = ex.Message;
                notifyIcon.ShowBalloonTip(1000);
            }
        }

        private void OpenWeb()
        {
            string jsonstr = File.ReadAllText("../ui-appsettings.json").ToLower();
            System.Text.Json.JsonDocument jd = System.Text.Json.JsonDocument.Parse(jsonstr);
            string port = jd.RootElement.GetProperty("web").GetProperty("port").ToString();
            Process.Start("explorer.exe", $"http://127.0.0.1:{port}/");
        }

        private new void Closing(object sender, FormClosingEventArgs e)
        {
            proc.Close();
            proc.Dispose();
        }
    }
}
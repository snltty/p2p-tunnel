using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
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
        }

        Image unright = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream(@"client.service.tray.right1.png"));
        Image right = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream(@"client.service.tray.right.png"));
        private void InitialTray()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.BalloonTipTitle = "p2p-tunnel";
            notifyIcon.BalloonTipText = "p2p-tunnel托盘程序已启动";
            notifyIcon.Text = "p2p-tunnel托盘程序";

            notifyIcon.Icon = new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream(@"client.service.tray.logo.ico"));
            notifyIcon.Visible = true;

            notifyIcon.ContextMenuStrip = new ContextMenuStrip();
            notifyIcon.ContextMenuStrip.Items.Add("服务托管", unright, Service);
            notifyIcon.ContextMenuStrip.Items.Add("管理页面", null, OpenWeb);
            notifyIcon.ContextMenuStrip.Items.Add("退出", null, Close);
            notifyIcon.DoubleClick += ContextMenuStrip_MouseDoubleClick;

            Service(null, null);
        }

        private void ContextMenuStrip_MouseDoubleClick(object sender, EventArgs e)
        {
            OpenWeb(null, null);
        }

        private void Service(object sender, EventArgs e)
        {
            if (proc == null)
            {
                if (OpenExe())
                {
                    notifyIcon.BalloonTipText = "已托管服务";
                    notifyIcon.ContextMenuStrip.Items[0].Image = right;
                }
                else
                {
                    notifyIcon.BalloonTipText = "托管服务失败";
                    notifyIcon.ContextMenuStrip.Items[0].Image = unright;
                }
                notifyIcon.ShowBalloonTip(1000);
            }
            else
            {
                notifyIcon.BalloonTipText = "已取消托管服务";
                notifyIcon.ShowBalloonTip(1000);
                notifyIcon.ContextMenuStrip.Items[0].Image = unright;
                KillExe();
            }
        }
        private bool OpenExe()
        {
            try
            {
                string dir = Path.Combine(System.IO.Directory.GetCurrentDirectory());
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

                return true;
            }
            catch (Exception)
            {
                try
                {
                    proc.Kill();
                    proc.Dispose();
                }
                catch (Exception)
                {
                }
                proc = null;
                notifyIcon.BalloonTipText = "托管服务失败";
                notifyIcon.ShowBalloonTip(1000);
            }
            return false;
        }
        private void KillExe()
        {
            try
            {
                proc?.Kill();
                proc?.Dispose();

            }
            catch (Exception)
            {
            }
            finally
            {
                proc = null;
            }
        }

        private void OpenWeb(object sender, EventArgs e)
        {
            Web web = new Web("http://127.0.0.1:8080");
            web.Show();
        }
        private void Close(object sender, EventArgs e)
        {
            this.Close();
        }

        private new void Closing(object sender, FormClosingEventArgs e)
        {
            proc?.Close();
            proc?.Dispose();
        }
    }
}
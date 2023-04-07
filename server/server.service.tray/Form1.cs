using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace server.service.tray
{
    public partial class Form1 : Form
    {
        private NotifyIcon notifyIcon = null;
        private Process proc;
        public Form1()
        {
            ShowInTaskbar = false;
            Visible = false;
            InitializeComponent();
            Hide();
            InitialTray();
        }

        Image unright = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream(@"server.service.tray.right1.png"));
        Image right = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream(@"server.service.tray.right.png"));
        private void InitialTray()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.BalloonTipTitle = "p2p-tunnel";
            notifyIcon.BalloonTipText = "p2p-tunnel托盘程序已启动";
            notifyIcon.Text = "p2p-tunnel服务端托盘程序";

            notifyIcon.Icon = new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream(@"server.service.tray.logo.ico"));
            notifyIcon.Visible = true;

            notifyIcon.ContextMenuStrip = new ContextMenuStrip();
            notifyIcon.ContextMenuStrip.Items.Add("服务托管", unright, Service);
            notifyIcon.ContextMenuStrip.Items.Add("自启动", unright, StartUp);
            notifyIcon.ContextMenuStrip.Items.Add("退出", null, Close);

            Service(null, null);
            StartUp();
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
                string dir = Directory.GetCurrentDirectory();
                string file = Path.Combine(dir, "./server.service.exe");

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


        private Model GetReg()
        {
            string currentPath = Application.StartupPath;
            string exeName = AppDomain.CurrentDomain.FriendlyName;
            string value = string.Empty;
            Microsoft.Win32.RegistryKey Rkey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            string keyName = exeName.Replace(".exe", "");
            try
            {
                if (Rkey == null)
                {
                    Rkey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
                }
                try
                {
                    value = Rkey.GetValue(keyName).ToString();
                }
                catch (Exception)
                {
                }
            }
            catch (Exception)
            {
            }
            return new Model
            {
                Key = keyName,
                Value = value,
                Path = System.IO.Path.Combine(currentPath, exeName),
                RegKey = Rkey

            };
        }
        private void StartUp(object sender, EventArgs e)
        {
            Model model = GetReg();
            try
            {
                if (string.IsNullOrEmpty(model.Value))
                {
                    model.RegKey.SetValue(model.Key, model.Path);
                    notifyIcon.BalloonTipText = "已设置自启动";
                    notifyIcon.ShowBalloonTip(1000);
                }
                else
                {
                    model.RegKey.DeleteValue(model.Key, false);
                    notifyIcon.BalloonTipText = "已取消自启动";
                    notifyIcon.ShowBalloonTip(1000);
                }

                model.RegKey.Flush();
            }
            catch (Exception ex)
            {
                notifyIcon.BalloonTipText = ex.Message;
                notifyIcon.ShowBalloonTip(1000);
            }
            model.RegKey.Close();
            StartUp();
        }
        private void StartUp()
        {
            Model model = GetReg();
            if (string.IsNullOrEmpty(model.Value))
            {
                notifyIcon.ContextMenuStrip.Items[1].Image = unright;
            }
            else
            {
                notifyIcon.ContextMenuStrip.Items[1].Image = right;
            }
            model.RegKey.Close();
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


        class Model
        {
            public string Key { get; set; }
            public string Value { get; set; }
            public string Path { get; set; }
            public Microsoft.Win32.RegistryKey RegKey { get; set; }
        }
    }
}

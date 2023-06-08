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
        Image unright = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream(@"server.service.tray.right-gray.png"));
        Image right = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream(@"server.service.tray.right.png"));

        Icon icon = new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream(@"server.service.tray.logo.ico"));
        Icon iconGray = new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream(@"server.service.tray.logo-gray.ico"));

        string name = "p2p-tunnel服务端托盘程序";

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_APPWINDOW = 0x40000;
                const int WS_EX_TOOLWINDOW = 0x80;
                CreateParams cp = base.CreateParams;
                cp.ExStyle &= (~WS_EX_APPWINDOW);
                cp.ExStyle |= WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        public Form1()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.Hide();
            this.Opacity = 0;
            InitializeComponent();
            InitialTray();
        }
        private void InitialTray()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.BalloonTipTitle = name;
            notifyIcon.BalloonTipText = name + "已启动";
            notifyIcon.Text = name;

            notifyIcon.Icon = iconGray;
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
                    notifyIcon.Icon = icon;
                }
                else
                {
                    notifyIcon.BalloonTipText = "托管服务失败";
                    notifyIcon.ContextMenuStrip.Items[0].Image = unright;
                    notifyIcon.Icon = iconGray;
                }
                notifyIcon.ShowBalloonTip(1000);
            }
            else
            {
                notifyIcon.BalloonTipText = "已取消托管服务";
                notifyIcon.ShowBalloonTip(1000);
                notifyIcon.ContextMenuStrip.Items[0].Image = unright;
                notifyIcon.Icon = iconGray;
                KillExe();
            }
        }
        private bool OpenExe()
        {
            try
            {
                string filename = Process.GetCurrentProcess().MainModule.FileName;
                string dir = Path.GetDirectoryName(filename);
                string file = Path.Combine(dir, "./server.service.exe");

                ProcessStartInfo processStartInfo = new ProcessStartInfo()
                {
                    WorkingDirectory = dir,
                    FileName = file,
                    CreateNoWindow = false,
                    ErrorDialog = false,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Verb = "runas",
                };
                proc = Process.Start(processStartInfo);
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
                proc?.Close();
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


        private Model GetInfo()
        {
            string currentPath = Application.StartupPath;
            string exeName = AppDomain.CurrentDomain.FriendlyName;
            string keyName = exeName.Replace(".exe", "");
            return new Model
            {
                Key = keyName,
                Path = System.IO.Path.Combine(currentPath, exeName)
            };
        }
        bool isStartUp = false;
        private void StartUp(object sender, EventArgs e)
        {
            Model model = GetInfo();
            try
            {
                if (isStartUp == false)
                {
                    Command.Windows("schtasks.exe", new string[] {
                        "schtasks.exe /create /tn \""+model.Key+"\" /rl highest /sc ONSTART /delay 0000:30 /tr \""+model.Path+"\" /f"
                    });
                    notifyIcon.BalloonTipText = "已设置自启动";
                    notifyIcon.ShowBalloonTip(1000);
                }
                else
                {
                    Command.Windows("schtasks.exe", new string[] {
                        "schtasks /delete  /TN "+model.Key+" /f"
                    });
                    notifyIcon.BalloonTipText = "已取消自启动";
                    notifyIcon.ShowBalloonTip(1000);
                }
            }
            catch (Exception ex)
            {
                notifyIcon.BalloonTipText = ex.Message;
                notifyIcon.ShowBalloonTip(1000);
            }
            StartUp();
        }
        private void StartUp()
        {
            Model model = GetInfo();
            string res = Command.Windows("", new string[] {
                "schtasks.exe /query /fo TABLE|findstr \"" + model.Key + "\""
            });
            bool has = false;
            foreach (string item in res.Split('\n'))
            {
                if (item.StartsWith(model.Key))
                {
                    has = true;
                    break;
                }
            }

            if (has == false)
            {
                isStartUp = false;
                notifyIcon.ContextMenuStrip.Items[1].Image = unright;
            }
            else
            {
                notifyIcon.ContextMenuStrip.Items[1].Image = right;
                isStartUp = true;
            }
        }


        private void Close(object sender, EventArgs e)
        {
            KillExe();
            this.Close();
        }
        private new void Closing(object sender, FormClosingEventArgs e)
        {
            KillExe();
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

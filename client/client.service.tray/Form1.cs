using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace client.service.tray
{
    public partial class Form1 : Form
    {
        private NotifyIcon notifyIcon = null;
        private Process proc;
        Image unright = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream(@"client.service.tray.right-gray.png"));
        Image right = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream(@"client.service.tray.right.png"));

        Icon icon = new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream(@"client.service.tray.logo.ico"));
        Icon iconGray = new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream(@"client.service.tray.logo-gray.ico"));

        string name = "p2p-tunnel客户端托盘程序";

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
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

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
            notifyIcon.ContextMenuStrip.Items.Add("管理页面", null, OpenWeb);
            notifyIcon.ContextMenuStrip.Items.Add("退出", null, Close);
            notifyIcon.DoubleClick += ContextMenuStrip_MouseDoubleClick;

            WriteBat();
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
                string file = Path.Combine(dir, "./client.service.exe");
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

        private void OpenWeb(object sender, EventArgs e)
        {
            try
            {
                string path = Path.Combine(Application.StartupPath, "ui-appsettings.json");
                if (System.IO.File.Exists(path))
                {
                    string texts = System.IO.File.ReadAllText(path);
                    JObject jsObj = JObject.Parse(texts);
                    Process.Start($"http://127.0.0.1:{jsObj["Web"]["Port"]}/#/?port={jsObj["Websocket"]["Port"]}");
                }
                else
                {
                    notifyIcon.BalloonTipText = "未找到相应的配置文件,可以先运行客户端生成配置文件";
                    notifyIcon.ShowBalloonTip(1000);
                }
            }
            catch (Exception ex)
            {
                notifyIcon.BalloonTipText = ex.Message;
                notifyIcon.ShowBalloonTip(1000);
            }
        }
        private void ContextMenuStrip_MouseDoubleClick(object sender, EventArgs e)
        {
            OpenWeb(null, null);
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

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string resourceName = "client.service.tray." + new AssemblyName(args.Name).Name + ".dll";
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    byte[] assemblyData = new byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    return Assembly.Load(assemblyData);
                }

                return null;
            }
        }

        private void WriteBat()
        {
            string content = @"@echo off
cd  ""%CD%""
for /f ""tokens=4,5 delims=. "" %%a in ('ver') do if %%a%%b geq 60 goto new

:old
cmd /c netsh firewall delete allowedprogram program=""%CD%\client.service.exe"" profile=ALL
cmd /c netsh firewall add allowedprogram program=""%CD%\client.service.exe"" name=""client.service"" ENABLE
cmd /c netsh firewall add allowedprogram program=""%CD%\client.service.exe"" name=""client.service"" ENABLE profile=ALL
goto end
:new
cmd /c netsh advfirewall firewall delete rule name=""client.service""
cmd /c netsh advfirewall firewall add rule name=""client.service"" dir=in action=allow program=""%CD%\client.service.exe"" protocol=tcp enable=yes profile=public
cmd /c netsh advfirewall firewall add rule name=""client.service"" dir=in action=allow program=""%CD%\client.service.exe"" protocol=udp enable=yes profile=public
cmd /c netsh advfirewall firewall add rule name=""client.service"" dir=in action=allow program=""%CD%\client.service.exe"" protocol=tcp enable=yes profile=domain
cmd /c netsh advfirewall firewall add rule name=""client.service"" dir=in action=allow program=""%CD%\client.service.exe"" protocol=udp enable=yes profile=domain
cmd /c netsh advfirewall firewall add rule name=""client.service"" dir=in action=allow program=""%CD%\client.service.exe"" protocol=tcp enable=yes profile=private
cmd /c netsh advfirewall firewall add rule name=""client.service"" dir=in action=allow program=""%CD%\client.service.exe"" protocol=udp enable=yes profile=private
:end";
            System.IO.File.WriteAllText("firewall.bat", content);

            Command.Execute("firewall.bat", string.Empty, new string[0]);
        }

        class Model
        {
            public string Key { get; set; }
            public string Path { get; set; }
        }

        //lnk = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Startup), AppDomain.CurrentDomain.FriendlyName) + ".lnk";
        //static string lnk = "";
        /*
        private static void CreateShortcut(string args = "")
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            var shell = Activator.CreateInstance(shellType);
            var shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, shell, new object[] { lnk });

            var shortcutType = shortcut.GetType();
            shortcutType.InvokeMember("WindowStyle", BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty, null, shortcut, new object[] { 1 });
            shortcutType.InvokeMember("TargetPath", BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty, null, shortcut, new object[] { Assembly.GetEntryAssembly().Location });
            shortcutType.InvokeMember("Arguments", BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty, null, shortcut, new object[] { args });
            shortcutType.InvokeMember("WorkingDirectory", BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty, null, shortcut, new object[] { AppDomain.CurrentDomain.SetupInformation.ApplicationBase });
            shortcutType.InvokeMember("Save", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, shortcut, null);
        }
        */
    }
}

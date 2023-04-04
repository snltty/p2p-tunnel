using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using Newtonsoft.Json.Linq;

namespace client.service.tray
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon notifyIcon = null;
        private Process proc;
        public MainWindow()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            ShowInTaskbar = false;
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
            notifyIcon.ContextMenuStrip.Items.Add("自启动", unright, StartUp);
            notifyIcon.ContextMenuStrip.Items.Add("管理页面", null, OpenWeb);
            notifyIcon.ContextMenuStrip.Items.Add("退出", null, Close);
            notifyIcon.DoubleClick += ContextMenuStrip_MouseDoubleClick;

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
                string dir = Path.Combine(Directory.GetCurrentDirectory());
                string file = Path.Combine(dir, "../client.service.exe");

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


        private (string, string, string, Microsoft.Win32.RegistryKey) GetReg()
        {
            string currentPath = System.Windows.Forms.Application.StartupPath;
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
            return (keyName, value, System.IO.Path.Combine(currentPath, exeName), Rkey);

        }
        private void StartUp(object sender, EventArgs e)
        {
            (string keyName, string value, string path, Microsoft.Win32.RegistryKey Rkey) = GetReg();
            try
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    Rkey.SetValue(keyName, path);
                    notifyIcon.BalloonTipText = "已设置自启动";
                    notifyIcon.ShowBalloonTip(1000);
                }
                else
                {
                    Rkey.DeleteValue(keyName, false);
                    notifyIcon.BalloonTipText = "已取消自启动";
                    notifyIcon.ShowBalloonTip(1000);
                }

                Rkey.Flush();
            }
            catch (Exception ex)
            {
                notifyIcon.BalloonTipText = ex.Message;
                notifyIcon.ShowBalloonTip(1000);
            }
            Rkey.Close();
            StartUp();
        }
        private void StartUp()
        {
            (string keyName, string value, string path, Microsoft.Win32.RegistryKey Rkey) = GetReg();
            if (string.IsNullOrWhiteSpace(value))
            {
                notifyIcon.ContextMenuStrip.Items[1].Image = unright;
            }
            else
            {
                notifyIcon.ContextMenuStrip.Items[1].Image = right;
            }
            Rkey.Close();
        }


        Web web;
        private void OpenWeb(object sender, EventArgs e)
        {
            if (File.Exists("../ui-appsettings.json"))
            {
                string texts = File.ReadAllText("../ui-appsettings.json");
                JObject jsObj = JObject.Parse(texts);
                web = new Web($"http://127.0.0.1:{jsObj["web"]["Port"]}");
                web.Show();
                web.Closed += Web_Closed;
            }
            else
            {
                notifyIcon.BalloonTipText = "未找到相应的配置文件";
                notifyIcon.ShowBalloonTip(1000);
            }
        }
        private void ContextMenuStrip_MouseDoubleClick(object sender, EventArgs e)
        {
            OpenWeb(null, null);
        }
        private void Web_Closed(object sender, EventArgs e)
        {
            web = null;
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



        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string resourceName = "client.service.tray.res." + new AssemblyName(args.Name).Name + ".dll";
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
    }
}

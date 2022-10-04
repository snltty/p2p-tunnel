using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace common.libs
{
    /// <summary>
    /// 代理系统设置
    /// </summary>
    public class ProxySystemSetting
    {
        private static void WindowsSet(string url)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    string CurrentUserSID = string.Empty;
                    if (GetCurrentUserSID(ref CurrentUserSID))
                    {
                        RegistryKey rsg = Registry.Users.OpenSubKey($"{CurrentUserSID}\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
                        rsg.SetValue("AutoConfigURL", url);
                        rsg.Close();
                    }
                    FlushOs();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }
        private static void WindowsClear()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    string CurrentUserSID = string.Empty;
                    if (GetCurrentUserSID(ref CurrentUserSID))
                    {
                        RegistryKey rsg = Registry.Users.OpenSubKey($"{CurrentUserSID}\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
                        rsg.DeleteValue("AutoConfigURL");
                        rsg.Close();
                    }
                    FlushOs();
                }
            }
            catch (Exception)
            {

            }
        }

        private static void MacExcute(string command)
        {
            Process proc = new();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                proc.StartInfo.FileName = "cmd.exe";
            }
            else
            {
                proc.StartInfo.FileName = "/bin/bash";
            }
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.Verb = "runas";
            proc.Start();

            proc.StandardInput.WriteLine(command);
            proc.StandardInput.AutoFlush = true;
            proc.StandardInput.WriteLine("exit");
            proc.StandardOutput.ReadToEnd();
            proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            proc.Close();
            proc.Dispose();
        }
        private static void MacSet(string url)
        {
            MacExcute($"networksetup -setautoproxyurl ethernet {url}");
            MacExcute($"networksetup -setautoproxyurl Wi-Fi {url}");
        }
        private static void MacClear()
        {
            MacExcute($"networksetup -setautoproxystate ethernet off");
            MacExcute($"networksetup -setautoproxystate Wi-Fi off");
        }

        public static void Set(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                WindowsSet(url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                MacSet(url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {

            }
        }
        public static void Clear()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                WindowsClear();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                MacClear();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {

            }
        }


        #region windows

        [DllImport("wininet.dll")]
        static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        const int INTERNET_OPTION_REFRESH = 37;
        private static void FlushOs()
        {
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }

        public static bool GetCurrentUserToken(ref IntPtr hCurrentUserToken)
        {
            uint dwSessionId = WTSGetActiveConsoleSessionId();
            if (!WTSQueryUserToken(dwSessionId, ref hCurrentUserToken))
            {
                return false;
            }
            return true;
        }
        public static bool GetCurrentUserSID(ref string CurrentUserSID)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                IntPtr hUserToken = IntPtr.Zero;
                if (GetCurrentUserToken(ref hUserToken))
                {
                    if (!ImpersonateLoggedOnUser(hUserToken))//Impersonate Current User logon
                    {
                        return false;
                    }
                    CloseHandle(hUserToken);

                    WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();
                    CurrentUserSID = windowsIdentity.User.ToString();

                    if (!RevertToSelf())
                    {
                    }
                    return true;
                }
            }
            return false;
        }
        

        [DllImport("Advapi32.dll", EntryPoint = "ImpersonateLoggedOnUser", SetLastError = true)]
        private static extern bool ImpersonateLoggedOnUser(IntPtr hToken);

        [DllImport("Advapi32.dll", EntryPoint = "RevertToSelf", SetLastError = true)]
        private static extern bool RevertToSelf();

        [DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hSnapshot);

        [DllImport("kernel32.dll", EntryPoint = "WTSGetActiveConsoleSessionId")]
        private static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("Wtsapi32.dll", EntryPoint = "WTSQueryUserToken", SetLastError = true)]
        private static extern bool WTSQueryUserToken(uint SessionId, ref IntPtr hToken);
        #endregion
    }
}

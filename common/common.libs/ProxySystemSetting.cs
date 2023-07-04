using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace common.libs
{
    /// <summary>
    /// 代理系统设置
    /// </summary>
    public sealed class ProxySystemSetting
    {
        /// <summary>
        /// windows 下， “应用级”也就是直接运行exe时，可以直接修改当前用户的注册表
        /// 而作为 windows service时，无法修改，需要模拟登录当前用户，才能修改，而模拟登录还需要SE_TCB_NAME权限，并不能保证一定修改成功，这就增加了复杂性。
        /// 所以，直接获取 HKEY_USERS 下的所有key，直接修改所有用户的注册表，反而更简单
        /// </summary>
        /// <param name="url"></param>
        private static void WindowsSet(string pacUrl, string proxyUrl)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    string[] names = GetWindowsCurrentIds();
                    foreach (var item in names)
                    {
                        try
                        {
                            RegistryKey reg = Registry.Users.OpenSubKey($"{item}\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
                            reg.SetValue("AutoConfigURL", pacUrl);
                            reg.Close();
                        }
                        catch (Exception)
                        {
                        }
                    }
                    Command.Windows(string.Empty, new string[] { $"setx http_proxy {proxyUrl} -m", $"setx https_proxy {proxyUrl} -m" });

                    FlushOs();
                }
            }
            catch (Exception)
            {
            }
        }
        private static void WindowsClear()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    string[] names = GetWindowsCurrentIds();
                    foreach (var item in names)
                    {
                        try
                        {
                            RegistryKey reg = Registry.Users.OpenSubKey($"{item}\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
                            reg.DeleteValue("AutoConfigURL");
                            reg.Close();
                        }
                        catch (Exception)
                        {
                        }
                    }
                    Command.Windows(string.Empty, new string[] { $"setx http_proxy \"\" -m", $"setx https_proxy \"\" -m" });

                    FlushOs();
                }
            }
            catch (Exception)
            {
            }
        }

        private static void MacSet(string pacUrl, string proxyUrl)
        {
            Command.Osx(string.Empty, new string[] { $"networksetup -setautoproxyurl ethernet {pacUrl}" });
            Command.Osx(string.Empty, new string[] { $"networksetup -setautoproxyurl Wi-Fi {pacUrl}" });
        }
        private static void MacClear()
        {
            Command.Osx(string.Empty, new string[] { $"networksetup -setautoproxystate ethernet off" });
            Command.Osx(string.Empty, new string[] { $"networksetup -setautoproxystate Wi-Fi off" });
        }

        public static void Set(string pacUrl, string proxyUrl)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                WindowsSet(pacUrl, proxyUrl);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                MacSet(pacUrl, proxyUrl);
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

        private static string[] GetWindowsCurrentIds()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Registry.Users.GetSubKeyNames().Where(c => c.Length > 10 && c.Contains("Classes") == false).ToArray();
            }
            return Array.Empty<string>();
        }
        [DllImport("wininet.dll")]
        static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        const int INTERNET_OPTION_REFRESH = 37;
        private static void FlushOs()
        {
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }
        #endregion
    }
}

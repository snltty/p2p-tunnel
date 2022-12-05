using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace common.libs
{
    /// <summary>
    /// 
    /// </summary>
    public static class GCHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="proc"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        public static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);
        /// <summary>
        /// 
        /// </summary>
        public static void FlushMemory()
        {
            GC.Collect();
            GC.SuppressFinalize(true);
            GC.WaitForPendingFinalizers();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        public static void Gc(object obj)
        {
            GC.Collect();
            GC.SuppressFinalize(obj);
        }
    }
}

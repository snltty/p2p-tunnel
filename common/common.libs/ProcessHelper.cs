using System;
using System.Diagnostics;

namespace common.libs
{
    /// <summary>
    /// 
    /// </summary>
    public static class ProcessHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Process GetCurrentProcess()
        {
            return Process.GetCurrentProcess();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="proc"></param>
        /// <returns></returns>
        public static double GetMemory(Process proc)
        {
            double b = proc.WorkingSet64 / 1024.0 / 1024.0;
            return Math.Round(b, 2);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static double GetMemory2()
        {
            double b = Environment.WorkingSet / 1024.0 / 1024.0;
            return Math.Round(b, 2);
        }
       

        private static DateTime lastTime = DateTime.UtcNow;
        private static TimeSpan lastProcessTime = TimeSpan.Zero;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="proc"></param>
        /// <returns></returns>
        public static double GetCpu(Process proc)
        {
            DateTime time = DateTime.UtcNow;
            TimeSpan processTime = proc.TotalProcessorTime;

            double cpuUsedMs = (processTime - lastProcessTime).TotalMilliseconds;
            double totalMsPassed = (time - lastTime).TotalMilliseconds;

            lastTime = time;
            lastProcessTime = processTime;

            double cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

            return Math.Round(cpuUsageTotal * 100, 2);
        }
    }
}

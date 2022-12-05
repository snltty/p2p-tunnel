using System;

namespace common.libs
{
    /// <summary>
    /// 
    /// </summary>
    public static class DateTimeHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static long GetTimeStamp()
        {
            return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
        }
    }
}

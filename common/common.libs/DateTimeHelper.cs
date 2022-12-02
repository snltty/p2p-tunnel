using System;

namespace common.libs
{
    public static class DateTimeHelper
    {
        public static long GetTimeStamp()
        {
            return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
        }
    }
}

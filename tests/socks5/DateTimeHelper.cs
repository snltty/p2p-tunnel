namespace socks5
{
    public static class DateTimeHelper
    {
        public static long GetTimeStamp()
        {
            return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
        }
    }
}

using System;

namespace common.libs
{
    public class SnowflakeIdGenerator
    {
        private const long Twepoch = 1288834974657L;

        // 每个部分占用的位数
        private const int DatacenterIdBits = 5;
        private const int WorkerIdBits = 5;
        private const int SequenceBits = 12;

        // 最大值
        private const long MaxDatacenterId = -1L ^ (-1L << DatacenterIdBits);
        private const long MaxWorkerId = -1L ^ (-1L << WorkerIdBits);
        private const long SequenceMask = -1L ^ (-1L << SequenceBits);

        // 偏移量
        private readonly int _datacenterIdShift = WorkerIdBits + SequenceBits;
        private readonly int _workerIdShift = SequenceBits;
        private readonly int _timestampLeftShift = DatacenterIdBits + WorkerIdBits + SequenceBits;

        private readonly object _lockObj = new object();

        private long _lastTimestamp = -1L;
        private long _sequence = 0L;

        public SnowflakeIdGenerator(long datacenterId, long workerId)
        {
            if (datacenterId > MaxDatacenterId || datacenterId < 0)
            {
                throw new ArgumentException($"Datacenter ID can't be greater than {MaxDatacenterId} or less than 0");
            }
            if (workerId > MaxWorkerId || workerId < 0)
            {
                throw new ArgumentException($"Worker ID can't be greater than {MaxWorkerId} or less than 0");
            }

            DatacenterId = datacenterId;
            WorkerId = workerId;
        }

        public long DatacenterId { get; }
        public long WorkerId { get; }

        public long NextId()
        {
            lock (_lockObj)
            {
                var timestamp = TimeGen();

                if (timestamp < _lastTimestamp)
                {
                    throw new InvalidOperationException("Clock moved backwards, refusing to generate ID");
                }

                if (_lastTimestamp == timestamp)
                {
                    // 在同一毫秒内多次生成ID
                    _sequence = (_sequence + 1) & SequenceMask;
                    if (_sequence == 0)
                    {
                        // sequence用尽，等待下一毫秒
                        timestamp = NextMillis(_lastTimestamp);
                    }
                }
                else
                {
                    // 不同毫秒内，序列号从0开始
                    _sequence = 0L;
                }

                _lastTimestamp = timestamp;

                // 时间戳向左移动，datacenter ID和worker ID进行位运算后合并成一个数字
                var id = ((timestamp - Twepoch) << _timestampLeftShift)
                         | (DatacenterId << _datacenterIdShift)
                         | (WorkerId << _workerIdShift)
                         | _sequence;

                return id;
            }
        }

        private static long TimeGen()
        {
            return DateTimeOffset.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
        }

        private static long NextMillis(long lastTimestamp)
        {
            var timestamp = TimeGen();
            while (timestamp <= lastTimestamp)
            {
                timestamp = TimeGen();
            }
            return timestamp;
        }
    }
}

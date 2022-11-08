using System.Threading;

namespace common.libs
{
    public class NumberSpaceDefault
    {
        private int num = 0;

        public NumberSpaceDefault(int defaultVal = 0)
        {
            num = defaultVal;
        }

        public int Get()
        {
            return num;
        }

        public int Increment()
        {
            Interlocked.Increment(ref num);
            return num;
        }

        public void Decrement()
        {
            Interlocked.Decrement(ref num);
        }

        public void Reset(int val = 0)
        {
            Interlocked.Exchange(ref num, val);
        }
    }

    public class NumberSpace
    {
        private ulong num;

        public NumberSpace(ulong defaultVal = 0)
        {
            num = defaultVal;
        }

        public ulong Get()
        {
            return num;
        }

        public ulong Increment()
        {
            Interlocked.Increment(ref num);
            return num;
        }

        public void Decrement()
        {
            Interlocked.Decrement(ref num);
        }

        public void Reset(ulong val = 0)
        {
            Interlocked.Exchange(ref num, val);
        }
    }


    public class NumberSpaceInt32
    {
        private int num = 0;

        public NumberSpaceInt32(int defaultVal = 0)
        {
            num = defaultVal;
        }

        public int Get()
        {
            return num;
        }

        public int Increment()
        {
            Interlocked.Increment(ref num);
            return num;
        }

        public void Decrement()
        {
            Interlocked.Decrement(ref num);
        }

        public void Reset(int val = 0)
        {
            Interlocked.Exchange(ref num, val);
        }
    }

    public class NumberSpaceUInt32
    {
        private uint num = 0;

        public NumberSpaceUInt32(uint defaultVal = 0)
        {
            num = defaultVal;
        }

        public uint Get()
        {
            return num;
        }

        public uint Increment()
        {
            if (uint.MaxValue - 10000 < num)
            {
                Reset(0);
            }
            Interlocked.Increment(ref num);
            return num;
        }

        public void Decrement()
        {
            Interlocked.Decrement(ref num);
        }

        public void Reset(uint val = 0)
        {
            Interlocked.Exchange(ref num, val);
        }
    }

    public struct BoolSpace
    {
        bool _default;
        private bool value;
        public BoolSpace(bool defaultVal = true)
        {
            _default = defaultVal;
            value = _default;
        }

        public bool Get()
        {
            return value;
        }

        public bool Reverse()
        {
            value = !_default;
            return value;
        }

        public void Reset()
        {
            value = _default;
        }
    }

    public class RequestIdNumberSpace
    {
        private ulong num = 0;
        public RequestIdNumberSpace()
        {
        }

        public void Increment(ref ulong id)
        {
            Interlocked.CompareExchange(ref id, 0, Interlocked.Increment(ref num));
        }
    }
}

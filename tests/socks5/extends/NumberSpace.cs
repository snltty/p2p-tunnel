namespace socks5
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class NumberSpace
    {
        private ulong num;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="defaultVal"></param>
        public NumberSpace(ulong defaultVal = 0)
        {
            num = defaultVal;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ulong Get()
        {
            return num;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ulong Increment()
        {
            Interlocked.CompareExchange(ref num, 0, ulong.MaxValue - 10000);
            Interlocked.Increment(ref num);
            return num;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Decrement()
        {
            Interlocked.Decrement(ref num);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="val"></param>
        public void Reset(ulong val = 0)
        {
            Interlocked.Exchange(ref num, val);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class NumberSpaceUInt32
    {
        private uint num = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="defaultVal"></param>
        public NumberSpaceUInt32(uint defaultVal = 0)
        {
            num = defaultVal;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public uint Get()
        {
            return num;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public uint Increment()
        {
            Interlocked.CompareExchange(ref num, 0, uint.MaxValue - 10000);
            Interlocked.Increment(ref num);
            return num;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Decrement()
        {
            Interlocked.Decrement(ref num);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="val"></param>
        public void Reset(uint val = 0)
        {
            Interlocked.Exchange(ref num, val);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class BoolSpace
    {
        bool _default;
        private bool value;
        /// <summary>
        /// /
        /// </summary>
        /// <param name="defaultVal"></param>
        public BoolSpace(bool defaultVal = true)
        {
            _default = defaultVal;
            value = _default;
        }

        /// <summary>
        /// 是否是原始值
        /// </summary>
        public bool IsDefault => value == _default;
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool Reverse()
        {
            value = !_default;
            return value;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Reset()
        {
            value = _default;
        }
    }
}

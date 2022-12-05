using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace common.libs
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MemoryByteDictionaryComparer : IEqualityComparer<ReadOnlyMemory<byte>>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y)
        {
            return x.Span.SequenceEqual(y.Span);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode([DisallowNull] ReadOnlyMemory<byte> obj)
        {
            return 0;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class IPEndPointDictionaryComparer : IEqualityComparer<IPEndPoint>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(IPEndPoint x, IPEndPoint y)
        {
            return x.Equals(y);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(IPEndPoint obj)
        {
            return obj.GetHashCode();
        }
    }
}

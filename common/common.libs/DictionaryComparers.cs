using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace common.libs
{

    public class MemoryByteDictionaryComparer : IEqualityComparer<ReadOnlyMemory<byte>>
    {
        public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y)
        {
            return x.Span.SequenceEqual(y.Span);
        }

        public int GetHashCode([DisallowNull] ReadOnlyMemory<byte> obj)
        {
            return 0;
        }
    }
}

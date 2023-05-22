using common.proxy;
using System;
using System.Collections.Generic;

namespace common.forward
{
    public interface IForwardTargetProvider
    {
        bool Contains(ushort port);
        void Get(string domain, ProxyInfo info);
        void Get(ushort port, ProxyInfo info);
    }

    public interface IForwardTargetCaching<T>
    {
        T Get(string host);
        T Get(string domain, ushort port);
        T Get(ushort port);
        bool Add(string domain, ushort port, T mdoel);
        bool Add(ushort port, T mdoel);
        void AddOrUpdate(string domain, ushort port, T mdoel);
        void AddOrUpdate(ushort port, T mdoel);
        bool Remove(string domain, ushort port);
        bool Remove(ushort port);
        List<ushort> Remove(ulong id);
        bool Contains(string domain, ushort port);
        bool Contains(ushort port);
        void ClearConnection();
        void ClearConnection(ulong id);
    }
}
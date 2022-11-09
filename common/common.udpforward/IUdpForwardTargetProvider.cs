using System.Collections.Generic;

namespace common.udpforward
{
    public interface IUdpForwardTargetProvider
    {
        void Get(ushort sourcePort, UdpForwardInfo info);
    }
    public interface IUdpForwardTargetCaching<T>
    {
        T Get(ushort port);
        bool Add(ushort port, T mdoel);
        bool Remove(ushort port);
        IEnumerable<ushort> Remove(string targetName);
        void ClearConnection();
        void ClearConnection(string name);
    }
}
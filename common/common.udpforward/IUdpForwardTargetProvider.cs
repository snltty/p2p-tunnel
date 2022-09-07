using System.Collections.Generic;

namespace common.udpforward
{
    public interface IUdpForwardTargetProvider
    {
        void Get(int sourcePort, UdpForwardInfo info);
    }
    public interface IUdpForwardTargetCaching<T>
    {
        T Get(int port);
        bool Add(int port, T mdoel);
        bool Remove(int port);
        IEnumerable<int> Remove(string targetName);
        void ClearConnection();
        void ClearConnection(string name);
    }
}
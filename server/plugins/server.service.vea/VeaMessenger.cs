using common.server;
using common.vea;

namespace server.service.vea
{
    /// <summary>
    /// 组网消息
    /// </summary>
    [MessengerIdRange((ushort)VeaSocks5MessengerIds.Min, (ushort)VeaSocks5MessengerIds.Max)]
    public sealed class VeaMessenger : IMessenger
    {


        public VeaMessenger()
        {
        }

        [MessengerId((ushort)VeaSocks5MessengerIds.Network)]
        public void Network(IConnection connection)
        {

        }

        [MessengerId((ushort)VeaSocks5MessengerIds.AddNetwork)]
        public void AddNetwork(IConnection connection)
        {

        }

        [MessengerId((ushort)VeaSocks5MessengerIds.AssignIP)]
        public void AssignIP(IConnection connection)
        {

        }
    }



}

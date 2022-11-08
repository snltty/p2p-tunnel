using common.libs;
using common.server;
using common.server.model;

namespace client.realize.messengers.heart
{
    /// <summary>
    /// 心跳包
    /// </summary>
    [MessengerIdRange((ushort)HeartMessengerIds.Min, (ushort)HeartMessengerIds.Max)]
    public class HeartMessenger : IMessenger
    {
        public HeartMessenger()
        {
        }

        [MessengerId((ushort)HeartMessengerIds.Alive)]
        public byte[] Alive(IConnection connection)
        {
            return Helper.TrueArray;
        }
    }
}

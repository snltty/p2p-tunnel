using common.libs;
using common.server;
using common.server.model;

namespace server.service.messengers
{
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

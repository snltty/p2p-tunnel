using common.libs;
using common.server;
using common.server.model;

namespace server.service.messengers
{
    [MessengerIdRange((int)HeartMessengerIds.Min, (int)HeartMessengerIds.Max)]
    public class HeartMessenger : IMessenger
    {
        public HeartMessenger()
        {
        }

        [MessengerId((int)HeartMessengerIds.Alive)]
        public byte[] Alive(IConnection connection)
        {
            return Helper.TrueArray;
        }
    }
}

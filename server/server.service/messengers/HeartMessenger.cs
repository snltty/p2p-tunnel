using common.libs;
using common.server;
using common.server.model;

namespace server.service.messengers
{
    [MessengerIdRange((ushort)HeartMessengerIds.Min, (ushort)HeartMessengerIds.Max)]
    public sealed class HeartMessenger : IMessenger
    {
        public HeartMessenger()
        {
        }

        [MessengerId((ushort)HeartMessengerIds.Alive)]
        public void Alive(IConnection connection)
        {
            connection.Write(Helper.TrueArray);
        }
    }
}

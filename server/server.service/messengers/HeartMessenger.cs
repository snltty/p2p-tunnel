using common.libs;
using common.server;

namespace server.service.messengers
{
    public class HeartMessenger : IMessenger
    {
        public HeartMessenger()
        {
        }

        public byte[] Execute(IConnection connection)
        {
            return Helper.TrueArray;
        }
        public byte[] Alive(IConnection connection)
        {
            return Helper.TrueArray;
        }
    }
}

using common.libs;
using common.server;

namespace server.service.messengers
{
    public class ExitMessenger : IMessenger
    {
        public ExitMessenger()
        {
        }

        public void Execute(IConnection connection)
        {
            connection.Disponse();
        }
    }
}

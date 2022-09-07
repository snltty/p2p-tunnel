using client.messengers.clients;
using common.server;
using System.Threading.Tasks;

namespace client.service.ftp.server
{
    public class FtpServerMessenger : FtpMessengerBase
    {
        public FtpServerMessenger(Config config, FtpServer ftpServer, IClientInfoCaching clientInfoCaching) : base(config, clientInfoCaching, ftpServer.Plugins)
        {
        }

        public new async Task<byte[]> Execute(IConnection connection)
        {
            return await base.Execute(connection).ConfigureAwait(false);
        }
    }
}

using client.messengers.clients;
using common.server;
using System.Threading.Tasks;

namespace client.service.ftp.client
{
    public class FtpClientMessenger : FtpMessengerBase
    {
        public FtpClientMessenger(Config config, FtpClient ftpClient, IClientInfoCaching clientInfoCaching) : base(config, clientInfoCaching, ftpClient.Plugins)
        {
        }

        public new async Task<byte[]> Execute(IConnection connection)
        {
            return await base.Execute(connection).ConfigureAwait(false);
        }
    }
}

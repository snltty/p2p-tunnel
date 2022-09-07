using client.service.ftp.commands;
using common.libs.extends;
using System.Threading.Tasks;

namespace client.service.ftp.server.plugin
{
    public class ListPlugin : IFtpCommandServerPlugin
    {
        private readonly FtpServer ftpServer;

        public ListPlugin(FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
        }
        public FtpCommand Cmd => FtpCommand.LIST;

        public async Task<FtpResultInfo> Execute(FtpPluginParamWrap arg)
        {
            await Task.Yield();

            FtpListCommand cmd = new FtpListCommand();
            cmd.DeBytes(arg.Connection.ReceiveRequestWrap.Memory);
            return new FtpResultInfo { Data = ftpServer.GetFiles(cmd, arg).ToBytes() };
        }
    }
}
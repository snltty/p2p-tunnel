using client.service.ftp.commands;
using common.libs.extends;
using System.Threading.Tasks;

namespace client.service.ftp.server.plugin
{
    public class CurrentPathPlugin : IFtpCommandServerPlugin
    {
        private readonly FtpServer ftpServer;

        public CurrentPathPlugin(FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
        }
        public FtpCommand Cmd => FtpCommand.CURRENT_PATH;

        public async Task<FtpResultInfo> Execute(FtpPluginParamWrap arg)
        {
            return await Task.FromResult(new FtpResultInfo
            {
                Data = ftpServer.GetCurrentPath(arg.Client.Id).ToBytes()
            }).ConfigureAwait(false);
        }
    }
}
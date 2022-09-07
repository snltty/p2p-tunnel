using client.service.ftp.commands;
using System.Threading.Tasks;

namespace client.service.ftp.server.plugin
{
    public class SetCurrentPathPlugin : IFtpCommandServerPlugin
    {
        private readonly FtpServer ftpServer;

        public SetCurrentPathPlugin(FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
        }
        public FtpCommand Cmd => FtpCommand.CURRENT_PATH_SET;

        public async Task<FtpResultInfo> Execute(FtpPluginParamWrap arg)
        {
            FtpSetCurrentPathCommand cmd = new FtpSetCurrentPathCommand();
            cmd.DeBytes(arg.Connection.ReceiveRequestWrap.Memory);

            ftpServer.SetCurrentPath(cmd, arg);

            await Task.CompletedTask;

            return new FtpResultInfo();
        }
    }
}
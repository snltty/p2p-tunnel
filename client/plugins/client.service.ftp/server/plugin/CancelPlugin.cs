using client.service.ftp.commands;
using System.Threading.Tasks;

namespace client.service.ftp.server.plugin
{
    public class CancelPlugin : IFtpCommandServerPlugin
    {
        public FtpCommand Cmd => FtpCommand.FILE_CANCEL;

        private readonly FtpServer ftpServer;
        public CancelPlugin(FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
        }

        public async Task<FtpResultInfo> Execute(FtpPluginParamWrap arg)
        {
            FtpCancelCommand cmd = new FtpCancelCommand();
            cmd.DeBytes(arg.Connection.ReceiveRequestWrap.Memory);
            await ftpServer.OnFileUploadCancel(cmd, arg).ConfigureAwait(false);
            return new FtpResultInfo();
        }
    }

    public class CanceledPlugin : IFtpCommandServerPlugin
    {
        public FtpCommand Cmd => FtpCommand.FILE_CANCELED;

        private readonly FtpServer ftpServer;
        public CanceledPlugin(FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
        }

        public async Task<FtpResultInfo> Execute(FtpPluginParamWrap arg)
        {
            await Task.Yield();
            FtpCanceledCommand cmd = new FtpCanceledCommand();
            cmd.DeBytes(arg.Connection.ReceiveRequestWrap.Memory);
            ftpServer.OnFileUploadCanceled(cmd, arg);
            return new FtpResultInfo();
        }
    }
}

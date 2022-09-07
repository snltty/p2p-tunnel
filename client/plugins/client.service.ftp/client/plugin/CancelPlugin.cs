using client.service.ftp.commands;
using System.Threading.Tasks;

namespace client.service.ftp.client.plugin
{
    public class CanceledPlugin : IFtpCommandClientPlugin
    {
        public FtpCommand Cmd => FtpCommand.FILE_CANCELED;

        private readonly FtpClient ftpClient;
        public CanceledPlugin(FtpClient ftpClient)
        {
            this.ftpClient = ftpClient;
        }

        public async Task<FtpResultInfo> Execute(FtpPluginParamWrap arg)
        {
            await Task.Yield();

            FtpCanceledCommand cmd = new FtpCanceledCommand();
            cmd.DeBytes(arg.Connection.ReceiveRequestWrap.Memory);

            ftpClient.OnFileUploadCanceled(cmd, arg);

            return new FtpResultInfo();
        }
    }
}

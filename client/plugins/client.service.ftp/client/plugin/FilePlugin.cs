using client.service.ftp.commands;
using System.Threading.Tasks;

namespace client.service.ftp.client.plugin
{
    public class FilePlugin : IFtpCommandClientPlugin
    {
        private readonly FtpClient ftpClient;
        public FilePlugin(FtpClient ftpClient)
        {
            this.ftpClient = ftpClient;
        }
        public FtpCommand Cmd => FtpCommand.FILE;

        public async Task<FtpResultInfo> Execute(FtpPluginParamWrap data)
        {
            FtpFileCommand cmd = new FtpFileCommand();
            cmd.DeBytes(data.Connection.ReceiveRequestWrap.Memory);
            await ftpClient.OnFile(cmd, data).ConfigureAwait(false);
            return null;
        }
    }

    public class FileEndPlugin : IFtpCommandClientPlugin
    {
        private readonly FtpClient ftpClient;
        public FileEndPlugin(FtpClient ftpClient)
        {
            this.ftpClient = ftpClient;
        }
        public FtpCommand Cmd => FtpCommand.FILE_END;

        public async Task<FtpResultInfo> Execute(FtpPluginParamWrap arg)
        {
            await Task.Yield();
            FtpFileEndCommand cmd = new FtpFileEndCommand();
            cmd.DeBytes(arg.Connection.ReceiveRequestWrap.Memory);
            ftpClient.OnFileEnd(cmd, arg);
            return null;
        }
    }
}

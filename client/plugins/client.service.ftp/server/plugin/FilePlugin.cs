using client.service.ftp.commands;
using System.Threading.Tasks;

namespace client.service.ftp.server.plugin
{
    public class FilePlugin : IFtpCommandServerPlugin
    {
        private readonly FtpServer ftpServer;
        public FilePlugin(FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
        }
        public FtpCommand Cmd => FtpCommand.FILE;

        public async Task<FtpResultInfo> Execute(FtpPluginParamWrap data)
        {
            FtpFileCommand cmd = new FtpFileCommand();
            cmd.DeBytes(data.Connection.ReceiveRequestWrap.Memory);
            await ftpServer.OnFile(cmd, data).ConfigureAwait(false);
            return null;
        }
    }

    public class FileEndPlugin : IFtpCommandServerPlugin
    {
        private readonly FtpServer ftpServer;
        public FileEndPlugin(FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
        }
        public FtpCommand Cmd => FtpCommand.FILE_END;

        public async Task<FtpResultInfo> Execute(FtpPluginParamWrap arg)
        {
            await Task.Yield();

            FtpFileEndCommand cmd = new FtpFileEndCommand();
            cmd.DeBytes(arg.Connection.ReceiveRequestWrap.Memory);

            ftpServer.OnFileEnd(cmd, arg);
            return null;
        }
    }

    public class FileErrorPlugin : IFtpCommandServerPlugin
    {
        private readonly FtpServer ftpServer;
        public FileErrorPlugin(FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
        }
        public FtpCommand Cmd => FtpCommand.FILE_ERROR;

        public async Task<FtpResultInfo> Execute(FtpPluginParamWrap arg)
        {
            await Task.Yield();

            FtpFileErrorCommand cmd = new FtpFileErrorCommand();
            cmd.DeBytes(arg.Connection.ReceiveRequestWrap.Memory);
            ftpServer.OnFileError(cmd, arg);
            return null;
        }
    }
}

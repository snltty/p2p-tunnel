using client.service.ftp.commands;
using System.Threading.Tasks;

namespace client.service.ftp.client.plugin
{
    public class CreatePlugin : IFtpCommandClientPlugin
    {
        public FtpCommand Cmd => FtpCommand.CREATE;

        private readonly FtpClient ftpClient;
        public CreatePlugin(FtpClient ftpClient)
        {
            this.ftpClient = ftpClient;
        }

        public async Task<FtpResultInfo> Execute(FtpPluginParamWrap arg)
        {
            await Task.Yield();

            FtpCreateCommand cmd = new FtpCreateCommand();
            cmd.DeBytes(arg.Connection.ReceiveRequestWrap.Memory);

            if (string.IsNullOrWhiteSpace(cmd.Path))
            {
                return new FtpResultInfo { Code = FtpResultInfo.FtpResultCodes.PATH_REQUIRED };
            }
            else
            {
                ftpClient.Create(cmd.Path);
            }

            return new FtpResultInfo();
        }
    }
}

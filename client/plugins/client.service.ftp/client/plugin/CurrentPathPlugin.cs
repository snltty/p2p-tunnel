using client.service.ftp.commands;
using common.libs.extends;
using System.Threading.Tasks;

namespace client.service.ftp.client.plugin
{
    public class CurrentPathPlugin : IFtpCommandClientPlugin
    {
        private readonly FtpClient ftpClient;

        public CurrentPathPlugin(FtpClient ftpClient)
        {
            this.ftpClient = ftpClient;
        }
        public FtpCommand Cmd => FtpCommand.CURRENT_PATH;

        public async Task<FtpResultInfo> Execute(FtpPluginParamWrap arg)
        {
            return await Task.FromResult(new FtpResultInfo
            {
                Data = ftpClient.GetCurrentPath().ToBytes()
            }).ConfigureAwait(false);
        }
    }
}
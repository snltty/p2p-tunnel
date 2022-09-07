using client.service.ftp.commands;
using common.libs.extends;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.ftp.server.plugin
{
    public class DelPlugin : IFtpCommandServerPlugin
    {
        private readonly FtpServer ftpServer;
        public DelPlugin(FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
        }
        public FtpCommand Cmd => FtpCommand.DELETE;

        public async Task<FtpResultInfo> Execute(FtpPluginParamWrap arg)
        {
            await Task.Yield();

            FtpDelCommand cmd = new FtpDelCommand();
            cmd.DeBytes(arg.Connection.ReceiveRequestWrap.Memory);

            if (string.IsNullOrWhiteSpace(cmd.Path))
            {
                return new FtpResultInfo { Code = FtpResultInfo.FtpResultCodes.PATH_REQUIRED };
            }
            else
            {
                List<string> errs = ftpServer.Delete(cmd, arg);
                if (errs.Any())
                {
                    return new FtpResultInfo { Code = FtpResultInfo.FtpResultCodes.UNKNOW, Data = string.Join(",", errs).ToBytes() };
                }
            }
            return new FtpResultInfo();
        }
    }
}

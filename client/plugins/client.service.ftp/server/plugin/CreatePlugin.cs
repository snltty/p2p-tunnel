using client.service.ftp.commands;
using common.libs.extends;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.ftp.server.plugin
{
    public class CreatePlugin : IFtpCommandServerPlugin
    {
        public FtpCommand Cmd => FtpCommand.CREATE;

        private readonly FtpServer ftpServer;
        public CreatePlugin(FtpServer ftpServer)
        {
            this.ftpServer = ftpServer;
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
                List<string> errs = ftpServer.Create(cmd, arg);
                if (errs.Any())
                {
                    return new FtpResultInfo { Code = FtpResultInfo.FtpResultCodes.UNKNOW, Data = string.Join(",", errs).ToBytes() };
                }
            }
            return new FtpResultInfo();
        }
    }
}

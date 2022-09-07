using client.service.ftp.commands;
using System.Threading.Tasks;

namespace client.service.ftp
{
    public interface IFtpCommandPluginBase
    {
        public FtpCommand Cmd { get; }
        public Task<FtpResultInfo> Execute(FtpPluginParamWrap data);
    }

    public interface IFtpCommandServerPlugin : IFtpCommandPluginBase { }

    public interface IFtpCommandClientPlugin : IFtpCommandPluginBase { }
}

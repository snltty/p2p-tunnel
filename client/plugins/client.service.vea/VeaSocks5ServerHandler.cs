using common.libs;
using common.socks5;

namespace client.service.vea
{
    public interface IVeaSocks5ServerHandler : ISocks5ServerHandler
    {
        public void UpdateConfig();
    }
    public class VeaSocks5ServerHandler : Socks5ServerHandler, IVeaSocks5ServerHandler
    {
        private readonly Config config;
        public VeaSocks5ServerHandler(IVeaSocks5MessengerSender socks5MessengerSender, common.socks5.Config socks5Config, Config config, WheelTimer<object> wheelTimer, IVeaKeyValidator veaKeyValidator)
            : base(socks5MessengerSender, socks5Config, wheelTimer, veaKeyValidator)
        {
            this.config = config;
            UpdateConfig();
        }

        public void UpdateConfig()
        {
            Config.BufferSize = config.BufferSize;
            Config.ConnectEnable = config.ConnectEnable;
            Config.LanConnectEnable = config.LanConnectEnable;
        }
    }

}

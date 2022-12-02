using common.libs;
using common.socks5;

namespace client.service.vea
{
    public interface IVeaSocks5ServerHandler : ISocks5ServerHandler
    {
        public void UpdateConfig();
    }
    public sealed class VeaSocks5ServerHandler : Socks5ServerHandler, IVeaSocks5ServerHandler
    {
        private readonly Config _config;
        public VeaSocks5ServerHandler(IVeaSocks5MessengerSender socks5MessengerSender, Config config, WheelTimer<object> wheelTimer, IVeaKeyValidator veaKeyValidator)
            : base(socks5MessengerSender, new common.socks5.Config {
                ConnectEnable = config.ConnectEnable,
                NumConnections = config.NumConnections,
                BufferSize = config.BufferSize,
                TargetName = config.TargetName,
            }, wheelTimer, veaKeyValidator)
        {
            this._config = config;
            UpdateConfig();
        }

        public void UpdateConfig()
        {
            config.BufferSize = _config.BufferSize;
            config.ConnectEnable = _config.ConnectEnable;
            config.NumConnections = _config.NumConnections;
        }
    }

}

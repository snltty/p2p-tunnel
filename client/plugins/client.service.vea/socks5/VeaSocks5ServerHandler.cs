using common.libs;
using common.socks5;

namespace client.service.vea.socks5
{
    /// <summary>
    /// 组网socks5服务端
    /// </summary>
    public interface IVeaSocks5ServerHandler : ISocks5ServerHandler
    {
        public void UpdateConfig();
    }
    /// <summary>
    /// 组网socks5服务端
    /// </summary>
    public sealed class VeaSocks5ServerHandler : Socks5ServerHandler, IVeaSocks5ServerHandler
    {
        private readonly Config _config;
        /// <summary>
        /// 组网socks5服务端
        /// </summary>
        /// <param name="socks5MessengerSender"></param>
        /// <param name="config"></param>
        /// <param name="wheelTimer"></param>
        /// <param name="veaKeyValidator"></param>
        public VeaSocks5ServerHandler(IVeaSocks5MessengerSender socks5MessengerSender, Config config, WheelTimer<object> wheelTimer, IVeaKeyValidator veaKeyValidator,ISocks5AuthValidator socks5AuthValidator)
            : base(socks5MessengerSender, new common.socks5.Config
            {
                ConnectEnable = config.ConnectEnable,
                NumConnections = config.NumConnections,
                BufferSize = config.BufferSize
            }, wheelTimer, veaKeyValidator, socks5AuthValidator)
        {
            _config = config;
            UpdateConfig();
        }

        /// <summary>
        /// 更新配置
        /// </summary>
        public void UpdateConfig()
        {
            config.BufferSize = _config.BufferSize;
            config.ConnectEnable = _config.ConnectEnable;
            config.NumConnections = _config.NumConnections;
            config.ListenPort = _config.SocksPort;
        }
    }

}

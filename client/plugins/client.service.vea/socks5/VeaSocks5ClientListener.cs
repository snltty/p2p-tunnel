using common.socks5;

namespace client.service.vea.socks5
{
    /// <summary>
    /// 组网socks5监听
    /// </summary>
    public interface IVeaSocks5ClientListener : ISocks5ClientListener
    {
    }
    /// <summary>
    /// 组网socks5监听
    /// </summary>
    public sealed class VeaSocks5ClientListener : Socks5ClientListener, IVeaSocks5ClientListener
    {
        public VeaSocks5ClientListener(Config config)
        {
            base.SetBufferSize(config.BufferSize);
        }

        public new void SetBufferSize(int bufferSize)
        {
            base.SetBufferSize(bufferSize);
        }
    }
}

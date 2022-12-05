using common.server;
using common.socks5;

namespace client.service.vea
{
    /// <summary>
    /// 组网socks5消息发送
    /// </summary>
    public interface IVeaSocks5MessengerSender : ISocks5MessengerSender
    {

    }
    /// <summary>
    /// 组网socks5消息发送
    /// </summary>
    public sealed class VeaSocks5MessengerSender : Socks5MessengerSender, IVeaSocks5MessengerSender
    {
        /// <summary>
        /// 
        /// </summary>
        protected override ushort TargetRequest { get; } = (ushort)VeaSocks5MessengerIds.Request;
        /// <summary>
        /// 
        /// </summary>
        protected override ushort TargetResponse { get; } = (ushort)VeaSocks5MessengerIds.Response;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="messengerSender"></param>
        public VeaSocks5MessengerSender(MessengerSender messengerSender) : base(messengerSender)
        {
        }
    }
}

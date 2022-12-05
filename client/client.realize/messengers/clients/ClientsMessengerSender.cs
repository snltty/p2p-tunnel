using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System.Threading.Tasks;

namespace client.realize.messengers.clients
{
    /// <summary>
    /// 客户端消息发送
    /// </summary>
    public sealed class ClientsMessengerSender
    {
        private readonly MessengerSender messengerSender;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="messengerSender"></param>
        public ClientsMessengerSender(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }

        /// <summary>
        /// 收到服务器的客户端列表信息
        /// </summary>
        public SimpleSubPushHandler<ClientsInfo> OnServerClientsData { get; } = new SimpleSubPushHandler<ClientsInfo>();

        /// <summary>
        /// 获取通道端口
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public async Task<ushort> GetTunnelPort(IConnection connection)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                Payload = Helper.EmptyArray,
                MessengerId = (ushort)ClientsMessengerIds.Port,
                Timeout = 2000
            }).ConfigureAwait(false);

            if (resp.Code == MessageResponeCodes.OK)
            {
                return resp.Data.Span.ToUInt16();
            }
            return 0;
        }
        /// <summary>
        /// 添加通道
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="tunnelName"></param>
        /// <param name="port"></param>
        /// <param name="localPort"></param>
        /// <returns></returns>
        public async Task<ulong> AddTunnel(IConnection connection, ulong tunnelName, ushort port, ushort localPort)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Connection = connection,
                Payload = new TunnelRegisterInfo { LocalPort = localPort, Port = port, TunnelName = tunnelName }.ToBytes(),
                MessengerId = (ushort)ClientsMessengerIds.AddTunnel,
                Timeout = 2000
            }).ConfigureAwait(false);

            if (resp.Code == MessageResponeCodes.OK)
            {
                return resp.Data.Span.ToUInt64();
            }
            return 0;
        }
        /// <summary>
        /// 删除通道
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="tunnelName"></param>
        /// <returns></returns>
        public async Task RemoveTunnel(IConnection connection, ulong tunnelName)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Connection = connection,
                Payload = tunnelName.ToBytes(),
                MessengerId = (ushort)ClientsMessengerIds.RemoveTunnel,
                Timeout = 2000
            }).ConfigureAwait(false);
        }
    }
}

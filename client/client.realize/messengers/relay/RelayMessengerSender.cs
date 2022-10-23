using client.messengers.register;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System;
using System.Threading.Tasks;

namespace client.realize.messengers.relay
{
    public class RelayMessengerSender
    {

        public SimpleSubPushHandler<OnRelayInfo> OnRelay { get; } = new SimpleSubPushHandler<OnRelayInfo>();
        private readonly MessengerSender messengerSender;
        private readonly RegisterStateInfo registerStateInfo;

        public RelayMessengerSender(MessengerSender messengerSender, RegisterStateInfo registerStateInfo)
        {
            this.messengerSender = messengerSender;
            this.registerStateInfo = registerStateInfo;
        }

        /// <summary>
        /// 通知其要使用中继
        /// </summary>
        /// <param name="toid"></param>
        /// <param name="connection">中继节点</param>
        /// <returns></returns>
        public async Task SendRelay(ulong toid, IConnection connection)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                Path = "relay/relay",
                Connection = connection,
                Payload = new RelayInfo { FromId = registerStateInfo.ConnectId, ToId = toid }.ToBytes()
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 验证对方是否可中继
        /// </summary>
        /// <param name="toid"></param>
        /// <param name="connection">中继节点</param>
        /// <returns></returns>
        public async Task<bool> Verify(ulong toid, IConnection connection)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                Path = "relay/verify",
                Connection = connection,
                Payload = toid.ToBytes()
            }).ConfigureAwait(false);

            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }
    }

    public class OnRelayInfo
    {
        public IConnection Connection { get; set; }
        public ulong FromId { get; set; }
    }
}

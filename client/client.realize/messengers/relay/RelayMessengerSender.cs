using client.messengers.register;
using client.messengers.relay;
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
        public async Task Relay(ulong toid, IConnection connection)
        {
            await messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = (ushort)RelayMessengerIds.Notify,
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
                MessengerId = (ushort)RelayMessengerIds.Verify,
                Connection = connection,
                Payload = toid.ToBytes()
            }).ConfigureAwait(false);

            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }

        /// <summary>
        /// 中继延迟
        /// </summary>
        /// <param name="toid"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        public async Task<bool> Delay(ulong toid, IConnection connection)
        {
            var resp = await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)RelayMessengerIds.Delay,
                Connection = connection,
                Timeout = 2000,
                Payload = Helper.EmptyArray,
                RelayId = toid
            }).ConfigureAwait(false);
            return resp.Code == MessageResponeCodes.OK && resp.Data.Span.SequenceEqual(Helper.TrueArray);
        }


        public async Task<bool> AskConnects()
        {
            return await messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = (ushort)RelayMessengerIds.AskConnects,
                Connection = registerStateInfo.OnlineConnection,
                Payload = Helper.EmptyArray,
            }).ConfigureAwait(false);
        }
        public async Task<bool> Connects(ConnectsInfo routes)
        {
            return await messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = (ushort)RelayMessengerIds.Connects,
                Connection = registerStateInfo.OnlineConnection,
                Payload = routes.ToBytes(),
            }).ConfigureAwait(false);
        }
        public async Task<bool> Routes(RoutesInfo routes)
        {
            return await messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = (ushort)RelayMessengerIds.Routes,
                Connection = registerStateInfo.OnlineConnection,
                Payload = routes.ToBytes(),
            }).ConfigureAwait(false);
        }
    }

    public class OnRelayInfo
    {
        public IConnection Connection { get; set; }
        public ulong FromId { get; set; }
    }
}

using common.libs;
using common.server;
using common.server.model;
using server.messengers.register;
using System.Threading.Tasks;

namespace server.service.messengers
{
    /// <summary>
    /// 
    /// </summary>
    [MessengerIdRange((ushort)PunchHoleMessengerIds.Min, (ushort)PunchHoleMessengerIds.Max)]
    public sealed class PunchHoleMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        private readonly MessengerSender messengerSender;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientRegisterCache"></param>
        /// <param name="messengerSender"></param>
        public PunchHoleMessenger(IClientRegisterCaching clientRegisterCache, MessengerSender messengerSender)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.messengerSender = messengerSender;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)PunchHoleMessengerIds.Response)]
        public async Task Response(IConnection connection)
        {
            PunchHoleResponseInfo model = new PunchHoleResponseInfo();
            model.DeBytes(connection.ReceiveRequestWrap.Payload);

            if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
            {
                //B已注册
                if (clientRegisterCache.Get(model.ToId, out RegisterCacheInfo target))
                {
                    //是否在同一个组
                    if (source.GroupId == target.GroupId)
                    {

                        model.FromId = connection.ConnectId;
                        IConnection online = connection.ServerType == ServerType.UDP ? target.UdpConnection : target.TcpConnection;
                        if (online == null || online.Connected == false)
                        {
                            online = target.OnLineConnection;
                        }

                        await messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = online,
                            Payload = model.ToBytes(),
                            MessengerId = connection.ReceiveRequestWrap.MessengerId,
                            RequestId = connection.ReceiveRequestWrap.RequestId,
                        }).ConfigureAwait(false);
                    }
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [MessengerId((ushort)PunchHoleMessengerIds.Request)]
        public async Task Request(IConnection connection)
        {
            PunchHoleRequestInfo model = new PunchHoleRequestInfo();
            model.DeBytes(connection.ReceiveRequestWrap.Payload);
            // if (model.Data.Length > 50) return;

            //A已注册
            if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
            {
                //B已注册
                if (clientRegisterCache.Get(model.ToId, out RegisterCacheInfo target))
                {
                    //是否在同一个组
                    if (source.GroupId == target.GroupId)
                    {
                        if (model.PunchForwardType == PunchForwardTypes.NOTIFY)
                        {
                            if (source.GetTunnel(model.TunnelName, out TunnelRegisterCacheInfo tunnel) == false)
                            {
                                return;
                            }
                            model.Data = new PunchHoleNotifyInfo
                            {
                                Ip = source.OnLineConnection.Address.Address,
                                LocalIps = source.LocalIps,
                                LocalPort = tunnel.LocalPort,
                                Port = tunnel.Port,
                                IsDefault = tunnel.IsDefault,
                                Index = model.Index,
                            }.ToBytes();
                        }

                        model.FromId = connection.ConnectId;
                        IConnection online = connection.ServerType == ServerType.UDP ? target.UdpConnection : target.TcpConnection;
                        if (online == null || online.Connected == false)
                        {
                            online = target.OnLineConnection;
                        }


                        bool res = await messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = online,
                            Payload = model.ToBytes(),
                            MessengerId = connection.ReceiveRequestWrap.MessengerId,
                            RequestId = connection.ReceiveRequestWrap.RequestId,
                        }).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}

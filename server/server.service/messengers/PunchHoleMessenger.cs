using common.libs;
using common.server;
using common.server.model;
using server.messengers.register;
using System.Threading.Tasks;

namespace server.service.messengers
{
    [MessengerIdRange((ushort)PunchHoleMessengerIds.Min, (ushort)PunchHoleMessengerIds.Max)]
    public sealed class PunchHoleMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        private readonly MessengerSender messengerSender;

        public PunchHoleMessenger(IClientRegisterCaching clientRegisterCache, MessengerSender messengerSender)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.messengerSender = messengerSender;
        }

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
                        await messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = target.Connection,
                            Payload = model.ToBytes(),
                            MessengerId = connection.ReceiveRequestWrap.MessengerId,
                            RequestId = connection.ReceiveRequestWrap.RequestId,
                        }).ConfigureAwait(false);
                    }
                }
            }
        }

        [MessengerId((ushort)PunchHoleMessengerIds.Request)]
        public async Task Request(IConnection connection)
        {
            PunchHoleRequestInfo model = new PunchHoleRequestInfo();
            model.DeBytes(connection.ReceiveRequestWrap.Payload);
            model.FromId = connection.ConnectId;

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
                            TunnelRegisterCacheInfo tunnel;
                            if (model.NewTunnel == 0)
                            {
                                tunnel = new TunnelRegisterCacheInfo { LocalPort = source.LocalPort, Port = source.Port };
                            }
                            else
                            {
                                if (source.GetTunnel(model.ToId, out tunnel) == false)
                                {
                                    return;
                                }
                            }
                            model.Data = new PunchHoleNotifyInfo
                            {
                                Ip = source.Connection.Address.Address,
                                LocalIps = source.LocalIps,
                                LocalPort = tunnel.LocalPort,
                                Port = tunnel.Port,
                            }.ToBytes();
                        }

                        await messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = target.Connection,
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

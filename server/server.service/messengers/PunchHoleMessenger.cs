using common.libs.extends;
using common.server;
using common.server.model;
using server.messengers.singnin;
using System;
using System.Threading.Tasks;

namespace server.service.messengers
{
    [MessengerIdRange((ushort)PunchHoleMessengerIds.Min, (ushort)PunchHoleMessengerIds.Max)]
    public sealed class PunchHoleMessenger : IMessenger
    {
        private readonly IClientSignInCaching clientSignInCache;
        private readonly MessengerSender messengerSender;

        public PunchHoleMessenger(IClientSignInCaching clientSignInCache, MessengerSender messengerSender)
        {
            this.clientSignInCache = clientSignInCache;
            this.messengerSender = messengerSender;
        }

        [MessengerId((ushort)PunchHoleMessengerIds.Response)]
        public async Task Response(IConnection connection)
        {
            PunchHoleResponseInfo model = new PunchHoleResponseInfo();
            model.DeBytes(connection.ReceiveRequestWrap.Payload);

            if (clientSignInCache.Get(connection.ConnectId, out SignInCacheInfo source))
            {
                //B已注册
                if (clientSignInCache.Get(model.ToId, out SignInCacheInfo target))
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
            if (clientSignInCache.Get(connection.ConnectId, out SignInCacheInfo source))
            {
                //B已注册
                if (clientSignInCache.Get(model.ToId, out SignInCacheInfo target))
                {
                    //是否在同一个组
                    if (source.GroupId == target.GroupId)
                    {
                        if (model.PunchForwardType == PunchForwardTypes.NOTIFY)
                        {
                            TunnelCacheInfo tunnel;
                            if (model.NewTunnel == 0)
                            {
                                tunnel = new TunnelCacheInfo { LocalPort = source.LocalPort, Port = connection.Address.Port };
                            }
                            else
                            {
                                if (target.GetTunnel(connection.ConnectId, out tunnel) == false)
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

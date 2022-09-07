using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using server.messengers.register;
using System;
using System.Threading.Tasks;

namespace server.service.messengers
{
    /// <summary>
    /// 中继
    /// </summary>
    public class RelayMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        private readonly MessengerSender messengerSender;
        private readonly Config config;
        public RelayMessenger(IClientRegisterCaching clientRegisterCache, MessengerSender messengerSender, Config config)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.messengerSender = messengerSender;
            this.config = config;
        }

        public async Task SendOnly(IConnection connection)
        {
            if (!config.Relay)
            {
                return;
            }

            RelayParamsInfo model = new RelayParamsInfo();
            model.DeBytes(connection.ReceiveRequestWrap.Memory);

            //A已注册
            if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
            {
                //B已注册
                if (clientRegisterCache.Get(model.ToId, out RegisterCacheInfo target))
                {
                    //是否在同一个组
                    if (source.GroupId == target.GroupId)
                    {
                        await messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = connection.ServerType == ServerType.UDP ? target.UdpConnection : target.TcpConnection,
                            Memory = model.Data,
                            MemoryPath = model.Path,
                            RequestId = connection.ReceiveRequestWrap.RequestId,
                            Relay = 1,
                            RelayId = source.Id,
                        }).ConfigureAwait(false);
                    }
                }
            }
        }

        public async Task<byte[]> SendReply(IConnection connection)
        {
            if (!config.Relay)
            {
                return Helper.EmptyArray;
            }

            RelayParamsInfo model = new RelayParamsInfo();
            model.DeBytes(connection.ReceiveRequestWrap.Memory);

            //A已注册
            if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
            {
                //B已注册
                if (clientRegisterCache.Get(model.ToId, out RegisterCacheInfo target))
                {
                    //是否在同一个组
                    if (source.GroupId == target.GroupId)
                    {
                        return (await messengerSender.SendReply(new MessageRequestWrap
                        {
                            Connection = connection.ServerType == ServerType.UDP ? target.UdpConnection : target.TcpConnection,
                            Memory = model.Data,
                            MemoryPath = model.Path,
                            RequestId = connection.ReceiveRequestWrap.RequestId
                        }).ConfigureAwait(false)).Data.ToArray();
                    }
                }
            }
            return Helper.EmptyArray;
        }
    }
}

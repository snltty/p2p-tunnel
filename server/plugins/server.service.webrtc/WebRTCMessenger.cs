using common.libs.extends;
using common.server;
using common.server.model;
using server.messengers.register;
using server.service.webrtc.models;
using System.Threading.Tasks;

namespace server.service.manager
{
    public class WebRTCMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCaching;
        private readonly MessengerSender messengerSender;


        public WebRTCMessenger(IClientRegisterCaching clientRegisterCaching, MessengerSender messengerSender)
        {
            this.clientRegisterCaching = clientRegisterCaching;
            this.messengerSender = messengerSender;
        }

        public async Task<bool> Execute(IConnection connection)
        {
            WebRTCConnectionInfo model = new WebRTCConnectionInfo();
            model.DeBytes(connection.ReceiveRequestWrap.Memory);

            //A已注册
            if (clientRegisterCaching.Get(connection.ConnectId, out RegisterCacheInfo source))
            {
                //B已注册
                if (clientRegisterCaching.Get(model.ToId, out RegisterCacheInfo target))
                {
                    //是否在同一个组
                    if (source.GroupId == target.GroupId)
                    {
                        model.FromId = connection.ConnectId;
                        return await messengerSender.SendOnly(new MessageRequestWrap
                        {
                            Connection = connection.ServerType == ServerType.UDP ? target.UdpConnection : target.TcpConnection,
                            Content = connection.ReceiveRequestWrap.Memory,
                            MemoryPath = connection.ReceiveRequestWrap.MemoryPath,
                            RequestId = connection.ReceiveRequestWrap.RequestId
                        }).ConfigureAwait(false);
                    }
                }
            }

            return false;
        }
    }


}

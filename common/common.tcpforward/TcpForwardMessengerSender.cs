using common.libs;
using common.server;
using common.server.model;
using System.Threading.Tasks;

namespace common.tcpforward
{
    public class TcpForwardMessengerSender
    {
        private readonly MessengerSender messengerSender;
        public TcpForwardMessengerSender(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }

        public SimpleSubPushHandler<TcpForwardInfo> OnRequestHandler { get; } = new SimpleSubPushHandler<TcpForwardInfo>();
        public void SendRequest(TcpForwardInfo arg)
        {
            var bytes = arg.ToBytes();
            _ = messengerSender.SendOnly(new MessageRequestWrap
            {
                Path = "TcpForward/Request",
                Connection = arg.Connection,
                Memory = bytes
            });
        }
        public void OnRequest(TcpForwardInfo data)
        {
            OnRequestHandler.Push(data);
        }

        public SimpleSubPushHandler<TcpForwardInfo> OnResponseHandler { get; } = new SimpleSubPushHandler<TcpForwardInfo>();
        public void SendResponse(TcpForwardInfo arg)
        {
            var bytes = arg.ToBytes();
            _ = messengerSender.SendOnly(new MessageRequestWrap
            {
                Path = "TcpForward/Response",
                Connection = arg.Connection.FromConnection,
                Memory = bytes
            });
        }
        public void OnResponse(TcpForwardInfo data)
        {
            OnResponseHandler.Push(data);
        }

        public async Task<MessageResponeInfo> GetPorts(IConnection Connection)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                Path = "TcpForward/GetPorts",
                Connection = Connection,
                Memory = Helper.EmptyArray
            }).ConfigureAwait(false);
        }

        public async Task<MessageResponeInfo> UnRegister(IConnection Connection, TcpForwardUnRegisterParamsInfo data)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                Path = "TcpForward/UnRegister",
                Connection = Connection,
                Memory = data.ToBytes()
            }).ConfigureAwait(false);
        }
        public async Task<MessageResponeInfo> Register(IConnection Connection, TcpForwardRegisterParamsInfo data)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                Path = "TcpForward/Register",
                Connection = Connection,
                Memory = data.ToBytes(),
            }).ConfigureAwait(false);
        }
    }
}

using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System;
using System.Threading.Tasks;

namespace common.udpforward
{
    /// <summary>
    /// udp转发消息发送
    /// </summary>
    public sealed class UdpForwardMessengerSender
    {
        private readonly MessengerSender messengerSender;
        public UdpForwardMessengerSender(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }

        public Func<UdpForwardInfo, Task> OnRequestHandle { get; set; } = async (a) => await Task.CompletedTask;
        public async Task SendRequest(UdpForwardInfo arg)
        {
            byte[] bytes = arg.ToBytes(out int length);
            var res = await messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = (ushort)UdpForwardMessengerIds.Request,
                Connection = arg.Connection,
                Payload = bytes.AsMemory(0, length)
            }).ConfigureAwait(false);

            arg.Return(bytes);
        }
        public async Task OnRequest(UdpForwardInfo data)
        {
            await OnRequestHandle(data);
        }

        public Func<UdpForwardInfo, Task> OnResponseHandle { get; set; } = async (a) => await Task.CompletedTask;
        public async Task SendResponse(UdpForwardInfo arg, IConnection Connection)
        {
            byte[] bytes = arg.ToBytes(out int length);
            var res = messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = (ushort)UdpForwardMessengerIds.Response,
                Connection = Connection,
                Payload = bytes.AsMemory(0, length)
            }).ConfigureAwait(false);
            await res;
            arg.Return(bytes);
        }
        public async Task OnResponse(UdpForwardInfo data)
        {
            await OnResponseHandle(data);
        }

        public async Task<MessageResponeInfo> GetPorts(IConnection Connection)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)UdpForwardMessengerIds.Ports,
                Connection = Connection,
            }).ConfigureAwait(false);
        }

        public async Task<MessageResponeInfo> SignOut(IConnection Connection, ushort port)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)UdpForwardMessengerIds.SignOut,
                Connection = Connection,
                Payload = port.ToBytes()
            }).ConfigureAwait(false);
        }
        public async Task<MessageResponeInfo> SignIn(IConnection Connection, UdpForwardRegisterParamsInfo param)
        {
            return await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)UdpForwardMessengerIds.SignIn,
                Connection = Connection,
                Payload = param.ToBytes(),
            }).ConfigureAwait(false);
        }

    }
}

using common.libs;
using common.server.model;
using common.server;
using System;
using System.Threading.Tasks;

namespace common.proxy
{
    public interface IProxyMessengerSender
    {
        public Task<bool> Request(ProxyInfo data);
        public Task<bool> Response(ProxyInfo data);

        public Task<bool> ResponseClose(ProxyInfo data);
        public Task<bool> RequestClose(ProxyInfo data);
    }


    /// <summary>
    /// socks5消息发送
    /// </summary>
    public sealed class Socks5MessengerSender : IProxyMessengerSender
    {
        private readonly MessengerSender messengerSender;

        private IConnection connection;
        private string targetName;
        public Socks5MessengerSender(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }

        public async Task<bool> Request(ProxyInfo info)
        {
            if (connection == null || connection.Connected == false) return false;

            byte[] bytes = info.ToBytes(out int length);
            bool res = await messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = (ushort)Socks5MessengerIds.Request,
                Connection = connection,
                Payload = bytes.AsMemory(0, length)
            });
            info.Return(bytes);
            return res;
        }
        public async Task<bool> Response(ProxyInfo info)
        {
            byte[] bytes = info.ToBytes(out int length);
            bool res = await messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = (ushort)Socks5MessengerIds.Response,
                Connection = info.Connection,
                Payload = bytes.AsMemory(0, length)
            });
            info.Return(bytes);
            return res;
        }

        public async Task<bool> ResponseClose(ProxyInfo data)
        {
            data.Data = Helper.EmptyArray;
            return await Response(data);
        }

        public async Task<bool> RequestClose(ProxyInfo data)
        {
            data.Data = Helper.EmptyArray;
            return await Request(data);
        }
    }
}

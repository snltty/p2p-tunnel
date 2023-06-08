using common.libs;
using common.server.model;
using common.server;
using System;
using System.Threading.Tasks;
using System.Text;

namespace common.proxy
{
    public interface IProxyMessengerSender
    {
        public Task<bool> Request(ProxyInfo data, bool unconnectedMessage = false);
        public Task<ProxyConnectTestResult> Test(ProxyInfo data);
        public Task<bool> Response(ProxyInfo data, bool unconnectedMessage = false);

        public Task<bool> ResponseClose(ProxyInfo data);
        public Task<bool> RequestClose(ProxyInfo data);
    }


    /// <summary>
    /// socks5消息发送
    /// </summary>
    public sealed class ProxyMessengerSender : IProxyMessengerSender
    {
        private readonly MessengerSender messengerSender;

        public ProxyMessengerSender(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }

        public async Task<bool> Request(ProxyInfo info, bool unconnectedMessage = false)
        {
            if (info.Connection == null || info.Connection.Connected == false || info.Connection.SendDenied > 0) return false;

            byte[] bytes = info.ToBytes(out int length);

            bool res = await messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = (ushort)ProxyMessengerIds.Request,
                Connection = info.Connection,
                Payload = bytes.AsMemory(0, length)
            }, unconnectedMessage: unconnectedMessage);
            if (info.Connection != null) info.Connection.SentBytes += (ulong)info.Data.Length;
            info.Return(bytes);
            return res;
        }
        public async Task<ProxyConnectTestResult> Test(ProxyInfo info)
        {
            if (info.Connection == null || info.Connection.Connected == false)
            {
                return ProxyConnectTestResult.Connection;
            }
            if (info.Connection.SendDenied > 0)
            {
                return ProxyConnectTestResult.Denied;
            }

            byte[] bytes = info.ToBytes(out int length);
            var res = await messengerSender.SendReply(new MessageRequestWrap
            {
                MessengerId = (ushort)ProxyMessengerIds.Test,
                Connection = info.Connection,
                Payload = bytes.AsMemory(0, length)
            });
            info.Return(bytes);
            
            if(res.Code != MessageResponeCodes.OK)
            {
                return ProxyConnectTestResult.Connection;
            }
            return (ProxyConnectTestResult)res.Data.Span[0];
        }

        public async Task<bool> Response(ProxyInfo info, bool unconnectedMessage = false)
        {
            if (info.Connection == null || info.Connection.Connected == false || info.Connection.SendDenied > 0) return false;

            byte[] bytes = info.ToBytes(out int length);
            bool res = await messengerSender.SendOnly(new MessageRequestWrap
            {
                MessengerId = (ushort)ProxyMessengerIds.Response,
                Connection = info.Connection,
                Payload = bytes.AsMemory(0, length)
            }, unconnectedMessage: unconnectedMessage);
            if (info.Connection != null) info.Connection.SentBytes += (ulong)info.Data.Length;
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

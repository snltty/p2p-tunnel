using client.messengers.clients;
using client.messengers.signin;
using common.forward;
using common.libs;
using common.libs.extends;
using common.proxy;
using common.server;
using System;

namespace client.service.forward
{
    /// <summary>
    /// 转发目标提供
    /// </summary>
    internal class ForwardTargetProvider : IForwardTargetProvider
    {
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly IForwardTargetCaching<ForwardTargetCacheInfo> forwardTargetCaching;
        private readonly SignInStateInfo signInStateInfo;

        public ForwardTargetProvider(IClientInfoCaching clientInfoCaching, IForwardTargetCaching<ForwardTargetCacheInfo> forwardTargetCaching, SignInStateInfo signInStateInfo)
        {
            this.clientInfoCaching = clientInfoCaching;
            this.forwardTargetCaching = forwardTargetCaching;
            this.signInStateInfo = signInStateInfo;
            signInStateInfo.OnChange += (state) =>
            {
                forwardTargetCaching.ClearConnection();
            };
            clientInfoCaching.OnOffline += (client) =>
            {
                forwardTargetCaching.ClearConnection(client.ConnectionId);
            };
        }

        public bool Contains(ushort port)
        {
            return forwardTargetCaching.Contains(port);
        }

        /// <summary>
        /// 根据host获取目标连接
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="info"></param>
        public void Get(string domain, ProxyInfo info)
        {
            GetTarget(forwardTargetCaching.Get(domain), info);
            if (Logger.Instance.LoggerLevel <= LoggerTypes.WARNING)
            {
                if (info.Connection == null || info.Connection.Connected == false)
                {
                    Logger.Instance.Warning($"{info.ProxyPlugin.Name}->domain:{domain}->target not exists or not connect");
                }
            }
        }
        /// <summary>
        /// 根据端口获取目标连接
        /// </summary>
        /// <param name="port"></param>
        /// <param name="info"></param>
        public void Get(ushort port, ProxyInfo info)
        {
            GetTarget(forwardTargetCaching.Get(port), info);
            if (Logger.Instance.LoggerLevel <= LoggerTypes.WARNING)
            {
                if (info.Connection == null || info.Connection.Connected == false)
                {
                    Logger.Instance.Warning($"{info.ProxyPlugin.Name}->port:{port}->target not exists or not connect");
                }
            }
        }

        private void GetTarget(ForwardTargetCacheInfo cacheInfo, ProxyInfo info)
        {
            if (cacheInfo != null)
            {
                if (cacheInfo.Connection == null || cacheInfo.Connection.Connected == false)
                {
                    cacheInfo.Connection = SelectConnection(cacheInfo);
                }
                info.Connection = cacheInfo.Connection;
                info.TargetAddress = cacheInfo.IPAddress;
                info.TargetPort = cacheInfo.Port;
            }
        }

        private IConnection SelectConnection(ForwardTargetCacheInfo cacheInfo)
        {
            if (cacheInfo.ConnectionId == 0)
            {
                return signInStateInfo.Connection;
            }
            if (clientInfoCaching.Get(cacheInfo.ConnectionId, out ClientInfo client))
            {
                return client.Connection;
            }
            return null;

        }
    }

}
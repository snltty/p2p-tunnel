using client.messengers.clients;
using client.messengers.singnin;
using common.forward;
using common.libs;
using common.libs.database;
using common.libs.extends;
using common.proxy;
using common.server;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace client.service.forward
{
    /// <summary>
    /// tcp转发中转器和入口
    /// </summary>
    public sealed class ForwardTransfer
    {
        private readonly P2PConfigInfo p2PConfigInfo;
        /// <summary>
        /// 监听列表
        /// </summary>
        public List<P2PListenInfo> p2pListens = new List<P2PListenInfo>();
        private readonly IConfigDataProvider<P2PConfigInfo> p2pConfigDataProvider;
        private readonly IForwardTargetCaching<ForwardTargetCacheInfo> forwardTargetCaching;
        private readonly IProxyServer proxyServer;
        private readonly IProxyMessengerSender proxyMessengerSender;
        private readonly SignInStateInfo signInStateInfo;
        private readonly IClientInfoCaching clientInfoCaching;

        NumberSpaceUInt32 listenNS = new NumberSpaceUInt32();
        NumberSpaceUInt32 forwardNS = new NumberSpaceUInt32();
        private readonly common.forward.Config config;


        public ForwardTransfer(
            IConfigDataProvider<P2PConfigInfo> p2pConfigDataProvider,
            IForwardTargetCaching<ForwardTargetCacheInfo> forwardTargetCaching,
            common.forward.Config config, IForwardProxyPlugin forwardProxyPlugin, IProxyServer proxyServer,
            IProxyMessengerSender proxyMessengerSender, SignInStateInfo signInStateInfo, IClientInfoCaching clientInfoCaching)
        {
            this.p2pConfigDataProvider = p2pConfigDataProvider;
            this.forwardTargetCaching = forwardTargetCaching;
            this.config = config;
            this.proxyServer = proxyServer;
            this.proxyMessengerSender = proxyMessengerSender;
            this.signInStateInfo = signInStateInfo;
            this.clientInfoCaching = clientInfoCaching;

            p2PConfigInfo = ReadP2PConfig();

            forwardProxyPlugin.OnStarted += (port) => StateChanged(port, true);
            forwardProxyPlugin.OnStoped += (port) => StateChanged(port, false);


        }

        public void Start()
        {
            StartP2PAllWithListening();
        }

        private void StateChanged(ushort port, bool state)
        {
            if (port == 0)
            {
                p2pListens.ForEach(c =>
                {
                    c.Listening = state;
                });
            }
            else
            {
                P2PListenInfo listen = GetP2PByPort(port);
                listen.Listening = state;
            }
        }

        /// <summary>
        /// 添加监听
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public string AddP2PListen(P2PListenAddParams param)
        {
            try
            {

                P2PListenInfo oldPort = GetP2PByPort(param.Port);
                if (oldPort.ID > 0 && oldPort.ID != param.ID)
                {
                    return "已存在";
                }

                P2PListenInfo old = GetP2PByID(param.ID);
                if (old.ID > 0)
                {
                    StopP2PListen(old);
                    old.Port = param.Port;
                    old.AliveType = param.AliveType;
                    old.Desc = param.Desc;
                }
                else
                {
                    param.ID = listenNS.Increment();
                    p2pListens.Add(new P2PListenInfo
                    {
                        Port = param.Port,
                        ID = param.ID,
                        AliveType = param.AliveType,
                        Desc = param.Desc
                    });
                }

                if (param.Listening)
                {
                    StartP2P(param.ID);
                }
                SaveP2PConfig();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return string.Empty;
        }
        /// <summary>
        /// 删除监听
        /// </summary>
        /// <param name="listenid"></param>
        public void RemoveP2PListen(uint listenid)
        {
            RemoveP2PListen(GetP2PByID(listenid));
        }
        private void RemoveP2PListen(P2PListenInfo listen)
        {
            if (listen.ID > 0)
            {
                StopP2PListen(listen);
                p2pListens.Remove(listen);
                foreach (P2PForwardInfo item in listen.Forwards)
                {
                    forwardTargetCaching.Remove(item.SourceIp, item.TargetPort);
                }
                forwardTargetCaching.Remove(listen.Port);
                SaveP2PConfig();
            }
        }

        /// <summary>
        /// 停止监听
        /// </summary>
        /// <param name="listenid"></param>
        public void StopP2PListen(uint listenid)
        {
            StopP2PListen(GetP2PByID(listenid));
        }
        private void StopP2PListen(P2PListenInfo listen)
        {
            if (listen.ID > 0)
            {
                proxyServer.Stop(listen.Port);
            }
        }

        /// <summary>
        /// 获取监听
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public P2PListenInfo GetP2PByID(uint id)
        {
            return p2pListens.FirstOrDefault(c => c.ID == id) ?? new P2PListenInfo { };
        }
        private P2PListenInfo GetP2PByPort(ushort port)
        {
            return p2pListens.FirstOrDefault(c => c.Port == port) ?? new P2PListenInfo { };
        }

        /// <summary>
        /// 添加转发
        /// </summary>
        /// <param name="forward"></param>
        /// <returns></returns>
        public string AddP2PForward(P2PForwardAddParams forward)
        {
            P2PListenInfo listen = GetP2PByID(forward.ListenID);
            if (listen.ID == 0)
            {
                return "未找到监听对象";
            }

            P2PForwardInfo saveInfo = listen.Forwards.FirstOrDefault(c => c.ID == forward.Forward.ID);
            if (saveInfo != null)
            {
                if (listen.AliveType == ForwardAliveTypes.Web)
                {
                    forwardTargetCaching.Remove(saveInfo.SourceIp, listen.Port);
                }
                else
                {
                    forwardTargetCaching.Remove(listen.Port);
                }
                saveInfo.Desc = forward.Forward.Desc;
                saveInfo.SourceIp = forward.Forward.SourceIp;
                saveInfo.TargetIp = forward.Forward.TargetIp;
                saveInfo.TargetPort = forward.Forward.TargetPort;
                saveInfo.ConnectionId = forward.Forward.ConnectionId;
            }
            else
            {
                if (listen.AliveType == ForwardAliveTypes.Web)
                {
                    if (listen.Forwards.FirstOrDefault(c => c.SourceIp == forward.Forward.SourceIp) != null)
                    {
                        return $"已存在源:{forward.Forward.SourceIp}";
                    }
                }
                else
                {
                    if (listen.Forwards.Count > 0)
                    {
                        return $"一个隧道端口仅允许一条转发";
                    }
                }

                forward.Forward.ID = forwardNS.Increment();
                listen.Forwards.Add(forward.Forward);
            }

            if (listen.AliveType == ForwardAliveTypes.Web)
            {
                forwardTargetCaching.Add(forward.Forward.SourceIp, listen.Port, new ForwardTargetCacheInfo
                {
                    IPAddress = forward.Forward.TargetIp.GetAddressBytes(),
                    Port = forward.Forward.TargetPort,
                    ConnectionId = forward.Forward.ConnectionId,
                });
            }
            else
            {
                forwardTargetCaching.Add(listen.Port, new ForwardTargetCacheInfo
                {
                    IPAddress = forward.Forward.TargetIp.GetAddressBytes(),
                    Port = forward.Forward.TargetPort,
                    ConnectionId = forward.Forward.ConnectionId,
                });
            }

            SaveP2PConfig();

            return string.Empty;
        }
        /// <summary>
        /// 删除转发
        /// </summary>
        /// <param name="forward"></param>
        public void RemoveP2PForward(P2PForwardRemoveParams forward)
        {
            P2PListenInfo listen = GetP2PByID(forward.ListenID);
            if (listen.ID == 0)
            {
                return;
            }

            P2PForwardInfo forwardInfo = listen.Forwards.FirstOrDefault(c => c.ID == forward.ForwardID);
            if (forwardInfo == null)
            {
                return;
            }

            listen.Forwards.Remove(forwardInfo);
            forwardTargetCaching.Remove(forwardInfo.SourceIp, listen.Port);
            SaveP2PConfig();
        }
        public async Task<ProxyConnectTestResult> TestP2PForward(P2PForwardRemoveParams model)
        {
            var listen = GetP2PByID(model.ListenID);
            var forward = listen.Forwards.FirstOrDefault(c => c.ID == model.ForwardID);
            IConnection connection = signInStateInfo.Connection;
            if (forward.ConnectionId > 0)
            {
                if (clientInfoCaching.Get(forward.ConnectionId, out ClientInfo client))
                {
                    connection = client.Connection;
                }
            }
            return await proxyMessengerSender.Test(new ProxyInfo
            {
                AddressType = EnumProxyAddressType.IPV4,
                Command = EnumProxyCommand.Connect,
                Connection = connection,
                PluginId = config.Plugin,
                RequestId = 1,
                Step = EnumProxyStep.Command,
                TargetAddress = forward.TargetIp.GetAddressBytes(),
                TargetPort = forward.TargetPort
            });
        }

        /// <summary>
        /// 开启监听
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string StartP2P(uint id)
        {
            return StartP2P(GetP2PByID(id));
        }
        private string StartP2P(P2PListenInfo listen)
        {
            try
            {
                proxyServer.Start(listen.Port, config.Plugin, (byte)listen.AliveType);
                SaveP2PConfig();
            }
            catch (Exception ex)
            {
                Logger.Instance.DebugError(ex);
                return ex.Message;
            }
            return string.Empty;
        }

        /// <summary>
        /// 停止监听
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string StopP2P(uint id)
        {
            StopP2PListen(GetP2PByID(id));
            return string.Empty;
        }
        private void StartP2PAllWithListening()
        {
            p2pListens.ForEach(c =>
            {
                if (c.Listening)
                {
                    string error = StartP2P(c);
                    if (string.IsNullOrWhiteSpace(error) == false)
                    {
                        Logger.Instance.Error(error);
                    }
                }
            });
        }

        private P2PConfigInfo ReadP2PConfig()
        {
            P2PConfigInfo config = p2pConfigDataProvider.Load().Result ?? new P2PConfigInfo { };

            foreach (var listen in config.Webs)
            {
                listen.AliveType = ForwardAliveTypes.Web;
                foreach (P2PForwardInfo forward in listen.Forwards)
                {
                    try
                    {
                        forwardTargetCaching.Add(forward.SourceIp, listen.Port, new ForwardTargetCacheInfo
                        {
                            IPAddress = forward.TargetIp.GetAddressBytes(),
                            Port = forward.TargetPort,
                            ConnectionId = forward.ConnectionId,
                        });
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            foreach (var listen in config.Tunnels)
            {
                listen.AliveType = ForwardAliveTypes.Tunnel;
                foreach (P2PForwardInfo forward in listen.Forwards)
                {
                    try
                    {
                        forwardTargetCaching.Add(listen.Port, new ForwardTargetCacheInfo
                        {
                            IPAddress = forward.TargetIp.GetAddressBytes(),
                            Port = forward.TargetPort,
                            ConnectionId = forward.ConnectionId,
                        });
                    }
                    catch (Exception)
                    {
                    }
                }
            }


            p2pListens = config.Webs.Concat(config.Tunnels).ToList();
            listenNS.Reset(p2pListens.Count() == 0 ? 1 : p2pListens.Max(c => c.ID));

            var forwards = p2pListens.SelectMany(c => c.Forwards);
            forwardNS.Reset(forwards.Count() == 0 ? 1 : forwards.Max(c => c.ID));

            return config;
        }
        private void SaveP2PConfig()
        {
            p2PConfigInfo.Webs = p2pListens.Where(c => c.AliveType == ForwardAliveTypes.Web).ToList();
            p2PConfigInfo.Tunnels = p2pListens.Where(c => c.AliveType == ForwardAliveTypes.Tunnel).ToList();

            p2pConfigDataProvider.Save(p2PConfigInfo);
        }
    }

    public sealed class P2PListenAddParams
    {
        public uint ID { get; set; }
        public ushort Port { get; set; }
        public bool Listening { get; set; }
        public string Name { get; set; } = string.Empty;
        public ForwardAliveTypes AliveType { get; set; } = ForwardAliveTypes.Web;
        public string Desc { get; set; } = string.Empty;
    }

    public sealed class P2PForwardAddParams
    {
        public uint ListenID { get; set; }
        public P2PForwardInfo Forward { get; set; } = new P2PForwardInfo();
    }

    public sealed class P2PForwardRemoveParams
    {
        public uint ListenID { get; set; }
        public uint ForwardID { get; set; }
    }

    [Table("forwards")]
    public sealed class P2PConfigInfo
    {
        public P2PConfigInfo() { }
        public List<P2PListenInfo> Webs { get; set; } = new List<P2PListenInfo>();
        public List<P2PListenInfo> Tunnels { get; set; } = new List<P2PListenInfo>();
    }

    public sealed class P2PListenInfo
    {
        public uint ID { get; set; }
        public ushort Port { get; set; }
        public ForwardAliveTypes AliveType { get; set; } = ForwardAliveTypes.Web;
        public List<P2PForwardInfo> Forwards { get; set; } = new List<P2PForwardInfo>();
        public bool Listening { get; set; } = false;
        public string Desc { get; set; } = string.Empty;
    }
    public sealed class P2PForwardInfo
    {
        public uint ID { get; set; }
        public ulong ConnectionId { get; set; }
        public string SourceIp { get; set; } = string.Empty;
        public IPAddress TargetIp { get; set; } = IPAddress.Any;
        public ushort TargetPort { get; set; }
        public string Desc { get; set; } = string.Empty;
    }

}
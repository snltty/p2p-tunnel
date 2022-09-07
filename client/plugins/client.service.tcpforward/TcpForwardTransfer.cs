using client.messengers.register;
using common.libs;
using common.libs.database;
using common.libs.extends;
using common.server;
using common.server.model;
using common.tcpforward;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace client.service.tcpforward
{
    /// <summary>
    /// tcp转发中转器和入口
    /// </summary>
    public class TcpForwardTransfer : TcpForwardTransferBase
    {
        private readonly P2PConfigInfo p2PConfigInfo;
        public List<P2PListenInfo> p2pListens = new List<P2PListenInfo>();
        private readonly IConfigDataProvider<P2PConfigInfo> p2pConfigDataProvider;
        private readonly ITcpForwardTargetCaching<TcpForwardTargetCacheInfo> tcpForwardTargetCaching;
        private readonly ITcpForwardServer tcpForwardServer;
        NumberSpaceInt32 listenNS = new NumberSpaceInt32();
        NumberSpaceInt32 forwardNS = new NumberSpaceInt32();

        private readonly ServerForwardConfigInfo serverForwardConfigInfo;
        public List<ServerForwardItemInfo> serverForwards = new List<ServerForwardItemInfo>();
        private readonly IConfigDataProvider<ServerForwardConfigInfo> serverConfigDataProvider;

        private readonly Config clientConfig;
        private readonly RegisterStateInfo registerStateInfo;
        private readonly ui.api.Config uiconfig;

        private readonly TcpForwardMessengerSender tcpForwardMessengerSender;

        public TcpForwardTransfer(
            IConfigDataProvider<P2PConfigInfo> p2pConfigDataProvider,
            ITcpForwardTargetCaching<TcpForwardTargetCacheInfo> tcpForwardTargetCaching,
            ITcpForwardServer tcpForwardServer,

            IConfigDataProvider<ServerForwardConfigInfo> serverConfigDataProvider,

            common.tcpforward.Config config,
            Config clientConfig,
            RegisterStateInfo registerStateInfo,

           TcpForwardMessengerSender tcpForwardMessengerSender,
           ITcpForwardTargetProvider tcpForwardTargetProvider, ui.api.Config uiconfig) : base(tcpForwardServer, tcpForwardMessengerSender, tcpForwardTargetProvider)
        {
            this.p2pConfigDataProvider = p2pConfigDataProvider;
            this.tcpForwardTargetCaching = tcpForwardTargetCaching;

            this.serverConfigDataProvider = serverConfigDataProvider;

            this.clientConfig = clientConfig;
            this.registerStateInfo = registerStateInfo;

            this.tcpForwardServer = tcpForwardServer;
            this.tcpForwardMessengerSender = tcpForwardMessengerSender;
            this.uiconfig = uiconfig;

            p2PConfigInfo = ReadP2PConfig();
            serverForwardConfigInfo = ReadServerConfig();

            tcpForwardServer.Init(config.NumConnections, config.BufferSize);
            tcpForwardServer.OnListeningChange.Sub((model) =>
            {
                if (model.Port == 0)
                {
                    p2pListens.ForEach(c =>
                    {
                        c.Listening = model.State;
                    });
                }
                else
                {
                    P2PListenInfo listen = GetP2PByPort(model.Port);
                    listen.Listening = model.State;
                }

            });
            registerStateInfo.OnRegisterStateChange.Sub((state) =>
            {
                if (state)
                {
                    RegisterServerForward();
                }
            });

            AppDomain.CurrentDomain.ProcessExit += (s, e) => ClearPac();
            //安卓注释
            //Console.CancelKeyPress += (s, e) => ClearPac();


            StartP2PAllWithListening();
        }

        #region p2p

        public string AddP2PListen(P2PListenAddParams param)
        {
            try
            {

                P2PListenInfo oldPort = GetP2PByPort(param.Port);
                if (oldPort.ID > 0 && oldPort.ID != param.ID)
                {
                    return "已存在";
                }

                if (param.ForwardType == TcpForwardTypes.PROXY && p2pListens.Where(c => c.ForwardType == TcpForwardTypes.PROXY && c.ID != param.ID).Count() > 0)
                {
                    return "http代理仅能添加一条";
                }

                P2PListenInfo old = GetP2PByID(param.ID);
                bool listening = old.Listening;
                if (old.ID > 0)
                {
                    StopP2PListen(old);
                    old.Port = param.Port;
                    old.AliveType = param.AliveType;
                    old.ForwardType = param.ForwardType;
                    old.TunnelType = param.TunnelType;
                    old.Name = param.Name;
                    old.IsCustomPac = param.IsCustomPac;
                    old.IsPac = param.IsPac;
                    old.Pac = param.Pac;
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
                        ForwardType = param.ForwardType,
                        TunnelType = param.TunnelType,
                        Name = param.Name,
                        Pac = param.Pac,
                        IsCustomPac = param.IsCustomPac,
                        IsPac = param.IsPac,
                        Desc = param.Desc
                    });
                }

                if (listening)
                {
                    StartP2P(param.ID);
                }

                if (param.ForwardType == TcpForwardTypes.PROXY)
                {
                    tcpForwardTargetCaching.Remove(param.Port);
                    tcpForwardTargetCaching.Add(param.Port, new TcpForwardTargetCacheInfo
                    {
                        Endpoint = null,
                        Name = param.Name,
                        TunnelType = param.TunnelType,
                        ForwardType = param.ForwardType
                    });
                }
                SaveP2PConfig();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return string.Empty;
        }
        public void RemoveP2PListen(int listenid)
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
                    tcpForwardTargetCaching.Remove(item.TargetIp, item.TargetPort);
                }
                SaveP2PConfig();
            }
        }

        public void StopP2PListen(int listenid)
        {
            StopP2PListen(GetP2PByID(listenid));
        }
        private void StopP2PListen(P2PListenInfo listen)
        {
            if (listen.ID > 0)
            {
                tcpForwardServer.Stop(listen.Port);
                if (listen.ForwardType == TcpForwardTypes.PROXY)
                {
                    ClearPac();
                }
            }
        }

        public P2PListenInfo GetP2PByID(int id)
        {
            return p2pListens.FirstOrDefault(c => c.ID == id) ?? new P2PListenInfo { };
        }
        private P2PListenInfo GetP2PByPort(int port)
        {
            return p2pListens.FirstOrDefault(c => c.Port == port) ?? new P2PListenInfo { };
        }

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
                if (listen.AliveType == TcpForwardAliveTypes.WEB)
                {
                    tcpForwardTargetCaching.Remove(saveInfo.SourceIp, listen.Port);
                }
                else
                {
                    tcpForwardTargetCaching.Remove(listen.Port);
                }
                saveInfo.Desc = forward.Forward.Desc;
                saveInfo.SourceIp = forward.Forward.SourceIp;
                saveInfo.TargetIp = forward.Forward.TargetIp;
                saveInfo.TargetPort = forward.Forward.TargetPort;
                saveInfo.Name = forward.Forward.Name;
                saveInfo.TunnelType = forward.Forward.TunnelType;
            }
            else
            {
                if (listen.AliveType == TcpForwardAliveTypes.WEB)
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

            if (listen.AliveType == TcpForwardAliveTypes.WEB)
            {
                tcpForwardTargetCaching.Add(forward.Forward.SourceIp, listen.Port, new TcpForwardTargetCacheInfo
                {
                    Endpoint = NetworkHelper.EndpointToArray(forward.Forward.TargetIp, forward.Forward.TargetPort),
                    Name = forward.Forward.Name,
                    TunnelType = forward.Forward.TunnelType,
                    ForwardType = TcpForwardTypes.FORWARD
                });
            }
            else
            {
                tcpForwardTargetCaching.Add(listen.Port, new TcpForwardTargetCacheInfo
                {
                    Endpoint = NetworkHelper.EndpointToArray(forward.Forward.TargetIp, forward.Forward.TargetPort),
                    Name = forward.Forward.Name,
                    TunnelType = forward.Forward.TunnelType,
                    ForwardType = TcpForwardTypes.FORWARD
                });
            }

            SaveP2PConfig();

            return string.Empty;
        }
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
            tcpForwardTargetCaching.Remove(forwardInfo.SourceIp, listen.Port);
            SaveP2PConfig();
        }

        public string StartP2P(int id)
        {
            return StartP2P(GetP2PByID(id));
        }
        public string StartP2P(P2PListenInfo listen)
        {
            try
            {
                tcpForwardServer.Start(listen.Port, listen.AliveType);
                if (listen.ForwardType == TcpForwardTypes.PROXY)
                {
                    UpdatePac(listen);
                }
                SaveP2PConfig();
            }
            catch (Exception ex)
            {
                Logger.Instance.DebugError(ex);
                return ex.Message;
            }
            return string.Empty;
        }

        public string StopP2P(int id)
        {
            StopP2PListen(GetP2PByID(id));
            return string.Empty;
        }
        private void StartP2PAllWithListening()
        {
            //IEnumerable<(int, TcpForwardAliveTypes)> a = p2pListens.Where(c=>c.Listening == true).Select(c => (c.Port, c.AliveType));

            p2pListens.ForEach(c =>
            {
                if (c.Listening)
                {
                    string error = StartP2P(c);
                    if (!string.IsNullOrWhiteSpace(error))
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
                listen.AliveType = TcpForwardAliveTypes.WEB;
                foreach (P2PForwardInfo forward in listen.Forwards)
                {
                    try
                    {
                        tcpForwardTargetCaching.Add(forward.SourceIp, listen.Port, new TcpForwardTargetCacheInfo
                        {
                            Endpoint = NetworkHelper.EndpointToArray(forward.TargetIp, forward.TargetPort),
                            Name = forward.Name,
                            TunnelType = forward.TunnelType
                        });
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            foreach (var listen in config.Tunnels)
            {
                listen.AliveType = TcpForwardAliveTypes.TUNNEL;
                foreach (P2PForwardInfo forward in listen.Forwards)
                {
                    try
                    {
                        tcpForwardTargetCaching.Add(listen.Port, new TcpForwardTargetCacheInfo
                        {
                            Endpoint = NetworkHelper.EndpointToArray(forward.TargetIp, forward.TargetPort),
                            Name = forward.Name,
                            TunnelType = forward.TunnelType
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
            p2PConfigInfo.Webs = p2pListens.Where(c => c.AliveType == TcpForwardAliveTypes.WEB).ToList();
            p2PConfigInfo.Tunnels = p2pListens.Where(c => c.AliveType == TcpForwardAliveTypes.TUNNEL).ToList();

            p2pConfigDataProvider.Save(p2PConfigInfo);
        }
        #endregion

        #region 服务器代理

        public async Task<int[]> GetServerPorts()
        {
            var resp = await tcpForwardMessengerSender.GetPorts(registerStateInfo.OnlineConnection);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return resp.Data.DeBytes2IntArray();
            }

            return Array.Empty<int>();
        }
        public async Task<string> AddServerForward(ServerForwardItemInfo forward)
        {
            var resp = await tcpForwardMessengerSender.Register(registerStateInfo.OnlineConnection, new TcpForwardRegisterParamsInfo
            {
                AliveType = forward.AliveType,
                SourceIp = forward.Domain,
                SourcePort = forward.ServerPort,
                TargetIp = forward.LocalIp,
                TargetPort = forward.LocalPort,
                TargetName = clientConfig.Client.Name,
                TunnelType = forward.TunnelType,

            }).ConfigureAwait(false);
            if (resp.Code != MessageResponeCodes.OK)
            {
                return resp.Code.GetDesc((byte)resp.Code);
            }

            TcpForwardRegisterResult result = new TcpForwardRegisterResult();
            result.DeBytes(resp.Data);
            if (result.Code != TcpForwardRegisterResultCodes.OK)
            {
                return $"{result.Code.GetDesc((byte)result.Code)},{result.Msg}";
            }

            serverForwards.Remove(serverForwards.FirstOrDefault(c => c.Domain == forward.Domain && c.ServerPort == forward.ServerPort));
            serverForwards.Add(forward);
            SaveServerConfig();
            return string.Empty;
        }
        public async Task<string> StartServerForward(ServerForwardItemInfo forward)
        {
            ServerForwardItemInfo forwardInfo;
            if (forward.AliveType == TcpForwardAliveTypes.WEB)
            {
                forwardInfo = serverForwards.FirstOrDefault(c => c.Domain == forward.Domain && c.ServerPort == forward.ServerPort);
            }
            else
            {
                forwardInfo = serverForwards.FirstOrDefault(c => c.ServerPort == forward.ServerPort);
            }
            if (forwardInfo == null)
            {
                return "未找到操作对象";
            }
            var resp = await tcpForwardMessengerSender.Register(registerStateInfo.OnlineConnection, new TcpForwardRegisterParamsInfo
            {
                AliveType = forward.AliveType,
                SourceIp = forward.Domain,
                SourcePort = forward.ServerPort,
                TargetIp = forward.Domain,
                TargetName = clientConfig.Client.Name,
                TargetPort = forward.ServerPort,
                TunnelType = forward.TunnelType
            }).ConfigureAwait(false);
            if (resp.Code != MessageResponeCodes.OK)
            {
                return resp.Code.GetDesc((byte)resp.Code);
            }
            forwardInfo.Listening = true;
            SaveServerConfig();
            return string.Empty;
        }
        public async Task<string> StopServerForward(ServerForwardItemInfo forward)
        {
            ServerForwardItemInfo forwardInfo;
            if (forward.AliveType == TcpForwardAliveTypes.WEB)
            {
                forwardInfo = serverForwards.FirstOrDefault(c => c.Domain == forward.Domain && c.ServerPort == forward.ServerPort);
            }
            else
            {
                forwardInfo = serverForwards.FirstOrDefault(c => c.ServerPort == forward.ServerPort);
            }
            if (forwardInfo == null)
            {
                return "未找到操作对象";
            }
            var resp = await tcpForwardMessengerSender.UnRegister(registerStateInfo.OnlineConnection, new TcpForwardUnRegisterParamsInfo
            {
                AliveType = forward.AliveType,
                SourceIp = forward.Domain,
                SourcePort = forward.ServerPort,
            }).ConfigureAwait(false);
            if (resp.Code != MessageResponeCodes.OK)
            {
                return resp.Code.GetDesc((byte)resp.Code);
            }
            forwardInfo.Listening = false;
            SaveServerConfig();
            return string.Empty;
        }
        public async Task<string> RemoveServerForward(ServerForwardItemInfo forward)
        {
            ServerForwardItemInfo forwardInfo;
            if (forward.AliveType == TcpForwardAliveTypes.WEB)
            {
                forwardInfo = serverForwards.FirstOrDefault(c => c.Domain == forward.Domain && c.ServerPort == forward.ServerPort);
            }
            else
            {
                forwardInfo = serverForwards.FirstOrDefault(c => c.ServerPort == forward.ServerPort);
            }
            if (forwardInfo == null)
            {
                return "未找到删除对象";
            }
            var resp = await tcpForwardMessengerSender.UnRegister(registerStateInfo.OnlineConnection, new TcpForwardUnRegisterParamsInfo
            {
                AliveType = forward.AliveType,
                SourceIp = forward.Domain,
                SourcePort = forward.ServerPort,
            }).ConfigureAwait(false);
            if (resp.Code != MessageResponeCodes.OK)
            {
                return resp.Code.GetDesc((byte)resp.Code);
            }
            serverForwards.Remove(forwardInfo);
            SaveServerConfig();

            return string.Empty;
        }


        private ServerForwardConfigInfo ReadServerConfig()
        {
            var config = serverConfigDataProvider.Load().Result;
            foreach (var item in config.Webs)
            {
                item.AliveType = TcpForwardAliveTypes.WEB;
            }
            foreach (var item in config.Tunnels)
            {
                item.AliveType = TcpForwardAliveTypes.TUNNEL;
            }
            serverForwards = config.Webs.Concat(config.Tunnels).ToList();
            return config;
        }
        private void SaveServerConfig()
        {
            serverForwardConfigInfo.Webs = serverForwards.Where(c => c.AliveType == TcpForwardAliveTypes.WEB).ToArray();
            serverForwardConfigInfo.Tunnels = serverForwards.Where(c => c.AliveType == TcpForwardAliveTypes.TUNNEL).ToArray();
            serverConfigDataProvider.Save(serverForwardConfigInfo);
        }

        private void RegisterServerForward()
        {
            Task.Run(async () =>
            {
                foreach (var item in serverForwardConfigInfo.Webs)
                {
                    await SendRegister(item, TcpForwardAliveTypes.WEB).ConfigureAwait(false);
                }
                foreach (var item in serverForwardConfigInfo.Tunnels.Where(c => c.Listening == true))
                {
                    await SendRegister(item, TcpForwardAliveTypes.TUNNEL).ConfigureAwait(false);
                }
            });
        }
        private async Task SendRegister(ServerForwardItemInfo item, TcpForwardAliveTypes type)
        {
            var resp = await tcpForwardMessengerSender.Register(registerStateInfo.OnlineConnection, new TcpForwardRegisterParamsInfo
            {
                AliveType = type,
                SourceIp = item.Domain,
                SourcePort = item.ServerPort,
                TargetIp = item.LocalIp,
                TargetPort = item.LocalPort,
                TargetName = clientConfig.Client.Name,
                TunnelType = item.TunnelType
            }).ConfigureAwait(false);
            PrintResult(item, resp, type);
        }
        private void PrintResult(ServerForwardItemInfo item, MessageResponeInfo resp, TcpForwardAliveTypes type)
        {
            bool success = false;
            StringBuilder sb = new StringBuilder();

            sb.Append($"注册服务器代理Tcp转发【{type.GetDesc((byte)type)}】代理 {item.Domain}:{item.ServerPort} -> {item.LocalIp}:{item.LocalPort}");
            if (resp.Code != MessageResponeCodes.OK)
            {
                sb.Append($" 【{resp.Code.GetDesc((byte)resp.Code)}】");
            }
            else
            {
                TcpForwardRegisterResult result = new TcpForwardRegisterResult();
                result.DeBytes(resp.Data);
                sb.Append($" 【{result.Code.GetDesc((byte)result.Code)},{result.Msg}】{result.Msg}");
                success = result.Code == TcpForwardRegisterResultCodes.OK;
            }
            if (success)
            {
                Logger.Instance.Info(sb.ToString());
            }
            else
            {
                Logger.Instance.Warning(sb.ToString());
            }
        }

        #endregion

        public string GetPac()
        {
            try
            {
                return File.ReadAllText("./proxy-custom.pac");
            }
            catch (Exception)
            {
            }
            return string.Empty;
        }
        public string UpdatePac(P2PListenInfo param)
        {
            try
            {
                string pacContent = param.Pac;
                string file = Path.Join(uiconfig.Web.Root, "proxy-custom.pac");
                if (param.IsCustomPac)
                {
                    if (string.IsNullOrEmpty(param.Pac))
                    {
                        pacContent = File.ReadAllText("./proxy-custom.pac");
                    }
                    else
                    {
                        File.WriteAllText("./proxy-custom.pac", param.Pac);
                    }
                }
                else
                {
                    file = Path.Join(uiconfig.Web.Root, "proxy.pac");
                    pacContent = File.ReadAllText("./proxy.pac");
                }

                pacContent = pacContent.Replace("{socks5-address}", $"127.0.0.1:{param.Port}");
                File.WriteAllText(file, pacContent);

                if (param.IsPac)
                {
                    SetPac($"http://{uiconfig.Web.BindIp}:{uiconfig.Web.Port}/{Path.GetFileName(file)}");
                }
                else
                {
                    ClearPac();
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public void SetPac(string url)
        {
            ProxySystemSetting.Set(url);
        }
        public void ClearPac()
        {
            ProxySystemSetting.Clear();
        }
    }

    public class P2PListenAddParams
    {
        public int ID { get; set; } = 0;
        public int Port { get; set; } = 0;
        public bool Listening { get; set; } = false;
        public TcpForwardTypes ForwardType { get; set; } = TcpForwardTypes.FORWARD;
        public string Name { get; set; } = String.Empty;
        public TcpForwardTunnelTypes TunnelType { get; set; } = TcpForwardTunnelTypes.TCP_FIRST;

        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.WEB;

        public string Desc { get; set; } = String.Empty;
        public bool IsPac { get; set; } = false;
        public bool IsCustomPac { get; set; } = false;
        public string Pac { get; set; } = string.Empty;
    }
    public class P2PForwardAddParams
    {
        public int ListenID { get; set; } = 0;
        public P2PForwardInfo Forward { get; set; } = new P2PForwardInfo();
    }
    public class P2PForwardRemoveParams
    {
        public int ListenID { get; set; } = 0;
        public int ForwardID { get; set; } = 0;
    }

    [Table("p2p-tcp-forwards")]
    public class P2PConfigInfo
    {
        public P2PConfigInfo() { }

        public List<P2PListenInfo> Webs { get; set; } = new List<P2PListenInfo>();
        public List<P2PListenInfo> Tunnels { get; set; } = new List<P2PListenInfo>();
    }
    public class P2PListenInfo
    {
        public int ID { get; set; } = 0;
        public int Port { get; set; } = 0;

        public TcpForwardTypes ForwardType { get; set; } = TcpForwardTypes.FORWARD;
        /// <summary>
        /// 代理时 必须 TcpForwardAliveTypes.WEB
        /// </summary>
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.WEB;

        public List<P2PForwardInfo> Forwards { get; set; } = new List<P2PForwardInfo>();

        /// <summary>
        /// 代理时填写
        /// </summary>
        public TcpForwardTunnelTypes TunnelType { get; set; } = TcpForwardTunnelTypes.TCP_FIRST;
        /// <summary>
        /// 代理时填写
        /// </summary>
        public string Name { get; set; } = String.Empty;
        /// <summary>
        /// 是否直接开始监听
        /// </summary>
        public bool Listening { get; set; } = false;

        public string Desc { get; set; } = string.Empty;

        /// <summary>
        /// 代理时填写，是否设置pac代理
        /// </summary>
        public bool IsPac { get; set; } = false;
        /// <summary>
        /// 代理时填写 是否自定义pac内容
        /// </summary>
        public bool IsCustomPac { get; set; } = false;
        /// <summary>
        /// 自定义pac内容时传
        /// </summary>
        public string Pac { get; set; } = string.Empty;
    }
    public class P2PForwardInfo
    {
        public int ID { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public string SourceIp { get; set; } = string.Empty;
        public string TargetIp { get; set; } = string.Empty;
        public int TargetPort { get; set; } = 0;
        public string Desc { get; set; } = string.Empty;
        public TcpForwardTunnelTypes TunnelType { get; set; } = TcpForwardTunnelTypes.TCP_FIRST;
    }


    [Table("server-tcp-forwards")]
    public class ServerForwardConfigInfo
    {
        public ServerForwardItemInfo[] Webs { get; set; } = Array.Empty<ServerForwardItemInfo>();
        public ServerForwardItemInfo[] Tunnels { get; set; } = Array.Empty<ServerForwardItemInfo>();
    }
    public class ServerForwardItemInfo
    {
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.WEB;
        public string Domain { get; set; }
        public int ServerPort { get; set; }
        public string LocalIp { get; set; }
        public int LocalPort { get; set; }
        public TcpForwardTunnelTypes TunnelType { get; set; } = TcpForwardTunnelTypes.TCP_FIRST;
        public string Desc { get; set; } = string.Empty;
        public bool Listening { get; set; } = false;
    }
}
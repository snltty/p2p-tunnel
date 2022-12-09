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
using System.Text;
using System.Threading.Tasks;

namespace client.service.tcpforward
{
    /// <summary>
    /// tcp转发中转器和入口
    /// </summary>
    public sealed class TcpForwardTransfer : TcpForwardTransferBase
    {
        private readonly P2PConfigInfo p2PConfigInfo;
        /// <summary>
        /// 监听列表
        /// </summary>
        public List<P2PListenInfo> p2pListens = new List<P2PListenInfo>();
        private readonly IConfigDataProvider<P2PConfigInfo> p2pConfigDataProvider;
        private readonly ITcpForwardTargetCaching<TcpForwardTargetCacheInfo> tcpForwardTargetCaching;
        private readonly ITcpForwardServer tcpForwardServer;
        NumberSpaceUInt32 listenNS = new NumberSpaceUInt32();
        NumberSpaceUInt32 forwardNS = new NumberSpaceUInt32();

        private readonly ServerForwardConfigInfo serverForwardConfigInfo;
        /// <summary>
        /// 服务器转发列表
        /// </summary>
        public List<ServerForwardItemInfo> serverForwards = new List<ServerForwardItemInfo>();
        private readonly IConfigDataProvider<ServerForwardConfigInfo> serverConfigDataProvider;

        private readonly Config clientConfig;
        private readonly RegisterStateInfo registerStateInfo;
        private readonly ui.api.Config uiconfig;

        private readonly TcpForwardMessengerSender tcpForwardMessengerSender;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p2pConfigDataProvider"></param>
        /// <param name="tcpForwardTargetCaching"></param>
        /// <param name="tcpForwardServer"></param>
        /// <param name="serverConfigDataProvider"></param>
        /// <param name="config"></param>
        /// <param name="clientConfig"></param>
        /// <param name="registerStateInfo"></param>
        /// <param name="tcpForwardMessengerSender"></param>
        /// <param name="tcpForwardTargetProvider"></param>
        /// <param name="uiconfig"></param>
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

                if (param.ForwardType == TcpForwardTypes.Proxy && p2pListens.Where(c => c.ForwardType == TcpForwardTypes.Proxy && c.ID != param.ID).Count() > 0)
                {
                    return "http代理仅能添加一条";
                }

                P2PListenInfo old = GetP2PByID(param.ID);
                bool listening = param.ForwardType == TcpForwardTypes.Proxy ? param.Listening : (old.Listening || param.Listening);
                if (old.ID > 0)
                {
                    StopP2PListen(old);
                    old.Port = param.Port;
                    old.AliveType = param.AliveType;
                    old.ForwardType = param.ForwardType;
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

                if (param.ForwardType == TcpForwardTypes.Proxy)
                {
                    tcpForwardTargetCaching.Remove(param.Port);
                    tcpForwardTargetCaching.Add(param.Port, new TcpForwardTargetCacheInfo
                    {
                        Endpoint = null,
                        Name = param.Name,
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
                    tcpForwardTargetCaching.Remove(item.TargetIp, item.TargetPort);
                }
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
                tcpForwardServer.Stop(listen.Port);
                if (listen.ForwardType == TcpForwardTypes.Proxy)
                {
                    ClearPac();
                }
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
                if (listen.AliveType == TcpForwardAliveTypes.Web)
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
            }
            else
            {
                if (listen.AliveType == TcpForwardAliveTypes.Web)
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

            if (listen.AliveType == TcpForwardAliveTypes.Web)
            {
                tcpForwardTargetCaching.Add(forward.Forward.SourceIp, listen.Port, new TcpForwardTargetCacheInfo
                {
                    Endpoint = NetworkHelper.EndpointToArray(forward.Forward.TargetIp, forward.Forward.TargetPort),
                    Name = forward.Forward.Name,
                    ForwardType = TcpForwardTypes.Forward
                });
            }
            else
            {
                tcpForwardTargetCaching.Add(listen.Port, new TcpForwardTargetCacheInfo
                {
                    Endpoint = NetworkHelper.EndpointToArray(forward.Forward.TargetIp, forward.Forward.TargetPort),
                    Name = forward.Forward.Name,
                    ForwardType = TcpForwardTypes.Forward
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
            tcpForwardTargetCaching.Remove(forwardInfo.SourceIp, listen.Port);
            SaveP2PConfig();
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
                tcpForwardServer.Start(listen.Port, listen.AliveType);
                if (listen.ForwardType == TcpForwardTypes.Proxy)
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
                listen.AliveType = TcpForwardAliveTypes.Web;
                foreach (P2PForwardInfo forward in listen.Forwards)
                {
                    try
                    {
                        tcpForwardTargetCaching.Add(forward.SourceIp, listen.Port, new TcpForwardTargetCacheInfo
                        {
                            Endpoint = NetworkHelper.EndpointToArray(forward.TargetIp, forward.TargetPort),
                            Name = forward.Name
                        });
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            foreach (var listen in config.Tunnels)
            {
                listen.AliveType = TcpForwardAliveTypes.Tunnel;
                foreach (P2PForwardInfo forward in listen.Forwards)
                {
                    try
                    {
                        tcpForwardTargetCaching.Add(listen.Port, new TcpForwardTargetCacheInfo
                        {
                            Endpoint = NetworkHelper.EndpointToArray(forward.TargetIp, forward.TargetPort),
                            Name = forward.Name
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
            p2PConfigInfo.Webs = p2pListens.Where(c => c.AliveType == TcpForwardAliveTypes.Web).ToList();
            p2PConfigInfo.Tunnels = p2pListens.Where(c => c.AliveType == TcpForwardAliveTypes.Tunnel).ToList();

            p2pConfigDataProvider.Save(p2PConfigInfo);
        }
        #endregion

        #region 服务器代理

        /// <summary>
        /// 获取服务器端口
        /// </summary>
        /// <returns></returns>
        public async Task<ushort[]> GetServerPorts()
        {
            var resp = await tcpForwardMessengerSender.GetPorts(registerStateInfo.OnlineConnection);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return resp.Data.ToUInt16Array();
            }

            return Array.Empty<ushort>();
        }
        /// <summary>
        /// 添加服务器转发
        /// </summary>
        /// <param name="forward"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 开启服务器转发
        /// </summary>
        /// <param name="forward"></param>
        /// <returns></returns>
        public async Task<string> StartServerForward(ServerForwardItemInfo forward)
        {
            ServerForwardItemInfo forwardInfo;
            if (forward.AliveType == TcpForwardAliveTypes.Web)
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
                TargetIp = forward.LocalIp,
                TargetName = clientConfig.Client.Name,
                TargetPort = forward.LocalPort,
            }).ConfigureAwait(false);
            if (resp.Code != MessageResponeCodes.OK)
            {
                return resp.Code.GetDesc((byte)resp.Code);
            }
            forwardInfo.Listening = true;
            SaveServerConfig();
            return string.Empty;
        }
        /// <summary>
        /// 停止服务器转发
        /// </summary>
        /// <param name="forward"></param>
        /// <returns></returns>
        public async Task<string> StopServerForward(ServerForwardItemInfo forward)
        {
            ServerForwardItemInfo forwardInfo;
            if (forward.AliveType == TcpForwardAliveTypes.Web)
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
        /// <summary>
        /// 删除服务器转发
        /// </summary>
        /// <param name="forward"></param>
        /// <returns></returns>
        public async Task<string> RemoveServerForward(ServerForwardItemInfo forward)
        {
            ServerForwardItemInfo forwardInfo;
            if (forward.AliveType == TcpForwardAliveTypes.Web)
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
                item.AliveType = TcpForwardAliveTypes.Web;
            }
            foreach (var item in config.Tunnels)
            {
                item.AliveType = TcpForwardAliveTypes.Tunnel;
            }
            serverForwards = config.Webs.Concat(config.Tunnels).ToList();
            return config;
        }
        private void SaveServerConfig()
        {
            serverForwardConfigInfo.Webs = serverForwards.Where(c => c.AliveType == TcpForwardAliveTypes.Web).ToArray();
            serverForwardConfigInfo.Tunnels = serverForwards.Where(c => c.AliveType == TcpForwardAliveTypes.Tunnel).ToArray();
            serverConfigDataProvider.Save(serverForwardConfigInfo);
        }

        private void RegisterServerForward()
        {
            foreach (var item in serverForwardConfigInfo.Webs)
            {
                SendRegister(item, TcpForwardAliveTypes.Web);
            }
            foreach (var item in serverForwardConfigInfo.Tunnels.Where(c => c.Listening == true))
            {
                SendRegister(item, TcpForwardAliveTypes.Tunnel);
            }
        }
        private void SendRegister(ServerForwardItemInfo item, TcpForwardAliveTypes type)
        {
            tcpForwardMessengerSender.Register(registerStateInfo.OnlineConnection, new TcpForwardRegisterParamsInfo
            {
                AliveType = type,
                SourceIp = item.Domain,
                SourcePort = item.ServerPort,
                TargetIp = item.LocalIp,
                TargetPort = item.LocalPort,
                TargetName = clientConfig.Client.Name,
            }).ContinueWith((result) =>
            {
                PrintResult(item, result.Result, type);
            });
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

        /// <summary>
        /// 获取pac
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// 更新pac
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
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
                    SetPac($"http://{(uiconfig.Web.BindIp == "+" ? "127.0.0.1" : uiconfig.Web.BindIp)}:{uiconfig.Web.Port}/{Path.GetFileName(file)}");
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
        /// <summary>
        /// 设置系统pac
        /// </summary>
        /// <param name="url"></param>
        public void SetPac(string url)
        {
            ProxySystemSetting.Set(url);
        }
        /// <summary>
        /// 清除pac
        /// </summary>
        public void ClearPac()
        {
            ProxySystemSetting.Clear();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class P2PListenAddParams
    {
        /// <summary>
        /// 
        /// </summary>
        public uint ID { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public ushort Port { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public bool Listening { get; set; } = false;
        /// <summary>
        /// 
        /// </summary>
        public TcpForwardTypes ForwardType { get; set; } = TcpForwardTypes.Forward;
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// 
        /// </summary>
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.Web;
        /// <summary>
        /// 
        /// </summary>
        public string Desc { get; set; } = string.Empty;
        /// <summary>
        /// 
        /// </summary>
        public bool IsPac { get; set; } = false;
        /// <summary>
        /// 
        /// </summary>
        public bool IsCustomPac { get; set; } = false;
        /// <summary>
        /// 
        /// </summary>
        public string Pac { get; set; } = string.Empty;
    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class P2PForwardAddParams
    {
        /// <summary>
        /// 
        /// </summary>
        public uint ListenID { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public P2PForwardInfo Forward { get; set; } = new P2PForwardInfo();
    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class P2PForwardRemoveParams
    {
        /// <summary>
        /// 
        /// </summary>
        public uint ListenID { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public uint ForwardID { get; set; } = 0;
    }

    /// <summary>
    /// 
    /// </summary>
    [Table("p2p-tcp-forwards")]
    public sealed class P2PConfigInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public P2PConfigInfo() { }
        /// <summary>
        /// 
        /// </summary>
        public List<P2PListenInfo> Webs { get; set; } = new List<P2PListenInfo>();
        /// <summary>
        /// 
        /// </summary>
        public List<P2PListenInfo> Tunnels { get; set; } = new List<P2PListenInfo>();
    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class P2PListenInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public uint ID { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public ushort Port { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public TcpForwardTypes ForwardType { get; set; } = TcpForwardTypes.Forward;
        /// <summary>
        /// 代理时 必须 TcpForwardAliveTypes.WEB
        /// </summary>
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.Web;
        /// <summary>
        /// 
        /// </summary>
        public List<P2PForwardInfo> Forwards { get; set; } = new List<P2PForwardInfo>();

        /// <summary>
        /// 代理时填写
        /// </summary>
        public string Name { get; set; } = String.Empty;
        /// <summary>
        /// 是否直接开始监听
        /// </summary>
        public bool Listening { get; set; } = false;
        /// <summary>
        /// 
        /// </summary>
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
    /// <summary>
    /// 
    /// </summary>
    public sealed class P2PForwardInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public uint ID { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// 
        /// </summary>
        public string SourceIp { get; set; } = string.Empty;
        /// <summary>
        /// 
        /// </summary>
        public string TargetIp { get; set; } = string.Empty;
        /// <summary>
        /// 
        /// </summary>
        public ushort TargetPort { get; set; } = 0;
        /// <summary>
        /// 
        /// </summary>
        public string Desc { get; set; } = string.Empty;
    }

    /// <summary>
    /// 
    /// </summary>
    [Table("server-tcp-forwards")]
    public sealed class ServerForwardConfigInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public ServerForwardItemInfo[] Webs { get; set; } = Array.Empty<ServerForwardItemInfo>();
        /// <summary>
        /// 
        /// </summary>
        public ServerForwardItemInfo[] Tunnels { get; set; } = Array.Empty<ServerForwardItemInfo>();
    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class ServerForwardItemInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.Web;
        /// <summary>
        /// 
        /// </summary>
        public string Domain { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ushort ServerPort { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string LocalIp { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ushort LocalPort { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Desc { get; set; } = string.Empty;
        /// <summary>
        /// 
        /// </summary>
        public bool Listening { get; set; } = false;
    }
}
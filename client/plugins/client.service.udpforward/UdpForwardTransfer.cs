using client.messengers.register;
using common.libs;
using common.libs.database;
using common.libs.extends;
using common.server;
using common.server.model;
using common.udpforward;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.udpforward
{
    /// <summary>
    /// tcp转发中转器和入口
    /// </summary>
    public sealed class UdpForwardTransfer : UdpForwardTransferBase
    {
        private readonly IUdpForwardTargetCaching<UdpForwardTargetCacheInfo> udpForwardTargetCaching;
        private readonly IUdpForwardServer udpForwardServer;
        NumberSpaceUInt32 listenNS = new NumberSpaceUInt32();

        /// <summary>
        /// 
        /// </summary>
        public readonly P2PConfigInfo p2PConfigInfo;
        private readonly IConfigDataProvider<P2PConfigInfo> p2pConfigDataProvider;
        /// <summary>
        /// 
        /// </summary>
        public readonly ServerConfigInfo serverConfigInfo;
        private readonly IConfigDataProvider<ServerConfigInfo> serverConfigDataProvider;
        private readonly Config clientConfig;
        private readonly RegisterStateInfo registerStateInfo;

        private readonly UdpForwardMessengerSender udpForwardMessengerSender;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="udpForwardTargetCaching"></param>
        /// <param name="udpForwardServer"></param>
        /// <param name="p2pConfigDataProvider"></param>
        /// <param name="serverConfigDataProvider"></param>
        /// <param name="clientConfig"></param>
        /// <param name="registerStateInfo"></param>
        /// <param name="udpForwardMessengerSender"></param>
        /// <param name="tcpForwardTargetProvider"></param>
        public UdpForwardTransfer(
            IUdpForwardTargetCaching<UdpForwardTargetCacheInfo> udpForwardTargetCaching,
            IUdpForwardServer udpForwardServer,

            IConfigDataProvider<P2PConfigInfo> p2pConfigDataProvider,
            IConfigDataProvider<ServerConfigInfo> serverConfigDataProvider,

            Config clientConfig,
            RegisterStateInfo registerStateInfo,

           UdpForwardMessengerSender udpForwardMessengerSender,
           IUdpForwardTargetProvider tcpForwardTargetProvider) : base(udpForwardServer, udpForwardMessengerSender, tcpForwardTargetProvider)
        {
            this.udpForwardTargetCaching = udpForwardTargetCaching;

            this.p2pConfigDataProvider = p2pConfigDataProvider;
            p2PConfigInfo = ReadP2PConfig();
            this.serverConfigDataProvider = serverConfigDataProvider;
            serverConfigInfo = ReadServerConfig();

            this.clientConfig = clientConfig;
            this.registerStateInfo = registerStateInfo;

            this.udpForwardServer = udpForwardServer;
            this.udpForwardMessengerSender = udpForwardMessengerSender;

            udpForwardServer.OnListenChange.Sub((model) =>
            {
                if (model.Port == 0)
                {
                    udpForwardTargetCaching.ClearConnection();
                }
                else
                {
                    GetP2PByPort(model.Port).Listening = model.State;
                }
            });
            registerStateInfo.OnRegisterStateChange.Sub((state) =>
            {
                if (state)
                {
                    RegisterServerForward();
                }
            });
            StartP2PAllWithListening();
        }

        #region p2p
        /// <summary>
        /// 添加监听
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public string AddP2PListen(P2PListenInfo param)
        {
            try
            {

                P2PListenInfo oldPort = GetP2PByPort(param.Port);
                bool listening = oldPort.Listening;
                if (oldPort.ID > 0 && oldPort.ID != param.ID)
                {
                    return "已存在";
                }

                udpForwardTargetCaching.Remove(oldPort.Port);
                if (oldPort.ID > 0)
                {
                    StopP2PListen(oldPort);
                    oldPort.Port = param.Port;
                    oldPort.TargetIp = param.TargetIp;
                    oldPort.TargetPort = param.TargetPort;
                    oldPort.Name = param.Name;
                    oldPort.Desc = param.Desc;
                    oldPort.Listening = param.Listening;
                }
                else
                {
                    param.ID = listenNS.Increment();
                    p2PConfigInfo.Tunnels.Add(new P2PListenInfo
                    {
                        Port = param.Port,
                        ID = param.ID,
                        Name = param.Name,
                        Listening = param.Listening,
                        TargetIp = param.TargetIp,
                        TargetPort = param.TargetPort,
                        Desc = param.Desc,
                    });
                }

                udpForwardTargetCaching.Add(param.Port, new UdpForwardTargetCacheInfo
                {
                    Name = param.Name,
                    Endpoint = NetworkHelper.EndpointToArray(param.TargetIp, param.TargetPort)
                });

                if (listening)
                {
                    StartP2P(param.Port);
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
        /// <param name="port"></param>
        public void RemoveP2PListen(int port)
        {
            RemoveP2PListen(GetP2PByPort(port));
        }
        private void RemoveP2PListen(P2PListenInfo listen)
        {
            if (listen.ID > 0)
            {
                StopP2PListen(listen);
                p2PConfigInfo.Tunnels.Remove(listen);
                udpForwardTargetCaching.Remove(listen.TargetPort);
                SaveP2PConfig();
            }
        }
        /// <summary>
        /// 停止监听
        /// </summary>
        /// <param name="port"></param>
        public void StopP2PListen(ushort port)
        {
            try
            {
                udpForwardServer.Stop(port);
            }
            catch (Exception)
            {
            }
        }
        private void StopP2PListen(P2PListenInfo listen)
        {
            StopP2PListen(listen.Port);
        }
        /// <summary>
        /// 获取监听
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public P2PListenInfo GetP2PByPort(int port)
        {
            return p2PConfigInfo.Tunnels.FirstOrDefault(c => c.Port == port) ?? new P2PListenInfo { };
        }
        /// <summary>
        /// 开启监听
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public string StartP2P(ushort port)
        {
            try
            {
                udpForwardServer.Start(port);
                SaveP2PConfig();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return string.Empty;
        }
        private string StartP2P(P2PListenInfo listen)
        {
            return StartP2P(listen.Port);
        }
        private void StartP2PAllWithListening()
        {
            foreach (var item in p2PConfigInfo.Tunnels.Where(c => c.Listening))
            {
                StartP2P(item);
            }
        }

        private P2PConfigInfo ReadP2PConfig()
        {
            P2PConfigInfo config = p2pConfigDataProvider.Load().Result ?? new P2PConfigInfo { };
            foreach (var listen in config.Tunnels)
            {
                try
                {
                    udpForwardTargetCaching.Add(listen.Port, new UdpForwardTargetCacheInfo
                    {
                        Endpoint = NetworkHelper.EndpointToArray(listen.TargetIp, listen.TargetPort),
                        Name = listen.Name
                    });
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex.Message);
                }
            }
            listenNS.Reset(config.Tunnels.Count > 0 ? config.Tunnels.Max(c => c.ID) : 1);
            return config;
        }
        private void SaveP2PConfig()
        {
            p2pConfigDataProvider.Save(p2PConfigInfo);
        }
        #endregion

        #region 服务器代理

        /// <summary>
        /// 获取服务器可用端口
        /// </summary>
        /// <returns></returns>
        public async Task<ushort[]> GetServerPorts()
        {
            var resp = await udpForwardMessengerSender.GetPorts(registerStateInfo.Connection);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return resp.Data.ToUInt16Array();
            }

            return Array.Empty<ushort>();
        }
        /// <summary>
        /// 添加转发
        /// </summary>
        /// <param name="forward"></param>
        /// <returns></returns>
        public async Task<string> AddServerForward(ServerForwardItemInfo forward)
        {
            var resp = await udpForwardMessengerSender.SignIn(registerStateInfo.Connection, new UdpForwardRegisterParamsInfo
            {
                SourcePort = forward.ServerPort,
                TargetIp = forward.LocalIp,
                TargetPort = forward.LocalPort,
                TargetName = clientConfig.Client.Name,
            }).ConfigureAwait(false);
            if (resp.Code != MessageResponeCodes.OK)
            {
                return resp.Code.GetDesc((byte)resp.Code);
            }

            UdpForwardRegisterResult result = new UdpForwardRegisterResult();
            result.DeBytes(resp.Data);
            if (result.Code != UdpForwardRegisterResultCodes.OK)
            {
                return $"{result.Code.GetDesc((byte)result.Code)},{result.Msg}";
            }

            serverConfigInfo.Tunnels.Add(forward);
            SaveServerConfig();
            return string.Empty;
        }
        /// <summary>
        /// 开启转发
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public async Task<string> StartServerForward(int port)
        {
            ServerForwardItemInfo forwardInfo = serverConfigInfo.Tunnels.FirstOrDefault(c => c.ServerPort == port);
            if (forwardInfo == null)
            {
                return "未找到操作对象";
            }
            var resp = await udpForwardMessengerSender.SignIn(registerStateInfo.Connection, new UdpForwardRegisterParamsInfo
            {
                SourcePort = forwardInfo.ServerPort,
                TargetIp = forwardInfo.LocalIp,
                TargetPort = forwardInfo.LocalPort,
                TargetName = clientConfig.Client.Name,
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
        /// 停止转发
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public async Task<string> StopServerForward(int port)
        {
            ServerForwardItemInfo forwardInfo = serverConfigInfo.Tunnels.FirstOrDefault(c => c.ServerPort == port);
            if (forwardInfo == null)
            {
                return "未找到操作对象";
            }
            var resp = await udpForwardMessengerSender.SignOut(registerStateInfo.Connection, (ushort)port).ConfigureAwait(false);
            if (resp.Code != MessageResponeCodes.OK)
            {
                return resp.Code.GetDesc((byte)resp.Code);
            }
            forwardInfo.Listening = false;
            SaveServerConfig();
            return string.Empty;
        }
        /// <summary>
        /// 删除转发
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public async Task<string> RemoveServerForward(int port)
        {
            ServerForwardItemInfo forwardInfo = serverConfigInfo.Tunnels.FirstOrDefault(c => c.ServerPort == port);
            if (forwardInfo == null)
            {
                return "未找到删除对象";
            }
            var resp = await udpForwardMessengerSender.SignOut(registerStateInfo.Connection, (ushort)port).ConfigureAwait(false);
            if (resp.Code != MessageResponeCodes.OK)
            {
                return resp.Code.GetDesc((byte)resp.Code);
            }
            serverConfigInfo.Tunnels.Remove(forwardInfo);
            SaveServerConfig();

            return string.Empty;
        }

        private ServerConfigInfo ReadServerConfig()
        {
            var config = serverConfigDataProvider.Load().Result;
            return config;
        }
        private void SaveServerConfig()
        {
            serverConfigDataProvider.Save(serverConfigInfo);
        }

        private void RegisterServerForward()
        {
            foreach (var item in serverConfigInfo.Tunnels.Where(c => c.Listening))
            {
                SendRegister(item);
            }
        }
        private void SendRegister(ServerForwardItemInfo item)
        {
            udpForwardMessengerSender.SignIn(registerStateInfo.Connection, new UdpForwardRegisterParamsInfo
            {
                SourcePort = item.ServerPort,
                TargetIp = item.LocalIp,
                TargetPort = item.LocalPort,
                TargetName = clientConfig.Client.Name,
            }).ContinueWith((result) =>
            {
                PrintResult(item, result.Result);
            });
        }
        private void PrintResult(ServerForwardItemInfo item, MessageResponeInfo resp)
        {
            bool success = false;
            StringBuilder sb = new StringBuilder();

            sb.Append($"注册服务器代理Udp转发代理 {item.ServerPort} -> {item.LocalIp}:{item.LocalPort}");
            if (resp.Code != MessageResponeCodes.OK)
            {
                sb.Append($" 【{resp.Code.GetDesc((byte)resp.Code)}】");
            }
            else
            {
                UdpForwardRegisterResult result = new UdpForwardRegisterResult();
                result.DeBytes(resp.Data);
                sb.Append($" 【{result.Code.GetDesc((byte)result.Code)}】{result.Msg}");
                success = result.Code == UdpForwardRegisterResultCodes.OK;
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

    }

    /// <summary>
    /// p2p udp转发配置文件
    /// </summary>
    [Table("p2p-udp-forwards")]
    public class P2PConfigInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public P2PConfigInfo() { }
        /// <summary>
        /// 
        /// </summary>
        public List<P2PListenInfo> Tunnels { get; set; } = new List<P2PListenInfo>();
    }
    /// <summary>
    /// p2p转发监听
    /// </summary>
    public class P2PListenInfo
    {
        /// <summary>
        /// id
        /// </summary>
        public uint ID { get; set; } = 0;
        /// <summary>
        /// 监听端口
        /// </summary>
        public ushort Port { get; set; } = 0;
        /// <summary>
        /// 目标名
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// 监听状态
        /// </summary>
        public bool Listening { get; set; } = false;
        /// <summary>
        /// 目标ip
        /// </summary>
        public string TargetIp { get; set; } = string.Empty;
        /// <summary>
        /// 目标端口
        /// </summary>
        public ushort TargetPort { get; set; } = 0;
        /// <summary>
        /// 简单描述
        /// </summary>
        public string Desc { get; set; } = string.Empty;
    }
    /// <summary>
    /// 服务器 udp转发配置文件
    /// </summary>
    [Table("server-udp-forwards")]
    public class ServerConfigInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public ServerConfigInfo() { }
        /// <summary>
        /// 
        /// </summary>
        public List<ServerForwardItemInfo> Tunnels { get; set; } = new List<ServerForwardItemInfo>();
    }
    /// <summary>
    /// 服务器转发
    /// </summary>
    public class ServerForwardItemInfo
    {
        /// <summary>
        /// 服务器端口
        /// </summary>
        public ushort ServerPort { get; set; }
        /// <summary>
        /// 本地ip
        /// </summary>
        public string LocalIp { get; set; }
        /// <summary>
        /// 本地端口
        /// </summary>
        public ushort LocalPort { get; set; }
        /// <summary>
        /// 简单描述
        /// </summary>
        public string Desc { get; set; } = string.Empty;
        /// <summary>
        /// 转发状态
        /// </summary>
        public bool Listening { get; set; } = false;
    }
}
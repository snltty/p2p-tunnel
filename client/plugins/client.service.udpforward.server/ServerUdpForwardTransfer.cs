using client.messengers.singnin;
using common.libs;
using common.libs.database;
using common.libs.extends;
using common.server;
using common.server.model;
using common.udpforward;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.udpforward.server
{
    /// <summary>
    /// tcp转发中转器和入口
    /// </summary>
    public sealed class ServerUdpForwardTransfer
    {
        public readonly ServerConfigInfo serverConfigInfo;
        private readonly IConfigDataProvider<ServerConfigInfo> serverConfigDataProvider;
        private readonly Config clientConfig;
        private readonly SignInStateInfo signInStateInfo;

        private readonly UdpForwardMessengerSender udpForwardMessengerSender;

        public ServerUdpForwardTransfer(UdpForwardMessengerSender udpForwardMessengerSender,IConfigDataProvider<ServerConfigInfo> serverConfigDataProvider,Config clientConfig,SignInStateInfo signInStateInfo)
        {
            this.udpForwardMessengerSender = udpForwardMessengerSender;
            this.serverConfigDataProvider = serverConfigDataProvider;
            serverConfigInfo = ReadServerConfig();

            this.clientConfig = clientConfig;
            this.signInStateInfo = signInStateInfo;

            signInStateInfo.OnChange.Sub((state) =>
            {
                if (state)
                {
                    RegisterServerForward();
                }
            });
        }

        #region 服务器代理

        /// <summary>
        /// 获取服务器可用端口
        /// </summary>
        /// <returns></returns>
        public async Task<ushort[]> GetServerPorts()
        {
            var resp = await udpForwardMessengerSender.GetPorts(signInStateInfo.Connection);
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
            var resp = await udpForwardMessengerSender.SignIn(signInStateInfo.Connection, new UdpForwardRegisterParamsInfo
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
            var resp = await udpForwardMessengerSender.SignIn(signInStateInfo.Connection, new UdpForwardRegisterParamsInfo
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
            var resp = await udpForwardMessengerSender.SignOut(signInStateInfo.Connection, (ushort)port).ConfigureAwait(false);
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
            var resp = await udpForwardMessengerSender.SignOut(signInStateInfo.Connection, (ushort)port).ConfigureAwait(false);
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
            udpForwardMessengerSender.SignIn(signInStateInfo.Connection, new UdpForwardRegisterParamsInfo
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
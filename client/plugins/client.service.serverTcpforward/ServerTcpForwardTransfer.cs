using client.messengers.singnin;
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

namespace client.service.tcpforward.server
{
    /// <summary>
    /// 服务器tcp转发中转器和入口
    /// </summary>
    public sealed class ServerTcpForwardTransfer
    {
        private readonly ServerForwardConfigInfo serverForwardConfigInfo;
        /// <summary>
        /// 服务器转发列表
        /// </summary>
        public List<ServerForwardItemInfo> serverForwards = new List<ServerForwardItemInfo>();
        private readonly IConfigDataProvider<ServerForwardConfigInfo> serverConfigDataProvider;

        private readonly Config clientConfig;
        private readonly SignInStateInfo signInStateInfo;

        private readonly TcpForwardMessengerSender tcpForwardMessengerSender;
        public ServerTcpForwardTransfer(IConfigDataProvider<ServerForwardConfigInfo> serverConfigDataProvider, Config clientConfig, SignInStateInfo signInStateInfo, TcpForwardMessengerSender tcpForwardMessengerSender)
        {
            this.serverConfigDataProvider = serverConfigDataProvider;

            this.clientConfig = clientConfig;
            this.signInStateInfo = signInStateInfo;

            this.tcpForwardMessengerSender = tcpForwardMessengerSender;

            serverForwardConfigInfo = ReadServerConfig();
            signInStateInfo.OnChange += (state) =>
            {
                if (state)
                {
                    RegisterServerForward();
                }
            };
        }



        #region 服务器代理

        /// <summary>
        /// 获取服务器端口
        /// </summary>
        /// <returns></returns>
        public async Task<ushort[]> GetServerPorts()
        {
            var resp = await tcpForwardMessengerSender.GetPorts(signInStateInfo.Connection);
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
            var resp = await tcpForwardMessengerSender.Register(signInStateInfo.Connection, new TcpForwardRegisterParamsInfo
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
            var resp = await tcpForwardMessengerSender.Register(signInStateInfo.Connection, new TcpForwardRegisterParamsInfo
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
            var resp = await tcpForwardMessengerSender.UnRegister(signInStateInfo.Connection, new TcpForwardUnRegisterParamsInfo
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
            var resp = await tcpForwardMessengerSender.UnRegister(signInStateInfo.Connection, new TcpForwardUnRegisterParamsInfo
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
            tcpForwardMessengerSender.Register(signInStateInfo.Connection, new TcpForwardRegisterParamsInfo
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


    }

    [Table("server-tcp-forwards")]
    public sealed class ServerForwardConfigInfo
    {
        public ServerForwardItemInfo[] Webs { get; set; } = Array.Empty<ServerForwardItemInfo>();
        public ServerForwardItemInfo[] Tunnels { get; set; } = Array.Empty<ServerForwardItemInfo>();
    }

    public sealed class ServerForwardItemInfo
    {
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.Web;
        public string Domain { get; set; }
        public ushort ServerPort { get; set; }
        public string LocalIp { get; set; }
        public ushort LocalPort { get; set; }
        public string Desc { get; set; } = string.Empty;
        public bool Listening { get; set; } = false;
    }
}
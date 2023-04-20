using client.messengers.singnin;
using common.forward;
using common.libs;
using common.libs.database;
using common.libs.extends;
using common.server;
using common.server.model;
using server.service.forward.model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace client.service.forward.server
{
    /// <summary>
    /// 服务器tcp转发中转器和入口
    /// </summary>
    public sealed class ServerForwardTransfer
    {
        private readonly ServerForwardConfigInfo serverForwardConfigInfo;
        /// <summary>
        /// 服务器转发列表
        /// </summary>
        public List<ServerForwardItemInfo> serverForwards = new List<ServerForwardItemInfo>();
        private readonly IConfigDataProvider<ServerForwardConfigInfo> serverConfigDataProvider;

        private readonly Config clientConfig;
        private readonly SignInStateInfo signInStateInfo;

        private readonly ServerForwardMessengerSender tcpForwardMessengerSender;
        public ServerForwardTransfer(IConfigDataProvider<ServerForwardConfigInfo> serverConfigDataProvider, Config clientConfig, SignInStateInfo signInStateInfo, ServerForwardMessengerSender tcpForwardMessengerSender)
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


        public async Task<string[]> GetServerDomains()
        {
            var resp = await tcpForwardMessengerSender.GetDomains(signInStateInfo.Connection);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return resp.Data.GetUTF8String().DeJson<string[]>();
            }

            return Array.Empty<string>();
        }
        public async Task<ushort[]> GetServerPorts()
        {
            var resp = await tcpForwardMessengerSender.GetPorts(signInStateInfo.Connection);
            if (resp.Code == MessageResponeCodes.OK)
            {
                return resp.Data.ToUInt16Array();
            }

            return Array.Empty<ushort>();
        }
        public async Task<string> AddServerForward(ServerForwardItemInfo forward)
        {
            var resp = await tcpForwardMessengerSender.SignIn(signInStateInfo.Connection, new ForwardSignInInfo
            {
                AliveType = forward.AliveType,
                SourceIp = forward.Domain,
                SourcePort = forward.ServerPort,
                TargetIp = forward.LocalIp,
                TargetPort = forward.LocalPort
            }).ConfigureAwait(false);
            if (resp.Code != MessageResponeCodes.OK)
            {
                return resp.Code.GetDesc((byte)resp.Code);
            }

            ForwardSignInResultInfo result = new ForwardSignInResultInfo();
            result.DeBytes(resp.Data);
            if (result.Code != ForwardSignInResultCodes.OK)
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
            if (forward.AliveType == ForwardAliveTypes.Web)
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
            var resp = await tcpForwardMessengerSender.SignIn(signInStateInfo.Connection, new ForwardSignInInfo
            {
                AliveType = forward.AliveType,
                SourceIp = forward.Domain,
                SourcePort = forward.ServerPort,
                TargetIp = forward.LocalIp,
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
       
        public async Task<string> StopServerForward(ServerForwardItemInfo forward)
        {
            ServerForwardItemInfo forwardInfo;
            if (forward.AliveType == ForwardAliveTypes.Web)
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
            var resp = await tcpForwardMessengerSender.SignOut(signInStateInfo.Connection, new ForwardSignOutInfo
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
            if (forward.AliveType == ForwardAliveTypes.Web)
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
            var resp = await tcpForwardMessengerSender.SignOut(signInStateInfo.Connection, new ForwardSignOutInfo
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
                item.AliveType = ForwardAliveTypes.Web;
            }
            foreach (var item in config.Tunnels)
            {
                item.AliveType = ForwardAliveTypes.Tunnel;
            }
            serverForwards = config.Webs.Concat(config.Tunnels).ToList();
            return config;
        }
        private void SaveServerConfig()
        {
            serverForwardConfigInfo.Webs = serverForwards.Where(c => c.AliveType == ForwardAliveTypes.Web).ToArray();
            serverForwardConfigInfo.Tunnels = serverForwards.Where(c => c.AliveType == ForwardAliveTypes.Tunnel).ToArray();
            serverConfigDataProvider.Save(serverForwardConfigInfo);
        }
        private void RegisterServerForward()
        {
            foreach (var item in serverForwardConfigInfo.Webs)
            {
                SendRegister(item, ForwardAliveTypes.Web);
            }
            foreach (var item in serverForwardConfigInfo.Tunnels.Where(c => c.Listening == true))
            {
                SendRegister(item, ForwardAliveTypes.Tunnel);
            }
        }
        private void SendRegister(ServerForwardItemInfo item, ForwardAliveTypes type)
        {
            tcpForwardMessengerSender.SignIn(signInStateInfo.Connection, new ForwardSignInInfo
            {
                AliveType = type,
                SourceIp = item.Domain,
                SourcePort = item.ServerPort,
                TargetIp = item.LocalIp,
                TargetPort = item.LocalPort
            }).ContinueWith((result) =>
            {
                PrintResult(item, result.Result, type);
            });
        }
        private void PrintResult(ServerForwardItemInfo item, MessageResponeInfo resp, ForwardAliveTypes type)
        {
            bool success = false;
            StringBuilder sb = new StringBuilder();

            sb.Append($"注册服务器代理转发【{type.GetDesc((byte)type)}】代理 {item.Domain}:{item.ServerPort} -> {item.LocalIp}:{item.LocalPort}");
            if (resp.Code != MessageResponeCodes.OK)
            {
                sb.Append($" 【{resp.Code.GetDesc((byte)resp.Code)}】");
            }
            else
            {
                ForwardSignInResultInfo result = new ForwardSignInResultInfo();
                result.DeBytes(resp.Data);
                sb.Append($" 【{result.Code.GetDesc((byte)result.Code)},{result.Msg}】{result.Msg}");
                success = result.Code == ForwardSignInResultCodes.OK;
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

    }

    [Table("server-forwards")]
    public sealed class ServerForwardConfigInfo
    {
        public ServerForwardItemInfo[] Webs { get; set; } = Array.Empty<ServerForwardItemInfo>();
        public ServerForwardItemInfo[] Tunnels { get; set; } = Array.Empty<ServerForwardItemInfo>();
    }

    public sealed class ServerForwardItemInfo
    {
        public ForwardAliveTypes AliveType { get; set; } = ForwardAliveTypes.Web;
        public string Domain { get; set; }
        public ushort ServerPort { get; set; }
        public IPAddress LocalIp { get; set; }
        public ushort LocalPort { get; set; }
        public string Desc { get; set; } = string.Empty;
        public bool Listening { get; set; } = false;
    }
}
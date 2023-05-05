using client.service.ui.api.clientServer;
using common.libs.extends;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace client.service.forward.server
{
    /// <summary>
    /// 服务器转发
    /// </summary>
    public sealed class ServerForwardClientService : IClientService
    {
        private readonly ServerForwardTransfer  serverForwardTransfer;
        public ServerForwardClientService(ServerForwardTransfer serverForwardTransfer)
        {
            this.serverForwardTransfer = serverForwardTransfer;
        }
       
        /// <summary>
        /// 服务器转发
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public List<ServerForwardItemInfo> List(ClientServiceParamsInfo arg)
        {
            return serverForwardTransfer.serverForwards;
        }

        /// <summary>
        /// 获取域名列表
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<string[]> Domains(ClientServiceParamsInfo arg)
        {
            return await serverForwardTransfer.GetServerDomains();
        }

        /// <summary>
        /// 服务器端口
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<ushort[]> Ports(ClientServiceParamsInfo arg)
        {
            return await serverForwardTransfer.GetServerPorts();
        }
        /// <summary>
        /// 服务器转发添加
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<bool> Add(ClientServiceParamsInfo arg)
        {
            ServerForwardItemInfo forward = arg.Content.DeJson<ServerForwardItemInfo>();
            string res = await serverForwardTransfer.AddServerForward(forward);
            if (string.IsNullOrWhiteSpace(res) == false)
            {
                arg.SetErrorMessage(res);
            }
            return true;
        }
        /// <summary>
        /// 服务器转发开启
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<bool> Start(ClientServiceParamsInfo arg)
        {
            ServerForwardItemInfo forward = arg.Content.DeJson<ServerForwardItemInfo>();
            string res = await serverForwardTransfer.StartServerForward(forward);
            if (string.IsNullOrWhiteSpace(res) == false)
            {
                arg.SetErrorMessage(res);
            }
            return true;
        }
        /// <summary>
        /// 服务器转发停止
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<bool> Stop(ClientServiceParamsInfo arg)
        {
            ServerForwardItemInfo forward = arg.Content.DeJson<ServerForwardItemInfo>();
            string res = await serverForwardTransfer.StopServerForward(forward);
            if (string.IsNullOrWhiteSpace(res) == false)
            {
                arg.SetErrorMessage(res);
            }
            return true;
        }
        /// <summary>
        /// 服务器转发删除
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<bool> Remove(ClientServiceParamsInfo arg)
        {
            ServerForwardItemInfo forward = arg.Content.DeJson<ServerForwardItemInfo>();
            string res = await serverForwardTransfer.RemoveServerForward(forward);
            if (string.IsNullOrWhiteSpace(res) == false)
            {
                arg.SetErrorMessage(res);
            }
            return true;
        }
    }
}

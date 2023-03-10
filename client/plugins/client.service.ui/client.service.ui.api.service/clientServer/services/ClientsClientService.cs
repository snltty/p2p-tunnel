using client.messengers.clients;
using client.messengers.register;
using client.service.ui.api.clientServer;
using common.libs.extends;
using common.server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace client.service.ui.api.service.clientServer.services
{
    /// <summary>
    /// 客户端列表
    /// </summary>
    public sealed class ClientsClientService : IClientService
    {
        private readonly IClientsTransfer clientsTransfer;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly RegisterStateInfo registerStateInfo;
        private readonly IClientServer clientServer;

        public ClientsClientService(IClientsTransfer clientsTransfer, IClientInfoCaching clientInfoCaching, RegisterStateInfo registerStateInfo, IClientServer clientServer)
        {
            this.clientsTransfer = clientsTransfer;
            this.clientInfoCaching = clientInfoCaching;
            this.registerStateInfo = registerStateInfo;
            this.clientServer = clientServer;
        }

        /// <summary>
        /// 客户端列表
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public IEnumerable<ClientInfo> List(ClientServiceParamsInfo arg)
        {
            return clientInfoCaching.All();
        }

        /// <summary>
        /// 连它
        /// </summary>
        /// <param name="arg"></param>
        public bool Connect(ClientServiceParamsInfo arg)
        {
            ulong id = ulong.Parse(arg.Content);
            if (clientInfoCaching.Get(id, out ClientInfo client) == false)
            {
                return false;
            }
            //clientInfoCaching.Offline(id,5);
            clientsTransfer.ConnectClient(client);
            return true;
        }
        /// <summary>
        /// 连我
        /// </summary>
        /// <param name="arg"></param>
        public bool ConnectReverse(ClientServiceParamsInfo arg)
        {
            ulong id = ulong.Parse(arg.Content);
            if (clientInfoCaching.Get(id, out ClientInfo client) == false)
            {
                return false;
            }
            //clientInfoCaching.Offline(id,6);
            clientsTransfer.ConnectReverse(client);
            return true;
        }

        /// <summary>
        /// 重启
        /// </summary>
        /// <param name="arg"></param>
        public bool Reset(ClientServiceParamsInfo arg)
        {
            clientsTransfer.Reset(ulong.Parse(arg.Content));
            return true;
        }
        /// <summary>
        /// 断开
        /// </summary>
        /// <param name="arg"></param>
        public async Task<bool> Offline(ClientServiceParamsInfo arg)
        {
            if (clientInfoCaching.Get(ulong.Parse(arg.Content), out ClientInfo client))
            {
                if (client.OnlineType == ClientOnlineTypes.Active)
                {
                    clientInfoCaching.Offline(client.Id);
                }
                else
                {
                    await clientsTransfer.SendOffline(client.Id);
                }
            }

            return true;
        }

        /// <summary>
        /// ping
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<bool> Ping(ClientServiceParamsInfo arg)
        {
            await clientsTransfer.Ping();
            return true;
        }


        private bool running = false;
        /// <summary>
        /// 速度测试
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public void Test(ClientServiceParamsInfo arg)
        {
            SpeedModel model = arg.Content.DeJson<SpeedModel>();
            if (model.Id == 0)
            {
                running = false;
                return;
            }
            if (running)
            {
                return;
            }

            int current = 0;
            int prev = 0;
            var bytes = new byte[model.Packet * 1024];
            running = true;

            _ = Task.Run(async () =>
            {
                while (running)
                {
                    clientServer.Notify(new ClientServiceResponseInfo
                    {
                        Code = ClientServiceResponseCodes.Success,
                        Path = "speed_test",
                        RequestId = 0,
                        Content = new
                        {
                            current = current,
                            prev = prev
                        }
                    });
                    prev = current;
                    await Task.Delay(1000);

                }
            });

            _ = Task.Run(async () =>
            {
                while (running)
                {
                    bool res = await clientsTransfer.Test(model.Id, bytes);
                    current += bytes.Length;
                    if (res == false)
                    {
                        running = res;
                        break;
                    }
                }
            });
        }

        class SpeedModel
        {
            public ulong Id { get; set; }
            public int Packet { get; set; }
        }

        /// <summary>
        /// 获取所有客户端的连接情况
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<ConcurrentDictionary<ulong, ulong[]>> Connects(ClientServiceParamsInfo arg)
        {
            return await clientsTransfer.Connects();
        }
        /// <summary>
        /// 获取可用于中继的线路的延迟
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<int[]> Delay(ClientServiceParamsInfo arg)
        {
            return await clientsTransfer.Delay(arg.Content.DeJson<ulong[][]>());
        }
        /// <summary>
        /// 中继
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task<bool> Relay(ClientServiceParamsInfo arg)
        {
            ulong[] relayids = arg.Content.DeJson<ulong[]>();
            if (relayids.Length < 3) return false;

            ulong connectId = relayids[1];
            ulong targetId = relayids[^1];

            IConnection sourceConnection = null;
            if (connectId == 0)
            {
                sourceConnection = registerStateInfo.Connection;
            }
            else if (clientInfoCaching.Get(connectId, out ClientInfo sourceClient))
            {
                sourceConnection = sourceClient.Connection;
            }
            if (sourceConnection == null)
            {
                return false;
            }

            //clientInfoCaching.Offline(targetId,8);
            await clientsTransfer.Relay(sourceConnection, relayids, true);

            return true;
        }

    }
}

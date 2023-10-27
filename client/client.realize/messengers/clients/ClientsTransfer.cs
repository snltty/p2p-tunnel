using client.messengers.clients;
using client.messengers.punchHole;
using client.messengers.punchHole.tcp;
using client.messengers.punchHole.udp;
using client.messengers.signin;
using client.messengers.relay;
using client.realize.messengers.crypto;
using client.realize.messengers.heart;
using client.realize.messengers.punchHole;
using client.realize.messengers.relay;
using common.libs;
using common.server;
using common.server.model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace client.realize.messengers.clients
{
    /// <summary>
    /// 客户端操作类
    /// </summary>
    public sealed class ClientsTransfer : IClientsTransfer
    {
        private readonly ClientsMessengerSender clientsMessengerSender;
        private readonly IPunchHoleUdp punchHoleUdp;
        private readonly IPunchHoleTcp punchHoleTcp;
        private readonly SignInStateInfo signInState;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly PunchHoleMessengerSender punchHoleMessengerSender;
        private readonly Config config;
        private readonly HeartMessengerSender heartMessengerSender;
        private readonly RelayMessengerSender relayMessengerSender;
        private readonly IClientConnectsCaching connecRouteCaching;
        private readonly PunchHoleDirectionConfig punchHoleDirectionConfig;

        private readonly CryptoSwap cryptoSwap;


        public ClientsTransfer(ClientsMessengerSender clientsMessengerSender,
            IPunchHoleUdp punchHoleUdp, IPunchHoleTcp punchHoleTcp, IClientInfoCaching clientInfoCaching,
            SignInStateInfo signInState, PunchHoleMessengerSender punchHoleMessengerSender, Config config,
           HeartMessengerSender heartMessengerSender,
            RelayMessengerSender relayMessengerSender, IClientConnectsCaching connecRouteCaching,
            PunchHoleDirectionConfig punchHoleDirectionConfig, CryptoSwap cryptoSwap
        )
        {
            this.clientsMessengerSender = clientsMessengerSender;
            this.punchHoleUdp = punchHoleUdp;
            this.punchHoleTcp = punchHoleTcp;
            this.signInState = signInState;
            this.clientInfoCaching = clientInfoCaching;
            this.config = config;
            this.heartMessengerSender = heartMessengerSender;
            this.relayMessengerSender = relayMessengerSender;
            this.connecRouteCaching = connecRouteCaching;
            this.punchHoleMessengerSender = punchHoleMessengerSender;
            this.punchHoleDirectionConfig = punchHoleDirectionConfig;
            this.cryptoSwap = cryptoSwap;

            punchHoleUdp.OnStepHandler += OnPunchHoleStep;
            punchHoleTcp.OnStepHandler += OnPunchHoleStep;

            //中继连线
            relayMessengerSender.OnRelay += (param) =>
            {
                _ = Relay(param.Connection, param.RelayIds, false);
            };

            //有人要求反向链接
            punchHoleMessengerSender.OnReverse += OnReverse;

            signInState.OnBind += OnBind;

            //收到来自服务器的 在线客户端 数据
            clientsMessengerSender.OnServerClientsData += OnServerSendClients;

            ClientTask();
        }

        private void OnPunchHoleStep(object sender, PunchHoleStepModel arg)
        {
            byte step1 = 0, step2fail = 0, step3 = 0, step4 = 0;
            if (arg.Connection.ServerType == ServerType.TCP)
            {
                if (config.Client.UseTcp == false) return;
                step1 = (byte)PunchHoleTcpNutssBSteps.STEP_1;
                step2fail = (byte)PunchHoleTcpNutssBSteps.STEP_2_FAIL;
                step3 = (byte)PunchHoleTcpNutssBSteps.STEP_3;
                step4 = (byte)PunchHoleTcpNutssBSteps.STEP_4;
            }
            else if (arg.Connection.ServerType == ServerType.UDP)
            {
                if (config.Client.UseUdp == false) return;
                step1 = (byte)PunchHoleUdpSteps.STEP_1;
                step2fail = (byte)PunchHoleUdpSteps.STEP_2_Fail;
                step3 = (byte)PunchHoleUdpSteps.STEP_3;
                step4 = (byte)PunchHoleUdpSteps.STEP_4;
            }

            //3 4步骤是已成功连接，设置上线状态
            if (arg.RawData.PunchStep == step3 || arg.RawData.PunchStep == step4)
            {
                if (clientInfoCaching.Get(arg.RawData.FromId, out ClientInfo client))
                {
                    //3是被动方 4是主动方
                    clientInfoCaching.Online(arg.RawData.FromId, arg.Connection, ClientConnectTypes.P2P);
                    _ = clientsMessengerSender.RemoveTunnel(signInState.Connection, arg.RawData.FromId);
                    //主动方 记录一下，是我这边主动打洞成功的，下次由我这边开始尝试
                    if (arg.RawData.PunchStep == step4)
                    {
                        punchHoleDirectionConfig.Add(client.Name);
                    }
                }
            }
            //被动方收到打洞消息，设置状态位连接中
            else if (arg.RawData.PunchStep == step1)
            {
                clientInfoCaching.SetConnecting(arg.RawData.FromId, true);
            }
            //打洞失败
            else if (arg.RawData.PunchStep == step2fail)
            {
                clientInfoCaching.Offline(arg.RawData.FromId, ClientOfflineTypes.Manual);
            }
        }

        private void ClientTask()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (config.Client.UsePunchHole)
                    {
                        foreach (ClientInfo client in clientInfoCaching.All())
                        {
                            bool alive = await heartMessengerSender.Alive(client.Connection);
                            if (alive == false && signInState.Connected && client.GetConnect())
                            {
                                clientInfoCaching.Offline(client.ConnectionId, ClientOfflineTypes.Disconnect);
                                if (config.Client.UsePunchHole && ((EnumClientAccess.UsePunchHole & (EnumClientAccess)client.ClientAccess) == EnumClientAccess.UsePunchHole))
                                {
                                    //主动打洞成功过
                                    if (punchHoleDirectionConfig.Contains(client.Name))
                                    {
                                        ConnectClient(client);
                                    }
                                    //否则让对方主动
                                    else
                                    {
                                        ConnectReverse(client);
                                    }
                                }
                                else if (config.Client.AutoRelay && ((EnumClientAccess.UseAutoRelay & (EnumClientAccess)client.ClientAccess) == EnumClientAccess.UseAutoRelay))
                                {
                                    _ = Relay(client, true);
                                }
                            }
                        }
                    }
                    await Task.Delay(10000);
                }

            }, TaskCreationOptions.LongRunning);
        }

        public async Task Offline(ulong toid)
        {
            if (clientInfoCaching.Get(toid, out ClientInfo client))
            {
                clientInfoCaching.Offline(client.ConnectionId, ClientOfflineTypes.Manual);
                await SendOffline(client.ConnectionId);
            }
        }

        public async Task SendOffline(ulong toid)
        {
            await punchHoleMessengerSender.SendOffline(toid);
        }
        /// <summary>
        /// 连它
        /// </summary>
        /// <param name="id"></param>
        public void ConnectClient(ulong id)
        {
            if (clientInfoCaching.Get(id, out ClientInfo client))
            {
                ConnectClient(client);
            }
        }
        /// <summary>
        /// 连它
        /// </summary>
        /// <param name="client"></param>
        public void ConnectClient(ClientInfo client)
        {
            if (client.ConnectionId == signInState.ConnectId)
            {
                Logger.Instance.Error($"canot connect you self");
                return;
            }
            if (signInState.LocalInfo.IsConnecting)
            {
                return;
            }

            Task.Run(async () =>
            {
                EnumConnectResult result = EnumConnectResult.Fail;
                //tcp没试过，先试试tcp
                if ((client.TryReverseValue & ClientInfo.TryReverseTcpBit) == ClientInfo.TryReverseDefault)
                {
                    client.TryReverseValue |= ClientInfo.TryReverseTcpBit;
                    result = await ConnectTcp(client).ConfigureAwait(false);
                }
                //udp没试过，试试udp
                else if ((client.TryReverseValue & ClientInfo.TryReverseUdpBit) == ClientInfo.TryReverseDefault)
                {
                    client.TryReverseValue |= ClientInfo.TryReverseUdpBit;
                    result = await ConnectUdp(client).ConfigureAwait(false);
                }

                //没成功
                if (result == EnumConnectResult.Fail)
                {
                    //对面有没试过的，让对面试试
                    if ((client.TryReverseValue >> 2 & ClientInfo.TryReverseTcpUdpBit) != ClientInfo.TryReverseTcpUdpBit)
                    {
                        ConnectReverse(client);
                    }
                    //都试过了， 都不行，中继 
                    else if (config.Client.AutoRelay && ((EnumClientAccess.UseAutoRelay & (EnumClientAccess)client.ClientAccess) == EnumClientAccess.UseAutoRelay))
                    {
                        _ = Relay(client, true);
                    }
                }
                else if (result == EnumConnectResult.BreakOff)
                {
                    Logger.Instance.Error($"打洞被跳过，最大的可能是，【{client.Name}】的打洞失败消息比本消息“反向连接”来的晚，可以重新手动尝试");
                }
                client.TryTimes++;
                client.TryReverseValue = ClientInfo.TryReverseDefault;
            });
        }
        //收到反连接请求
        private void OnReverse(PunchHoleRequestInfo info)
        {
            if (clientInfoCaching.Get(info.FromId, out ClientInfo client))
            {
                PunchHoleReverseInfo model = new PunchHoleReverseInfo();
                model.DeBytes(info.Data);
                //交换状态 , 11 01 -> 01 11
                client.TryReverseValue = (byte)(((model.Value & ClientInfo.TryReverseTcpUdpBit) << 2) | (model.Value >> 2));
                ConnectClient(client);
            }
            else
            {
                Logger.Instance.Error($"收到反向连接，但是这个客户端不存在，可能是因为对面比此客户端更早收到客户端列表数据");
            }
        }

        /// <summary>
        /// 连我
        /// </summary>
        /// <param name="id"></param>
        public void ConnectReverse(ulong id)
        {
            if (clientInfoCaching.Get(id, out ClientInfo client))
            {
                ConnectReverse(client);
            }
        }
        /// <summary>
        /// 连我
        /// </summary>
        /// <param name="info"></param>
        public void ConnectReverse(ClientInfo client)
        {
            punchHoleMessengerSender.SendReverse(client).ConfigureAwait(false);
        }

        /// <summary>
        /// 重启
        /// </summary>
        /// <param name="id"></param>
        public void Reset(ulong id)
        {
            punchHoleMessengerSender.SendReset(id).ConfigureAwait(false);

        }

        private async Task<EnumConnectResult> ConnectUdp(ClientInfo client)
        {
            if (client.Connecting)
            {
                return EnumConnectResult.BreakOff;
            }
            if ((config.Client.UseUdp & ((EnumClientAccess.UseUdp & (EnumClientAccess)client.ClientAccess) == EnumClientAccess.UseUdp)) == false)
            {
                return EnumConnectResult.Fail;
            }

            client.SetConnecting(true);

            for (int i = 0; i < 2; i++)
            {
                ConnectResultModel result = await punchHoleUdp.Send(new ConnectParams
                {
                    Id = client.ConnectionId,
                    NewTunnel = 1,
                    LocalPort = 0
                }).ConfigureAwait(false);
                if (result.State)
                {
                    return EnumConnectResult.Success;
                }
                Logger.Instance.Error((result.Result as ConnectFailModel).Msg);
            }

            client.SetConnecting(false);
            return EnumConnectResult.Fail;
        }
        private async Task<EnumConnectResult> ConnectTcp(ClientInfo client)
        {
            if (client.Connecting)
            {
                return EnumConnectResult.BreakOff;
            }
            if ((config.Client.UseTcp & ((EnumClientAccess.UseTcp & (EnumClientAccess)client.ClientAccess) == EnumClientAccess.UseTcp)) == false)
            {
                return EnumConnectResult.Fail;
            }

            client.SetConnecting(true);

            byte[] tunnelNames = new byte[] { 1, 1 };
            for (int i = 0; i < tunnelNames.Length; i++)
            {
                clientInfoCaching.AddTunnelPort(client.ConnectionId, signInState.LocalInfo.Port);
                ConnectResultModel result = await punchHoleTcp.Send(new ConnectParams
                {
                    Id = client.ConnectionId,
                    NewTunnel = tunnelNames[i],
                    LocalPort = signInState.LocalInfo.Port
                }).ConfigureAwait(false);
                if (result.State)
                {
                    return EnumConnectResult.Success;
                }
                Logger.Instance.Error((result.Result as ConnectFailModel).Msg);
            }

            client.SetConnecting(false);
            return EnumConnectResult.Fail;
        }

        /// <summary>
        /// ping
        /// </summary>
        /// <returns></returns>
        public async Task Ping()
        {
            foreach (var item in clientInfoCaching.All())
            {
                try
                {
                    var start = DateTime.Now;
                    var res = await heartMessengerSender.Alive(item.Connection);
                    if (res)
                    {
                        item.Connection.RoundTripTime = (ushort)(DateTime.Now - start).TotalMilliseconds;
                    }
                    else
                    {
                        item.Connection.RoundTripTime = -1;
                    }
                }
                catch (Exception)
                {
                }
            }
        }
        public async Task<bool> Test(ulong id, Memory<byte> data)
        {
            if (clientInfoCaching.Get(id, out ClientInfo client))
            {
                return await heartMessengerSender.Test(client.Connection, data);
            }
            return false;
        }

        private void OnBind(bool state)
        {
            clientInfoCaching.Clear();
        }

        private void OnServerSendClients(ClientsInfo clients)
        {
            try
            {
                if (signInState.Connection == null || clients.Clients == null)
                {
                    return;
                }
                IEnumerable<ulong> remoteIds = clients.Clients.Select(c => c.ConnectionId);
                //下线了的
                IEnumerable<ulong> offlines = clientInfoCaching.All().Where(c => c.Connected == false).Select(c => c.ConnectionId).Except(remoteIds).Where(c => c != signInState.ConnectId);
                foreach (ulong offid in offlines)
                {
                    clientInfoCaching.Remove(offid);
                }

                //新上线的或者更新的
                foreach (ClientsClientInfo item in clients.Clients.Where(c => c.ConnectionId != signInState.ConnectId))
                {
                    EnumClientAccess enumClientAccess = (EnumClientAccess)item.ClientAccess;
                    bool has = clientInfoCaching.Get(item.ConnectionId, out ClientInfo client);
                    if (has == false)
                    {
                        client = new ClientInfo();
                        client.ConnectionId = item.ConnectionId;
                    }

                    client.ClientAccess = item.ClientAccess;
                    client.Name = item.Name;
                    clientInfoCaching.Add(client);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }

        /// <summary>
        /// 获取各个客户端的连接状态
        /// </summary>
        /// <returns></returns>
        public async Task<ConcurrentDictionary<ulong, ulong[]>> Connects()
        {
            await relayMessengerSender.AskConnects();
            return connecRouteCaching.Connects;
        }
        /// <summary>
        /// 各个线路的延迟
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public async Task<int[]> Delay(ulong[][] paths)
        {
            int[] data = new int[paths.Length];
            DateTime current;
            for (int i = 0; i < paths.Length; i++)
            {
                ulong[] item = paths[i];

                data[i] = -1;

                IConnection connection = null;
                if (item[1] == 0)
                {
                    connection = signInState.Connection;
                }
                else
                {
                    if (clientInfoCaching.Get(item[1], out ClientInfo client))
                    {
                        connection = client.Connection;
                    }
                }
                if (connection == null || connection.Connected == false)
                {
                    continue;
                }

                current = DateTime.Now;
                bool res = await relayMessengerSender.Delay(item, connection);
                if (res)
                {
                    data[i] = (int)(DateTime.Now - current).TotalMilliseconds;
                }
            }

            return data;
        }
        private async Task Relay(ClientInfo client, bool notify = false)
        {
            if ((signInState.RemoteInfo.Access & 1) != 1)
            {
                Logger.Instance.Warning($"server relay not available");
                return;
            }

            IConnection connection = signInState.Connection;
            if (client.Connected == true)
            {
                return;
            }
            await Relay(connection, new ulong[] { signInState.ConnectId, 0, client.ConnectionId }, notify);
        }
        /// <summary>
        /// 中继
        /// </summary>
        /// <param name="sourceConnection"></param>
        /// <param name="relayids"></param>
        /// <param name="notify"></param>
        /// <returns></returns>
        public async Task Relay(IConnection sourceConnection, Memory<ulong> relayids, bool notify = false)
        {
            if (relayids.Length < 3)
            {
                Logger.Instance.Error($"relayids length least 3");
                return;
            };
            if (sourceConnection == null || sourceConnection.Connected == false)
            {
                Logger.Instance.Error($"sourceConnection is null");
                return;
            }

            IConnection connection = sourceConnection.Clone();
            connection.Relay = true;
            connection.RelayId = relayids;

            if (notify)
            {
                bool relayResult = await relayMessengerSender.Relay(relayids, connection);
                if (relayResult == false)
                {
                    Logger.Instance.Error($"relay fail");
                    return;
                }
                if (config.Client.Encode)
                {
                    ICrypto crypto = await cryptoSwap.Swap(connection, config.Client.EncodePassword);
                    if (crypto == null)
                    {
                        Logger.Instance.Error("交换密钥失败，如果客户端设置了密钥，则目标端必须设置相同的密钥，如果目标端未设置密钥，则客户端必须留空");
                        return;
                    }
                    connection.EncodeEnable(crypto);
                }
            }

            ClientConnectTypes relayType = relayids.Span[1] == 0 ? ClientConnectTypes.RelayServer : ClientConnectTypes.RelayNode;
            clientInfoCaching.Online(relayids.Span[^1], connection, relayType);
        }

        /// <summary>
        /// 打洞结果
        /// </summary>
        [Flags]
        enum EnumConnectResult : byte
        {
            /// <summary>
            /// 失败
            /// </summary>
            Fail = 1,
            /// <summary>
            /// 成功
            /// </summary>
            Success = 2,
            /// <summary>
            /// 跳过
            /// </summary>
            BreakOff = 4
        }
    }

}

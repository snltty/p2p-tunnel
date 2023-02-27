using client.messengers.clients;
using client.messengers.punchHole;
using client.messengers.punchHole.tcp;
using client.messengers.punchHole.udp;
using client.messengers.register;
using client.messengers.relay;
using client.realize.messengers.crypto;
using client.realize.messengers.heart;
using client.realize.messengers.punchHole;
using client.realize.messengers.relay;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using common.server.servers.rudp;
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
        private BoolSpace firstClients = new BoolSpace(true);

        private readonly ClientsMessengerSender clientsMessengerSender;
        private readonly IPunchHoleUdp punchHoleUdp;
        private readonly IPunchHoleTcp punchHoleTcp;
        private readonly RegisterStateInfo registerState;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly PunchHoleMessengerSender punchHoleMessengerSender;
        private readonly Config config;
        private readonly IUdpServer udpServer;
        private readonly HeartMessengerSender heartMessengerSender;
        private readonly RelayMessengerSender relayMessengerSender;
        private readonly IClientConnectsCaching connecRouteCaching;
        private readonly PunchHoleDirectionConfig punchHoleDirectionConfig;
        private readonly CryptoSwap cryptoSwap;

        private object lockObject = new();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientsMessengerSender"></param>
        /// <param name="punchHoleUdp"></param>
        /// <param name="punchHoleTcp"></param>
        /// <param name="clientInfoCaching"></param>
        /// <param name="registerState"></param>
        /// <param name="punchHoleMessengerSender"></param>
        /// <param name="config"></param>
        /// <param name="udpServer"></param>
        /// <param name="tcpServer"></param>
        /// <param name="heartMessengerSender"></param>
        /// <param name="relayMessengerSender"></param>
        /// <param name="clientsTunnel"></param>
        /// <param name="connecRouteCaching"></param>
        /// <param name="punchHoleDirectionConfig"></param>
        public ClientsTransfer(ClientsMessengerSender clientsMessengerSender,
            IPunchHoleUdp punchHoleUdp, IPunchHoleTcp punchHoleTcp, IClientInfoCaching clientInfoCaching,
            RegisterStateInfo registerState, PunchHoleMessengerSender punchHoleMessengerSender, Config config,
            IUdpServer udpServer, ITcpServer tcpServer, HeartMessengerSender heartMessengerSender,
            RelayMessengerSender relayMessengerSender, IClientsTunnel clientsTunnel, IClientConnectsCaching connecRouteCaching,
            PunchHoleDirectionConfig punchHoleDirectionConfig, CryptoSwap cryptoSwap
        )
        {
            this.clientsMessengerSender = clientsMessengerSender;
            this.punchHoleUdp = punchHoleUdp;
            this.punchHoleTcp = punchHoleTcp;
            this.registerState = registerState;
            this.clientInfoCaching = clientInfoCaching;
            this.config = config;
            this.udpServer = udpServer;
            this.heartMessengerSender = heartMessengerSender;
            this.relayMessengerSender = relayMessengerSender;
            this.connecRouteCaching = connecRouteCaching;
            this.punchHoleMessengerSender = punchHoleMessengerSender;
            this.punchHoleDirectionConfig = punchHoleDirectionConfig;
            this.cryptoSwap = cryptoSwap;

            punchHoleUdp.OnStepHandler += OnPunchHoleStep;
            punchHoleTcp.OnStepHandler += OnPunchHoleStep;

            //掉线的
            tcpServer.OnDisconnect.Sub((connection) => OnDisconnect(connection, registerState.TcpConnection));
            udpServer.OnDisconnect.Sub((connection) => OnDisconnect(connection, registerState.UdpConnection));
            clientsTunnel.OnDisConnect = OnDisconnect;
            clientInfoCaching.OnOffline.Sub(OnOffline);
            clientInfoCaching.OnOfflineAfter.Sub(OnOfflineAfter);

            //中继连线
            relayMessengerSender.OnRelay.Sub((param) =>
            {
                _ = Relay(param.Connection, param.RelayIds, false);
            });

            //有人要求反向链接
            punchHoleMessengerSender.OnReverse.Sub(OnReverse);

            registerState.OnRegisterBind.Sub(OnRegisterBind);

            //收到来自服务器的 在线客户端 数据
            clientsMessengerSender.OnServerClientsData.Sub(OnServerSendClients);

            Logger.Instance.Info("获取外网距离ing...");
            registerState.LocalInfo.RouteLevel = NetworkHelper.GetRouteLevel();
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
                    ClientOnlineTypes onlineType = arg.RawData.PunchStep == step3 ? ClientOnlineTypes.Passive : ClientOnlineTypes.Active;
                    clientInfoCaching.Online(arg.RawData.FromId, arg.Connection, ClientConnectTypes.P2P, onlineType, arg.RawData.TunnelName);
                    if (arg.RawData.TunnelName > (ulong)TunnelDefaults.MAX)
                    {
                        clientInfoCaching.RemoveTunnelPort(arg.RawData.TunnelName);
                        _ = clientsMessengerSender.RemoveTunnel(registerState.OnlineConnection, arg.RawData.TunnelName);
                    }
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
                if (clientInfoCaching.Get(arg.RawData.FromId, out ClientInfo client))
                {
                    client.SetConnecting(false);
                    if (arg.RawData.TunnelName > (ulong)TunnelDefaults.MAX)
                    {
                        clientInfoCaching.RemoveTunnelPort(arg.RawData.TunnelName);
                        clientInfoCaching.RemoveUdpserver(arg.RawData.TunnelName, true);
                        _ = clientsMessengerSender.RemoveTunnel(registerState.OnlineConnection, arg.RawData.TunnelName);
                    }
                }
            }
        }

        private void OnOffline(ClientInfo client)
        {
            if (clientInfoCaching.Get(client.Id, out _))
            {
                registerState.LocalInfo.IsConnecting = true;
                client.SetConnecting(true);
                punchHoleMessengerSender.SendOffline(client.Id).Wait();
                client.SetConnecting(false);
                registerState.LocalInfo.IsConnecting = false;
            }

        }
        private void OnOfflineAfter(ClientInfo client)
        {
            if (clientInfoCaching.Get(client.Id, out _))
            {
                //主动连接的，未知掉线信息的，去尝试重连一下
                if (config.Client.UseReConnect && client.OnlineType == ClientOnlineTypes.Active && client.OfflineType == ClientOfflineTypes.Disconnect)
                {
                    ConnectClient(client);
                }
            }
        }
        private void OnDisconnect(IConnection connection, IConnection regConnection)
        {
            if (ReferenceEquals(regConnection, connection))
            {
                return;
            }

            Logger.Instance.Error($"{connection.ServerType} client 断开~~~~${connection.Address}");
            if (clientInfoCaching.Get(connection.ConnectId, out ClientInfo client))
            {
                if (ReferenceEquals(connection, client.Connection))
                {
                    clientInfoCaching.Offline(connection.ConnectId, ClientOfflineTypes.Disconnect);
                }
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
            if (client.Id == registerState.ConnectId)
            {
                Logger.Instance.Error($"canot connect you self");
                return;
            }
            if (registerState.LocalInfo.IsConnecting)
            {
                return;
            }

            Task.Run(async () =>
            {
                /* 两边先试TCP，没成功，再两边都试试UDP
                 *  
                 * TryReverseTcpBit     0b00000010
                 * TryReverseUdpBit     0b00000001
                 * TryReverseTcpUdpBit  0b00000011
                 * TryReverseDefault    0b00000000
                 * 1、A 00 00 -> 00 10 -> B
                 *      A 试了tcp没成功，让B试试
                 * 2、B 00 10 -> 10 00 -> 10 10 -> A
                 *      B 收到反连接请求，先交换保存状态，试试TCP，没成功，继续给A试试，
                 * 3、A 10 10 -> 10 10 -> 10 11 -> B
                 *      A 收到反连接请求，先交换保存状态，前面试过TCP了，试试UDP，没成功，继续让B试试
                 * 4、B 10 11 -> 11 10 -> 11 11
                 *      B 收到反链接请求，先交换保存状态，前面试过TCP了，试试UDP，成就成，没成就全部结束
                 */
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
                    else
                    {
                        _ = Relay(client, true);
                    }
                }
                else if (result == EnumConnectResult.BreakOff)
                {
                    Logger.Instance.Error($"打洞被跳过，最大的可能是，【{client.Name}】的打洞失败消息比本消息“反向连接”来的晚，可以重新手动尝试");
                }
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
        public void ConnectReverse(ClientInfo info)
        {
            punchHoleMessengerSender.SendReverse(info).ConfigureAwait(false);
        }

        /// <summary>
        /// 重启
        /// </summary>
        /// <param name="id"></param>
        public void Reset(ulong id)
        {
            punchHoleMessengerSender.SendReset(id).ConfigureAwait(false);

        }
        /// <summary>
        /// 停止连接
        /// </summary>
        /// <param name="id"></param>
        public void ConnectStop(ulong id)
        {
        }

        private async Task<EnumConnectResult> ConnectUdp(ClientInfo client)
        {
            if (client.Connecting)
            {
                return EnumConnectResult.BreakOff;
            }
            if ((config.Client.UseUdp & client.UseUdp) == false)
            {
                return EnumConnectResult.Fail;
            }

            client.SetConnecting(true);

            ulong[] tunnelNames = new ulong[] { (ulong)TunnelDefaults.MIN, (ulong)TunnelDefaults.MIN };

            for (int i = 0; i < tunnelNames.Length; i++)
            {
                ConnectResultModel result = await punchHoleUdp.Send(new ConnectParams
                {
                    Id = client.Id,
                    TunnelName = tunnelNames[i],
                    LocalPort = registerState.LocalInfo.UdpPort
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
            if ((config.Client.UseTcp & client.UseTcp) == false)
            {
                return EnumConnectResult.Fail;
            }

            client.SetConnecting(true);

            ulong[] tunnelNames = new ulong[] { (ulong)TunnelDefaults.TCP, (ulong)TunnelDefaults.MIN };
            if (config.Client.UseOriginPort == false || client.UseOriginPort == false)
            {
                tunnelNames[0] = (ulong)TunnelDefaults.MIN;
            }
            for (int i = 0; i < tunnelNames.Length; i++)
            {
                ConnectResultModel result = await punchHoleTcp.Send(new ConnectParams
                {
                    Id = client.Id,
                    TunnelName = tunnelNames[i],
                    LocalPort = registerState.LocalInfo.UdpPort
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
                    var res = await heartMessengerSender.Heart(item.Connection);
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

        private void OnRegisterBind(bool state)
        {
            firstClients.Reset();
            clientInfoCaching.Clear();
            if (state)
            {
                clientInfoCaching.AddTunnelPort((ulong)TunnelDefaults.UDP, registerState.LocalInfo.UdpPort);
                clientInfoCaching.AddUdpserver((ulong)TunnelDefaults.UDP, udpServer as UdpServer);
                clientInfoCaching.AddTunnelPort((ulong)TunnelDefaults.TCP, registerState.LocalInfo.TcpPort);
            }
            else
            {
                clientInfoCaching.RemoveTunnelPort((ulong)TunnelDefaults.UDP);
                clientInfoCaching.RemoveUdpserver((ulong)TunnelDefaults.UDP, true);
                clientInfoCaching.RemoveTunnelPort((ulong)TunnelDefaults.TCP);
            }
        }

        private void OnServerSendClients(ClientsInfo clients)
        {
            try
            {

                if (registerState.OnlineConnection == null || clients.Clients == null)
                {
                    return;
                }
                lock (lockObject)
                {
                    IEnumerable<ulong> remoteIds = clients.Clients.Select(c => c.Id);
                    //下线了的
                    IEnumerable<ulong> offlines = clientInfoCaching.AllIds().Except(remoteIds).Where(c => c != registerState.ConnectId);
                    foreach (ulong offid in offlines)
                    {
                        clientInfoCaching.Remove(offid);
                    }
                    //新上线的
                    IEnumerable<ulong> upLines = remoteIds.Except(clientInfoCaching.AllIds());
                    IEnumerable<ClientsClientInfo> upLineClients = clients.Clients.Where(c => upLines.Contains(c.Id) && c.Id != registerState.ConnectId && c.Name != config.Client.Name);

                    foreach (ClientsClientInfo item in upLineClients)
                    {
                        EnumClientAccess enumClientAccess = (EnumClientAccess)item.Access;
                        ClientInfo client = new ClientInfo
                        {
                            Id = item.Id,
                            Name = item.Name,
                            UseTcp = (enumClientAccess & EnumClientAccess.UseTcp) == EnumClientAccess.UseTcp,
                            UseUdp = (enumClientAccess & EnumClientAccess.UseUdp) == EnumClientAccess.UseUdp,
                            UsePunchHole = (enumClientAccess & EnumClientAccess.UsePunchHole) == EnumClientAccess.UsePunchHole,
                            UseRelay = (enumClientAccess & EnumClientAccess.UseRelay) == EnumClientAccess.UseRelay,
                            UseAutoRelay = (enumClientAccess & EnumClientAccess.UseAutoRelay) == EnumClientAccess.UseAutoRelay,
                            UseOriginPort = (enumClientAccess & EnumClientAccess.UseOriginPort) == EnumClientAccess.UseOriginPort,
                        };
                        clientInfoCaching.Add(client);
                        if (firstClients.IsDefault)
                        {
                            if (config.Client.UsePunchHole && client.UsePunchHole)
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
                            else if (config.Client.AutoRelay && client.UseAutoRelay)
                            {
                                _ = Relay(client, true);
                            }
                        }
                    }

                    firstClients.Reverse();
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
                    connection = registerState.OnlineConnection;
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
            if (registerState.RemoteInfo.Relay == false)
            {
                Logger.Instance.Warning($"server relay not available");
                return;
            }

            IConnection connection = registerState.OnlineConnection;
            if (client.Connected == true)
            {
                return;
            }
            await Relay(connection, new ulong[] { registerState.ConnectId, 0, client.Id }, notify);
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
                    ICrypto crypto = await cryptoSwap.Swap(connection, null, config.Client.EncodePassword);
                    if (crypto == null)
                    {
                        Logger.Instance.Error("交换密钥失败，如果客户端设置了密钥，则目标端必须设置相同的密钥，如果目标端未设置密钥，则客户端必须留空");
                        return;
                    }
                    connection.EncodeEnable(crypto);
                }
            }

            ClientConnectTypes relayType = relayids.Span[1] == 0 ? ClientConnectTypes.RelayServer : ClientConnectTypes.RelayNode;
            ClientOnlineTypes onlineType = notify == false ? ClientOnlineTypes.Passive : ClientOnlineTypes.Active;
            clientInfoCaching.Online(relayids.Span[^1], connection, relayType, onlineType, (ulong)TunnelDefaults.MIN);
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

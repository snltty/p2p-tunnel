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

            PunchHoleSub();

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

        private void PunchHoleSub()
        {
            punchHoleUdp.OnStep1Handler.Sub((e) =>
            {
                if (config.Client.UseUdp == true)
                {
                    clientInfoCaching.SetConnecting(e.RawData.FromId, true);
                }
            });
            punchHoleUdp.OnStep2FailHandler.Sub((e) =>
            {
                if (clientInfoCaching.Get(e.RawData.FromId, out ClientInfo client))
                {
                    client.SetConnecting(false);
                    if (e.RawData.TunnelName > (ulong)TunnelDefaults.MAX)
                    {
                        clientInfoCaching.RemoveTunnelPort(e.RawData.TunnelName);
                        clientInfoCaching.RemoveUdpserver(e.RawData.TunnelName, true);
                        _ = clientsMessengerSender.RemoveTunnel(registerState.OnlineConnection, e.RawData.TunnelName);
                    }
                }

            });
            punchHoleUdp.OnStep3Handler.Sub((e) =>
            {
                clientInfoCaching.Online(e.RawData.FromId, e.Connection, ClientConnectTypes.P2P, ClientOnlineTypes.Passive, e.RawData.TunnelName);
                if (e.RawData.TunnelName > (ulong)TunnelDefaults.MAX)
                {
                    clientInfoCaching.RemoveTunnelPort(e.RawData.TunnelName);
                    _ = clientsMessengerSender.RemoveTunnel(registerState.OnlineConnection, e.RawData.TunnelName);
                }
            });
            punchHoleUdp.OnStep4Handler.Sub((e) =>
            {
                if (clientInfoCaching.Get(e.RawData.FromId, out ClientInfo client))
                {
                    clientInfoCaching.Online(e.RawData.FromId, e.Connection, ClientConnectTypes.P2P, ClientOnlineTypes.Active, e.RawData.TunnelName);
                    if (e.RawData.TunnelName > (ulong)TunnelDefaults.MAX)
                    {
                        clientInfoCaching.RemoveTunnelPort(e.RawData.TunnelName);
                        _ = clientsMessengerSender.RemoveTunnel(registerState.OnlineConnection, e.RawData.TunnelName);
                    }
                    punchHoleDirectionConfig.Add(client.Name);
                }
            });

            punchHoleTcp.OnStep1Handler.Sub((e) =>
            {
                if (config.Client.UseTcp == true)
                {
                    clientInfoCaching.SetConnecting(e.RawData.FromId, true);
                }
            });
            punchHoleTcp.OnStep2FailHandler.Sub((e) =>
            {
                if (clientInfoCaching.Get(e.RawData.FromId, out ClientInfo client))
                {
                    client.SetConnecting(false);
                    if (e.RawData.TunnelName > (ulong)TunnelDefaults.MAX)
                    {
                        clientInfoCaching.RemoveTunnelPort(e.RawData.TunnelName);
                        _ = clientsMessengerSender.RemoveTunnel(registerState.OnlineConnection, e.RawData.TunnelName);
                    }
                }
            });
            punchHoleTcp.OnStep3Handler.Sub((e) =>
            {
                if (clientInfoCaching.Get(e.RawData.FromId, out ClientInfo client))
                {
                    clientInfoCaching.Online(e.RawData.FromId, e.Connection, ClientConnectTypes.P2P, ClientOnlineTypes.Passive, e.RawData.TunnelName);
                    if (e.RawData.TunnelName > (ulong)TunnelDefaults.MAX)
                    {
                        clientInfoCaching.RemoveTunnelPort(e.RawData.TunnelName);
                        _ = clientsMessengerSender.RemoveTunnel(registerState.OnlineConnection, e.RawData.TunnelName);

                    }
                }

            });
            punchHoleTcp.OnStep4Handler.Sub((e) =>
            {
                if (clientInfoCaching.Get(e.RawData.FromId, out ClientInfo client))
                {
                    clientInfoCaching.Online(e.RawData.FromId, e.Connection, ClientConnectTypes.P2P, ClientOnlineTypes.Active, e.RawData.TunnelName);
                    if (e.RawData.TunnelName > (ulong)TunnelDefaults.MAX)
                    {
                        clientInfoCaching.RemoveTunnelPort(e.RawData.TunnelName);
                        _ = clientsMessengerSender.RemoveTunnel(registerState.OnlineConnection, e.RawData.TunnelName);
                    }
                    punchHoleDirectionConfig.Add(client.Name);
                }
            });
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
            if (clientInfoCaching.Get(connection.ConnectId, out ClientInfo client))
            {
                if (ReferenceEquals(connection, client.Connection))
                {
                    clientInfoCaching.Offline(connection.ConnectId, ClientOfflineTypes.Disconnect);
                }
            }
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
        private void OnReverse(OnPunchHoleArg arg)
        {
            if (clientInfoCaching.Get(arg.Data.FromId, out ClientInfo client))
            {
                PunchHoleReverseInfo model = new PunchHoleReverseInfo();
                model.DeBytes(arg.Data.Data);
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
            punchHoleTcp.SendStep2Stop(id);
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

            ulong[] tunnelNames = new ulong[] { (ulong)TunnelDefaults.UDP, (ulong)TunnelDefaults.MIN };
            if (config.Client.UseOriginPort == false)
            {
                tunnelNames[0] = (ulong)TunnelDefaults.MIN;
            }

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
            if (config.Client.UseOriginPort == false)
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
                    //Console.WriteLine($"start ping:{item.Name}");
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
                    // Console.WriteLine($"end ping:{item.Name}");
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
                            else
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

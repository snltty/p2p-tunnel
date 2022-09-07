using client.messengers.clients;
using client.messengers.punchHole;
using client.messengers.punchHole.tcp;
using client.messengers.punchHole.udp;
using client.messengers.register;
using client.realize.messengers.punchHole;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace client.realize.messengers.clients
{
    public class ClientsTransfer : IClientsTransfer
    {
        private BoolSpace firstClients = new BoolSpace(true);

        private readonly IPunchHoleUdp punchHoleUdp;
        private readonly IPunchHoleTcp punchHoleTcp;
        private readonly RegisterStateInfo registerState;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly PunchHoleMessengerSender punchHoleMessengerSender;
        private readonly Config config;

        private const byte TryReverseMinValue = 1;
        private const byte TryReverseMaxValue = 2;

        public ClientsTransfer(ClientsMessengerSender clientsMessengerSender,
            IPunchHoleUdp punchHoleUdp, IPunchHoleTcp punchHoleTcp, IClientInfoCaching clientInfoCaching,
            RegisterStateInfo registerState, PunchHoleMessengerSender punchHoleMessengerSender, Config config
        )
        {
            this.punchHoleUdp = punchHoleUdp;
            this.punchHoleTcp = punchHoleTcp;
            this.registerState = registerState;
            this.clientInfoCaching = clientInfoCaching;
            this.config = config;

            punchHoleUdp.OnStep1Handler.Sub((e) => clientInfoCaching.Connecting(e.RawData.FromId, true, ServerType.UDP));
            punchHoleUdp.OnStep2FailHandler.Sub((e) => clientInfoCaching.Connecting(e.RawData.FromId, false, ServerType.UDP));
            punchHoleUdp.OnStep3Handler.Sub((e) => { clientInfoCaching.Online(e.Data.FromId, e.Connection, ClientConnectTypes.P2P); });
            punchHoleUdp.OnStep4Handler.Sub((e) => { clientInfoCaching.Online(e.Data.FromId, e.Connection, ClientConnectTypes.P2P); });

            punchHoleTcp.OnStep1Handler.Sub((e) => clientInfoCaching.Connecting(e.RawData.FromId, true, ServerType.TCP));
            punchHoleTcp.OnStep2FailHandler.Sub((e) => clientInfoCaching.Connecting(e.RawData.FromId, false, ServerType.TCP));
            punchHoleTcp.OnStep3Handler.Sub((e) => clientInfoCaching.Online(e.Data.FromId, e.Connection, ClientConnectTypes.P2P));
            punchHoleTcp.OnStep4Handler.Sub((e) => clientInfoCaching.Online(e.Data.FromId, e.Connection, ClientConnectTypes.P2P));

            //中继连线
            punchHoleMessengerSender.OnRelay.Sub((param) =>
            {
                if (clientInfoCaching.Get(param.Raw.Data.FromId, out ClientInfo client))
                {
                    IConnection connection = null;
                    switch (param.Relay.ServerType)
                    {
                        case ServerType.TCP:
                            connection = registerState.TcpConnection.Clone();
                            break;
                        case ServerType.UDP:
                            connection = registerState.UdpConnection.Clone();
                            break;
                        default:
                            break;
                    }
                    if (connection != null)
                    {
                        connection.Relay = registerState.RemoteInfo.Relay;
                        clientInfoCaching.Online(param.Raw.Data.FromId, connection, ClientConnectTypes.Relay);
                    }
                }
            });

            this.punchHoleMessengerSender = punchHoleMessengerSender;
            //有人要求反向链接
            punchHoleMessengerSender.OnReverse.Sub(OnReverse);
            //本客户端注册状态
            registerState.OnRegisterStateChange.Sub(OnRegisterStateChange);
            //收到来自服务器的 在线客户端 数据
            clientsMessengerSender.OnServerClientsData.Sub(OnServerSendClients);

            Logger.Instance.Info("获取外网距离ing...");
            registerState.LocalInfo.RouteLevel = NetworkHelper.GetRouteLevel();
        }

        public void ConnectClient(ulong id)
        {
            if (clientInfoCaching.Get(id, out ClientInfo client))
            {
                ConnectClient(client);
            }
        }
        public void ConnectClient(ClientInfo info)
        {
            ConnectClient(info, TryReverseMinValue);
        }
        public void ConnectClient(ClientInfo info, byte tryreverse)
        {
            if (info.Id == registerState.ConnectId)
            {
                return;
            }

            Task.Run(async () =>
            {
                bool udp = false, tcp = false;
                if (config.Client.UseUdp && info.Udp && info.UdpConnecting == false && info.UdpConnected == false)
                {
                    udp = await ConnectUdp(info).ConfigureAwait(false);
                }
                if (config.Client.UseTcp && info.Tcp && info.TcpConnecting == false && info.TcpConnected == false)
                {
                    tcp = await ConnectTcp(info).ConfigureAwait(false);
                }

                if ((!udp || !tcp) && tryreverse < TryReverseMaxValue)
                {
                    ConnectReverse(info.Id, tryreverse);
                }
            });
        }

        public void ConnectReverse(ulong id)
        {
            ConnectReverse(id, TryReverseMinValue);
        }
        private void ConnectReverse(ulong id, byte tryreverse)
        {
            punchHoleMessengerSender.SendReverse(id, tryreverse).ConfigureAwait(false);
        }
        private void OnReverse(OnPunchHoleArg arg)
        {
            if (clientInfoCaching.Get(arg.Data.FromId, out ClientInfo client))
            {
                PunchHoleReverseInfo model = new PunchHoleReverseInfo();
                model.DeBytes(arg.Data.Data);
                ConnectClient(client, (byte)(model.TryReverse + 1));
            }
        }

        public void Reset(ulong id)
        {
            punchHoleMessengerSender.SendReset(id).ConfigureAwait(false);

        }
        public void ConnectStop(ulong id)
        {
            punchHoleTcp.SendStep2Stop(id);
        }

        private async Task<bool> ConnectUdp(ClientInfo info)
        {
            clientInfoCaching.Connecting(info.Id, true, ServerType.UDP);
            var result = await punchHoleUdp.Send(new ConnectParams
            {
                Id = info.Id,
                TunnelName = "udp",
                TryTimes = 5
            }).ConfigureAwait(false);
            if (result.State)
            {
                return result.State;
            }
            if (registerState.RemoteInfo.Relay)
            {
                IConnection connection = registerState.UdpConnection.Clone();
                connection.Relay = registerState.RemoteInfo.Relay;
                clientInfoCaching.Online(info.Id, connection, ClientConnectTypes.Relay);
                _ = punchHoleMessengerSender.SendRelay(info.Id, ServerType.UDP);
                return true;
            }
            else
            {
                Logger.Instance.Error((result.Result as ConnectFailModel).Msg);
                clientInfoCaching.Offline(info.Id, ServerType.UDP);
            }
            return false;
        }
        private async Task<bool> ConnectTcp(ClientInfo info)
        {
            clientInfoCaching.Connecting(info.Id, true, ServerType.TCP);
            var result = await punchHoleTcp.Send(new ConnectParams
            {
                Id = info.Id,
                TunnelName = "tcp",
                TryTimes = 5
            }).ConfigureAwait(false);
            if (result.State)
            {
                return result.State;
            }
            if (registerState.RemoteInfo.Relay)
            {
                IConnection connection = registerState.TcpConnection.Clone();
                connection.Relay = registerState.RemoteInfo.Relay;
                clientInfoCaching.Online(info.Id, connection, ClientConnectTypes.Relay);
                _ = punchHoleMessengerSender.SendRelay(info.Id, ServerType.TCP);
                return true;
            }
            else
            {
                Logger.Instance.Error((result.Result as ConnectFailModel).Msg);
                clientInfoCaching.Offline(info.Id, ServerType.TCP);
            }
            return false;
        }

        private void OnRegisterStateChange(bool state)
        {
            firstClients.Reset();
            clientInfoCaching.Clear();
        }
        private void OnServerSendClients(ClientsInfo clients)
        {
            try
            {
                if (registerState.OnlineConnection == null || clients.Clients == null)
                {
                    return;
                }

                IEnumerable<ulong> remoteIds = clients.Clients.Select(c => c.Id);
                //下线了的
                IEnumerable<ulong> offlines = clientInfoCaching.AllIds().Except(remoteIds).Where(c => c != registerState.ConnectId);
                foreach (ulong offid in offlines)
                {
                    clientInfoCaching.Offline(offid);
                    clientInfoCaching.Remove(offid);
                }
                //新上线的
                IEnumerable<ulong> upLines = remoteIds.Except(clientInfoCaching.AllIds());
                IEnumerable<ClientsClientInfo> upLineClients = clients.Clients.Where(c => upLines.Contains(c.Id) && c.Id != registerState.ConnectId);

                foreach (ClientsClientInfo item in upLineClients)
                {
                    ClientInfo client = new ClientInfo
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Mac = item.Mac,
                        Tcp = item.Tcp,
                        Udp = item.Udp
                    };
                    clientInfoCaching.Add(client);
                    if (firstClients.Get() && config.Client.AutoPunchHole)
                    {
                        if (registerState.LocalInfo.TcpPort == registerState.RemoteInfo.TcpPort || registerState.LocalInfo.UdpPort == registerState.RemoteInfo.UdpPort)
                        {
                            ConnectClient(client);
                        }
                        else
                        {
                            ConnectReverse(client.Id, TryReverseMinValue);
                        }
                    }
                }

                firstClients.Reverse();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }
    }
}

using client.messengers.clients;
using client.messengers.punchHole;
using client.messengers.punchHole.udp;
using client.messengers.register;
using client.realize.messengers.crypto;
using common.libs;
using common.server;
using common.server.model;
using common.server.servers.rudp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.realize.messengers.punchHole.udp
{
    /// <summary>
    /// rudp打洞
    /// </summary>
    public sealed class PunchHoleRUdpMessengerSender : IPunchHoleUdp
    {
        private readonly PunchHoleMessengerSender punchHoleMessengerSender;
        private readonly RegisterStateInfo registerState;
        private readonly CryptoSwap cryptoSwap;
        private readonly Config config;
        private readonly WheelTimer<object> wheelTimer;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly IClientsTunnel clientsTunnel;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="punchHoleMessengerSender"></param>
        /// <param name="registerState"></param>
        /// <param name="cryptoSwap"></param>
        /// <param name="config"></param>
        /// <param name="wheelTimer"></param>
        /// <param name="clientInfoCaching"></param>
        /// <param name="clientsTunnel"></param>
        public PunchHoleRUdpMessengerSender(PunchHoleMessengerSender punchHoleMessengerSender, RegisterStateInfo registerState, CryptoSwap cryptoSwap, Config config, WheelTimer<object> wheelTimer, IClientInfoCaching clientInfoCaching, IClientsTunnel clientsTunnel)
        {
            this.punchHoleMessengerSender = punchHoleMessengerSender;
            this.registerState = registerState;
            this.cryptoSwap = cryptoSwap;
            this.config = config;
            this.wheelTimer = wheelTimer;
            this.clientInfoCaching = clientInfoCaching;
            this.clientsTunnel = clientsTunnel;
        }
        private IConnection connection => registerState.UdpConnection;
#if DEBUG
        private bool UseLocalPort = true;
#else
        private bool UseLocalPort = true;
#endif

        private readonly ConcurrentDictionary<ulong, ConnectCacheModel> connectCache = new();

        private void SendTimeout(WheelTimerTimeout<object> timeout)
        {
            try
            {
                if (timeout.IsCanceled) return;

                ulong toid = (ulong)timeout.Task.State;
                timeout.Cancel();
                if (connectCache.TryGetValue(toid, out ConnectCacheModel cache))
                {
                    cache.Canceled = true;
                    _ = SendStep2Fail(cache.TunnelName, toid);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }
        private void AddSendTimeout(ulong toid)
        {
            if (connectCache.TryGetValue(toid, out ConnectCacheModel cache))
            {
                cache.SendTimeout = wheelTimer.NewTimeout(new WheelTimerTimeoutTask<object> { Callback = SendTimeout, State = toid }, 5000);
            }
        }
        private void RemoveSendTimeout(ulong toid)
        {
            if (connectCache.TryGetValue(toid, out ConnectCacheModel cache))
            {
                cache.SendTimeout?.Cancel();
            }
        }

        public SimpleSubPushHandler<ConnectParams> OnSendHandler => new SimpleSubPushHandler<ConnectParams>();
        public async Task<ConnectResultModel> Send(ConnectParams param)
        {
            if (param.TunnelName == (ulong)TunnelDefaults.MIN)
            {
                (ulong tunnelName, int localPort) = await clientsTunnel.NewBind(ServerType.UDP, (ulong)TunnelDefaults.MIN);
                param.TunnelName = tunnelName;
                param.LocalPort = localPort;
            }

            TaskCompletionSource<ConnectResultModel> tcs = new TaskCompletionSource<ConnectResultModel>();
            connectCache.TryAdd(param.Id, new ConnectCacheModel
            {
                Tcs = tcs,
                LocalPort = param.LocalPort,
            });
            AddSendTimeout(param.Id);

            await punchHoleMessengerSender.Request(new SendPunchHoleArg<PunchHoleStep1Info>
            {
                Connection = connection,
                TunnelName = param.TunnelName,
                ToId = param.Id,
                Data = new PunchHoleStep1Info { Step = (byte)PunchHoleUdpSteps.STEP_1, PunchType = PunchHoleTypes.UDP }
            }).ConfigureAwait(false);
            return await tcs.Task.ConfigureAwait(false);
        }

        public SimpleSubPushHandler<OnStep1Params> OnStep1Handler { get; } = new SimpleSubPushHandler<OnStep1Params>();
        public async Task OnStep1(OnStep1Params arg)
        {
            OnStep1Handler.Push(arg);
            if (arg.RawData.TunnelName > (ulong)TunnelDefaults.MAX)
            {
                await clientsTunnel.NewBind(arg.Connection.ServerType, arg.RawData.TunnelName);
            }

            RemoveSendTimeout(arg.RawData.FromId);
            if (clientInfoCaching.GetUdpserver(arg.RawData.TunnelName, out UdpServer udpServer))
            {

                foreach (var ip in arg.Data.LocalIps.Where(c => c.Equals(IPAddress.Any) == false))
                {
                    if (NotIPv6Support(ip))
                    {
                        continue;
                    }
                    udpServer.SendUnconnectedMessage(Helper.EmptyArray, new IPEndPoint(ip, arg.Data.LocalPort));
                }
                udpServer.SendUnconnectedMessage(Helper.EmptyArray, new IPEndPoint(arg.Data.Ip, arg.Data.Port));
                udpServer.SendUnconnectedMessage(Helper.EmptyArray, new IPEndPoint(arg.Data.Ip, arg.Data.Port + 1));

                AddSendTimeout(arg.RawData.FromId);
                await punchHoleMessengerSender.Request(new SendPunchHoleArg<PunchHoleStep2Info>
                {
                    Connection = arg.Connection,
                    TunnelName = arg.RawData.TunnelName,
                    ToId = arg.RawData.FromId,
                    Data = new PunchHoleStep2Info { Step = (byte)PunchHoleUdpSteps.STEP_2, PunchType = PunchHoleTypes.UDP }
                }).ConfigureAwait(false);
            }
            else
            {
                await SendStep2Fail(arg.RawData.TunnelName, arg.RawData.FromId).ConfigureAwait(false);
            }
        }

        public SimpleSubPushHandler<OnStep2Params> OnStep2Handler { get; } = new SimpleSubPushHandler<OnStep2Params>();
        public async Task OnStep2(OnStep2Params arg)
        {
            OnStep2Handler.Push(arg);
            await Task.Run(async () =>
             {
                 int localPort = arg.Data.LocalPort;
                 int port = arg.Data.Port;
                 if (connectCache.TryGetValue(arg.RawData.FromId, out ConnectCacheModel cache) == false)
                 {
                     Logger.Instance.Error($"udp 找不到缓存");
                     await SendStep2Fail(arg.RawData.TunnelName, arg.RawData.FromId).ConfigureAwait(false);
                     return;
                 }
                 if (clientInfoCaching.GetUdpserver(arg.RawData.TunnelName, out UdpServer udpServer) == false)
                 {
                     Logger.Instance.Error($"udp 找不到通道服务器：{arg.RawData.TunnelName}");
                     await SendStep2Fail(arg.RawData.TunnelName, arg.RawData.FromId).ConfigureAwait(false);
                     return;
                 }
                 RemoveSendTimeout(arg.RawData.FromId);

                 List<IPEndPoint> ips = new List<IPEndPoint>();
                 if (UseLocalPort)
                 {
                     var locals = arg.Data.LocalIps.Where(c => c.Equals(IPAddress.Any) == false && c.AddressFamily == AddressFamily.InterNetwork).Select(c => new IPEndPoint(c, localPort)).ToList();
                     ips.AddRange(locals);
                 }
                 if (IPv6Support())
                 {
                     var locals = arg.Data.LocalIps.Where(c => c.AddressFamily == AddressFamily.InterNetworkV6).Select(c => new IPEndPoint(c, port)).ToList();
                     ips.AddRange(locals);
                 }
                 ips.Add(new IPEndPoint(arg.Data.Ip, port));
                 ips.Add(new IPEndPoint(arg.Data.Ip, port + 1));


                 IConnection connection = null;
                 for (int i = 0; i < ips.Count; i++)
                 {
                     IPEndPoint ip = i >= ips.Count - 1 ? ips[^1] : ips[i];
                     if (NotIPv6Support(ip.Address))
                     {
                         continue;
                     }
                     Logger.Instance.DebugDebug($"udp {ip} connect");
                     connection = await udpServer.CreateConnection(ip);
                     if (connection != null)
                     {
                         break;
                     }
                     else
                     {
                         Logger.Instance.DebugError($"udp {ip} connect fail");
                     }
                 }
                 if (connection != null)
                 {
                     await CryptoSwap(connection).ConfigureAwait(false);
                     await SendStep3(connection, arg.RawData.TunnelName, arg.RawData.FromId).ConfigureAwait(false);
                 }
                 else
                 {
                     await SendStep2Fail(arg.RawData.TunnelName, arg.RawData.FromId).ConfigureAwait(false);
                 }
             });
        }

        private async Task CryptoSwap(IConnection connection)
        {
            if (config.Client.Encode)
            {
                ICrypto crypto = await cryptoSwap.Swap(null, connection, config.Client.EncodePassword);
                if (crypto == null)
                {
                    Logger.Instance.Error("udp打洞交换密钥失败，可能是两端密钥不一致，A如果设置了密钥，则B必须设置相同的密钥，如果B未设置密钥，则A必须留空");
                }
                else
                {
                    connection.EncodeEnable(crypto);
                }
            }
        }

        public SimpleSubPushHandler<OnStep21Params> OnStep21Handler { get; } = new SimpleSubPushHandler<OnStep21Params>();
        public async Task OnStep21(OnStep21Params arg)
        {
            OnStep21Handler.Push(arg);
            if (clientInfoCaching.GetUdpserver(arg.RawData.TunnelName, out UdpServer udpServer))
            {
                foreach (var ip in arg.Data.LocalIps.Where(c => c.Equals(IPAddress.Any) == false))
                {
                    if (NotIPv6Support(ip))
                    {
                        continue;
                    }
                    udpServer.SendUnconnectedMessage(Helper.EmptyArray, new IPEndPoint(ip, arg.Data.LocalPort));
                }
                udpServer.SendUnconnectedMessage(Helper.EmptyArray, new IPEndPoint(arg.Data.Ip, arg.Data.Port));
                udpServer.SendUnconnectedMessage(Helper.EmptyArray, new IPEndPoint(arg.Data.Ip, arg.Data.Port + 1));
            }
            await Task.CompletedTask;
        }

        public SimpleSubPushHandler<OnStep2FailParams> OnStep2FailHandler { get; } = new SimpleSubPushHandler<OnStep2FailParams>();
        public void OnStep2Fail(OnStep2FailParams arg)
        {
            OnStep2FailHandler.Push(arg);
        }
        private async Task SendStep2Fail(ulong tunname, ulong toid)
        {
            await punchHoleMessengerSender.Request(new SendPunchHoleArg<PunchHoleStep2FailInfo>
            {
                Connection = connection,
                TunnelName = tunname,
                ToId = toid,
                Data = new PunchHoleStep2FailInfo { Step = (byte)PunchHoleUdpSteps.STEP_2_Fail, PunchType = PunchHoleTypes.UDP }
            }).ConfigureAwait(false);

            if (connectCache.TryRemove(toid, out ConnectCacheModel cache))
            {
                cache.Canceled = true;
                cache.Tcs.SetResult(new ConnectResultModel
                {
                    State = false,
                    Result = new ConnectFailModel
                    {
                        Msg = "udp打洞失败",
                        Type = ConnectFailType.ERROR
                    }
                });
            }
        }

        private async Task SendStep3(IConnection connection, ulong tunnelName, ulong toid)
        {
            AddSendTimeout(toid);
            await punchHoleMessengerSender.Request(new SendPunchHoleArg<PunchHoleStep3Info>
            {
                TunnelName = tunnelName,
                Connection = connection,
                Data = new PunchHoleStep3Info
                {
                    Step = (byte)PunchHoleUdpSteps.STEP_3,
                    PunchType = PunchHoleTypes.UDP
                }
            }).ConfigureAwait(false);
        }
        public SimpleSubPushHandler<OnStep3Params> OnStep3Handler { get; } = new SimpleSubPushHandler<OnStep3Params>();
        public async Task OnStep3(OnStep3Params arg)
        {
            RemoveSendTimeout(arg.RawData.FromId);
            await punchHoleMessengerSender.Request(new SendPunchHoleArg<PunchHoleStep4Info>
            {
                Connection = arg.Connection,
                TunnelName = arg.RawData.TunnelName,
                Data = new PunchHoleStep4Info
                {
                    Step = (byte)PunchHoleUdpSteps.STEP_4,
                    PunchType = PunchHoleTypes.UDP
                }
            }).ConfigureAwait(false);
            OnStep3Handler.Push(arg);
        }

        public SimpleSubPushHandler<OnStep4Params> OnStep4Handler { get; } = new SimpleSubPushHandler<OnStep4Params>();
        public void OnStep4(OnStep4Params arg)
        {
            RemoveSendTimeout(arg.RawData.FromId);
            if (connectCache.TryRemove(arg.RawData.FromId, out ConnectCacheModel cache))
            {
                cache.Tcs.SetResult(new ConnectResultModel { State = true });
            }
            OnStep4Handler.Push(arg);
        }

        private bool NotIPv6Support(IPAddress ip)
        {
            return ip.AddressFamily == AddressFamily.InterNetworkV6 && (NetworkHelper.IPv6Support == false || registerState.LocalInfo.Ipv6s.Length == 0);
        }
        private bool IPv6Support()
        {
            return NetworkHelper.IPv6Support == true && registerState.LocalInfo.Ipv6s.Length > 0;
        }

    }
}

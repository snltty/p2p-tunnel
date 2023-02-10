using client.messengers.clients;
using client.messengers.punchHole;
using client.messengers.punchHole.tcp;
using client.messengers.register;
using client.realize.messengers.crypto;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace client.realize.messengers.punchHole.tcp.nutssb
{
    /// <summary>
    /// tcp打洞
    /// </summary>
    public sealed class PunchHoleTcpNutssBMessengerSender : IPunchHoleTcp
    {
        private readonly PunchHoleMessengerSender punchHoleMessengerSender;
        private readonly ITcpServer tcpServer;
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
        /// <param name="tcpServer"></param>
        /// <param name="registerState"></param>
        /// <param name="cryptoSwap"></param>
        /// <param name="config"></param>
        /// <param name="wheelTimer"></param>
        /// <param name="clientInfoCaching"></param>
        /// <param name="clientsTunnel"></param>
        public PunchHoleTcpNutssBMessengerSender(PunchHoleMessengerSender punchHoleMessengerSender, ITcpServer tcpServer,
            RegisterStateInfo registerState, CryptoSwap cryptoSwap, Config config, WheelTimer<object> wheelTimer, IClientInfoCaching clientInfoCaching, IClientsTunnel clientsTunnel)
        {
            this.punchHoleMessengerSender = punchHoleMessengerSender;
            this.tcpServer = tcpServer;
            this.registerState = registerState;
            this.cryptoSwap = cryptoSwap;
            this.config = config;
            this.wheelTimer = wheelTimer;
            this.clientInfoCaching = clientInfoCaching;
            this.clientsTunnel = clientsTunnel;
        }

        private IConnection TcpServer => registerState.TcpConnection;

        private int RouteLevel => registerState.LocalInfo.RouteLevel + 2;
#if DEBUG
        private bool UseLocalPort = true;
#else
        private bool UseLocalPort = true;
#endif
        private readonly ConcurrentDictionary<ulong, ConnectCacheModel> connectTcpCache = new();

        public SimpleSubPushHandler<ConnectParams> OnSendHandler => new SimpleSubPushHandler<ConnectParams>();
        public async Task<ConnectResultModel> Send(ConnectParams param)
        {
            if (param.TunnelName == (ulong)TunnelDefaults.MIN)
            {
                (ulong tunnelName, int localPort) = await clientsTunnel.NewBind(ServerType.TCP, (ulong)TunnelDefaults.MIN);
                param.TunnelName = tunnelName;
                param.LocalPort = localPort;
            }

            var ceche = new ConnectCacheModel
            {
                Tcs = new TaskCompletionSource<ConnectResultModel>(),
                TunnelName = param.TunnelName,
                LocalPort = param.LocalPort
            };
            connectTcpCache.TryAdd(param.Id, ceche);

            await SendStep1(param);
            return await ceche.Tcs.Task.ConfigureAwait(false);
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
            if (clientInfoCaching.GetTunnelPort(arg.RawData.TunnelName, out int localPort))
            {
                List<IPEndPoint> ips = arg.Data.LocalIps.Where(c => c.Equals(IPAddress.Any) == false).Select(c => new IPEndPoint(c, arg.Data.LocalPort)).ToList();
                ips.Add(new IPEndPoint(arg.Data.Ip, arg.Data.Port));
                ips.Add(new IPEndPoint(arg.Data.Ip, arg.Data.Port + 1));

                foreach (IPEndPoint ip in ips)
                {
                    if (ip.Address.Equals(IPAddress.Any) || ip.Address.Equals(IPAddress.IPv6Any))
                    {
                        continue;
                    }
                    if (NotIPv6Support(ip.Address))
                    {
                        continue;
                    }

                    using Socket targetSocket = new(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        targetSocket.IPv6Only(ip.Address.AddressFamily, false);
                        targetSocket.Ttl = (short)(RouteLevel);
                        targetSocket.ReuseBind(new IPEndPoint(ip.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, localPort));
                        _ = targetSocket.ConnectAsync(ip);
                    }
                    catch (Exception)
                    {
                    }
                    targetSocket.SafeClose();
                }
                await SendStep2(arg);
            }
            else
            {
                Logger.Instance.Warning($"OnStep1 未找到通道：{arg.RawData.TunnelName}");
                await SendStep2Fail(arg.RawData.TunnelName, arg.RawData.FromId).ConfigureAwait(false);
            }
        }

        private async Task CryptoSwap(IConnection connection)
        {
            if (config.Client.Encode)
            {
                ICrypto crypto = await cryptoSwap.Swap(connection, null, config.Client.EncodePassword);
                if (crypto == null)
                {
                    Logger.Instance.Error("tcp打洞交换密钥失败，可能是两端密钥不一致，A如果设置了密钥，则B必须设置相同的密钥，如果B未设置密钥，则A必须留空");
                }
                else
                {
                    connection.EncodeEnable(crypto);
                }
            }
        }

        private void Cancel(ulong id)
        {
            if (connectTcpCache.TryRemove(id, out ConnectCacheModel cache))
            {
                cache.Canceled = true;
                cache.Tcs.SetResult(new ConnectResultModel
                {
                    State = false,
                    Result = new ConnectFailModel
                    {
                        Msg = "取消",
                        Type = ConnectFailType.CANCEL
                    }
                });
            }
        }
        private void SendTimeout(WheelTimerTimeout<object> timeout)
        {
            try
            {
                if (timeout.IsCanceled) return;

                ulong toid = (ulong)timeout.Task.State;
                timeout.Cancel();
                if (connectTcpCache.TryRemove(toid, out ConnectCacheModel cache))
                {
                    cache.Canceled = true;
                    cache.Tcs.SetResult(new ConnectResultModel { State = false, Result = new ConnectFailModel { Type = ConnectFailType.ERROR, Msg = "tcp打洞超时" } });

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
            if (connectTcpCache.TryGetValue(toid, out ConnectCacheModel cache))
            {
                cache.SendTimeout = wheelTimer.NewTimeout(new WheelTimerTimeoutTask<object> { Callback = SendTimeout, State = toid }, 5000);
            }
        }
        private void RemoveSendTimeout(ulong toid)
        {
            if (connectTcpCache.TryGetValue(toid, out ConnectCacheModel cache))
            {
                cache.SendTimeout?.Cancel();
            }
        }


        public async Task SendStep1(ConnectParams param)
        {
            AddSendTimeout(param.Id);
            bool res = await punchHoleMessengerSender.Request(new SendPunchHoleArg<PunchHoleStep1Info>
            {
                TunnelName = param.TunnelName,
                Connection = TcpServer,
                ToId = param.Id,
                Data = new PunchHoleStep1Info { Step = (byte)PunchHoleTcpNutssBSteps.STEP_1, PunchType = PunchHoleTypes.TCP_NUTSSB }
            }).ConfigureAwait(false);
        }
        public async Task SendStep2(OnStep1Params arg)
        {
            AddSendTimeout(arg.RawData.FromId);
            bool res = await punchHoleMessengerSender.Request(new SendPunchHoleArg<PunchHoleStep2Info>
            {
                TunnelName = arg.RawData.TunnelName,
                Connection = TcpServer,
                ToId = arg.RawData.FromId,
                Data = new PunchHoleStep2Info { Step = (byte)PunchHoleTcpNutssBSteps.STEP_2, PunchType = PunchHoleTypes.TCP_NUTSSB }
            }).ConfigureAwait(false);
        }
        public SimpleSubPushHandler<OnStep2Params> OnStep2Handler { get; } = new SimpleSubPushHandler<OnStep2Params>();
        public async Task OnStep2(OnStep2Params arg)
        {
            await Task.Run(async () =>
            {
                OnStep2Handler.Push(arg);
                if (connectTcpCache.TryGetValue(arg.RawData.FromId, out ConnectCacheModel cache) == false)
                {
                    Logger.Instance.Warning($"OnStep2 未找到缓存：{arg.RawData.FromId}");
                    await SendStep2Fail(arg.RawData.TunnelName, arg.RawData.FromId).ConfigureAwait(false);
                    return;
                }
                RemoveSendTimeout(arg.RawData.FromId);

                bool success = false;
                List<IPEndPoint> ips = new List<IPEndPoint>();

                if (UseLocalPort)
                {
                    var locals = arg.Data.LocalIps.Where(c => c.Equals(IPAddress.Any) == false && c.AddressFamily == AddressFamily.InterNetwork).Select(c => new IPEndPoint(c, arg.Data.LocalPort)).ToList();
                    ips.AddRange(locals);
                }
                if (IPv6Support())
                {
                    var locals = arg.Data.LocalIps.Where(c => c.AddressFamily == AddressFamily.InterNetworkV6).Select(c => new IPEndPoint(c, arg.Data.Port)).ToList();
                    ips.AddRange(locals);
                }
                ips.Add(new IPEndPoint(arg.Data.Ip, arg.Data.Port));
                ips.Add(new IPEndPoint(arg.Data.Ip, arg.Data.Port + 1));

                for (byte i = 0; i < ips.Count; i++)
                {
                    if (cache.Canceled)
                    {
                        break;
                    }

                    IPEndPoint ip = i >= ips.Count ? ips[^1] : ips[i];
                    if (NotIPv6Support(ip.Address))
                    {
                        continue;
                    }

                    Socket targetSocket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        targetSocket.IPv6Only(ip.AddressFamily, false);
                        targetSocket.KeepAlive(time: config.Client.TimeoutDelay / 1000 / 5);
                        targetSocket.ReuseBind(new IPEndPoint(ip.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, cache.LocalPort));
                        Logger.Instance.DebugDebug($"tcp {ip} connect");
                        IAsyncResult result = targetSocket.BeginConnect(ip, null, null);
                        result.AsyncWaitHandle.WaitOne(ip.IsLan() ? 50 : 300, false);

                        if (result.IsCompleted)
                        {
                            if (cache.Canceled)
                            {
                                targetSocket.SafeClose();
                                break;
                            }

                            targetSocket.EndConnect(result);
                            Logger.Instance.Warning($"tcp {ip} connect success");
                            cache.Success = true;

                            IConnection connection = tcpServer.BindReceive(targetSocket, bufferSize: config.Client.TcpBufferSize);
                            await CryptoSwap(connection);
                            await SendStep3(connection, arg.RawData.TunnelName, arg.RawData.FromId);
                            success = true;
                            break;
                        }
                        else
                        {
                            Logger.Instance.DebugError($"tcp {ip} connect fail");
                            targetSocket.SafeClose();
                            targetSocket = null;
                        }
                    }
                    catch (SocketException ex)
                    {
                        Logger.Instance.DebugError($"tcp {ip} connect fail:{ex}");
                        targetSocket.SafeClose();
                        targetSocket = null;
                        if (ex.SocketErrorCode == SocketError.AddressNotAvailable)
                        {
                            Logger.Instance.DebugError($"{ex.SocketErrorCode}:{ip}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.DebugError($"tcp {ip} connect fail:{ex}");
                    }
                }

                if (success == false)
                {
                    await SendStep2Fail(arg.RawData.TunnelName, arg.RawData.FromId).ConfigureAwait(false);
                }

            }).ConfigureAwait(false);
        }
        public SimpleSubPushHandler<OnStep2RetryParams> OnStep2RetryHandler { get; } = new SimpleSubPushHandler<OnStep2RetryParams>();
        public void OnStep2Retry(OnStep2RetryParams e)
        {
            if (connectTcpCache.TryGetValue(e.RawData.FromId, out ConnectCacheModel cache) == false || cache.Success)
            {
                return;
            }

            if (clientInfoCaching.GetTunnelPort(e.RawData.TunnelName, out int localPort))
            {
                OnStep2RetryHandler.Push(e);

                if (NotIPv6Support(e.Data.Ip))
                {
                    return;
                }
                Socket targetSocket = null;
                try
                {
                    if (e.Data.Ip.Equals(IPAddress.Any) == false || e.Data.Ip.Equals(IPAddress.IPv6Any))
                    {
                        IPEndPoint target = new IPEndPoint(e.Data.Ip, e.Data.Port);
                        targetSocket = new Socket(target.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                        targetSocket.IPv6Only(target.AddressFamily, false);
                        targetSocket.Ttl = (short)(RouteLevel + e.RawData.Index);
                        targetSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        targetSocket.Bind(new IPEndPoint(e.Data.Ip.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, localPort));
                        _ = targetSocket.ConnectAsync(target);
                    }

                }
                catch (Exception)
                {
                }
                finally
                {
                    if (targetSocket != null)
                    {
                        targetSocket.SafeClose();
                    }
                }

            }
        }
        public SimpleSubPushHandler<ulong> OnSendStep2FailHandler => new SimpleSubPushHandler<ulong>();
        private async Task SendStep2Fail(ulong tunname, ulong toid)
        {
            OnSendStep2FailHandler.Push(toid);
            await punchHoleMessengerSender.Request(new SendPunchHoleArg<PunchHoleStep2FailInfo>
            {
                TunnelName = tunname,
                Connection = TcpServer,
                ToId = toid,
                Data = new PunchHoleStep2FailInfo { Step = (byte)PunchHoleTcpNutssBSteps.STEP_2_FAIL, PunchType = PunchHoleTypes.TCP_NUTSSB }
            }).ConfigureAwait(false);
            if (connectTcpCache.TryRemove(toid, out ConnectCacheModel cache))
            {
                cache.Canceled = true;
                cache.Tcs.SetResult(new ConnectResultModel
                {
                    State = false,
                    Result = new ConnectFailModel
                    {
                        Msg = "tcp打洞失败",
                        Type = ConnectFailType.ERROR
                    }
                });
            }

        }
        public SimpleSubPushHandler<OnStep2FailParams> OnStep2FailHandler { get; } = new SimpleSubPushHandler<OnStep2FailParams>();
        public void OnStep2Fail(OnStep2FailParams arg)
        {
            RemoveSendTimeout(arg.RawData.FromId);
            OnStep2FailHandler.Push(arg);
        }
        public async Task SendStep2Stop(ulong toid)
        {
            if (connectTcpCache.TryGetValue(toid, out ConnectCacheModel cache))
            {
                await punchHoleMessengerSender.Request(new SendPunchHoleArg<PunchHoleStep2StopInfo>
                {
                    TunnelName = cache.TunnelName,
                    Connection = TcpServer,
                    ToId = toid,
                    Data = new PunchHoleStep2StopInfo { Step = (byte)PunchHoleTcpNutssBSteps.STEP_2_STOP, PunchType = PunchHoleTypes.TCP_NUTSSB }
                }).ConfigureAwait(false);
                Cancel(toid);
            }
        }
        public void OnStep2Stop(OnStep2StopParams e)
        {
            Cancel(e.RawData.FromId);
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
                    Step = (byte)PunchHoleTcpNutssBSteps.STEP_3,
                    PunchType = PunchHoleTypes.TCP_NUTSSB
                }
            }).ConfigureAwait(false);
        }
        public SimpleSubPushHandler<OnStep3Params> OnStep3Handler { get; } = new SimpleSubPushHandler<OnStep3Params>();
        public async Task OnStep3(OnStep3Params arg)
        {
            RemoveSendTimeout(arg.RawData.FromId);
            await SendStep4(arg);
            OnStep3Handler.Push(arg);
        }

        private async Task SendStep4(OnStep3Params arg)
        {
            await punchHoleMessengerSender.Request(new SendPunchHoleArg<PunchHoleStep4Info>
            {
                TunnelName = arg.RawData.TunnelName,
                Connection = arg.Connection,
                Data = new PunchHoleStep4Info
                {
                    Step = (byte)PunchHoleTcpNutssBSteps.STEP_4,
                    PunchType = PunchHoleTypes.TCP_NUTSSB
                }
            });
        }
        public SimpleSubPushHandler<OnStep4Params> OnStep4Handler { get; } = new SimpleSubPushHandler<OnStep4Params>();
        public void OnStep4(OnStep4Params arg)
        {
            RemoveSendTimeout(arg.RawData.FromId);
            if (connectTcpCache.TryRemove(arg.RawData.FromId, out ConnectCacheModel cache))
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

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
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.realize.messengers.punchHole.tcp.nutssb
{
    public class PunchHoleTcpNutssBMessengerSender : IPunchHoleTcp
    {
        private readonly PunchHoleMessengerSender punchHoleMessengerSender;
        private readonly ITcpServer tcpServer;
        private readonly RegisterStateInfo registerState;
        private readonly CryptoSwap cryptoSwap;
        private readonly Config config;
        private readonly WheelTimer<object> wheelTimer;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly IClientsTunnel clientsTunnel;

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
        private ulong ConnectId => registerState.ConnectId;

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

            TaskCompletionSource<ConnectResultModel> tcs = new TaskCompletionSource<ConnectResultModel>();
            var ceche = new ConnectCacheModel
            {
                TryTimes = param.TryTimes,
                Tcs = tcs,
                TunnelName = param.TunnelName,
                LocalPort = param.LocalPort
            };
            connectTcpCache.TryAdd(param.Id, ceche);

            ceche.Step1Timeout = wheelTimer.NewTimeout(new WheelTimerTimeoutTask<object> { Callback = SendStep1Timeout, State = param.Id }, 2000);

            await SendStep1(param);

            return await tcs.Task.ConfigureAwait(false);
        }

        public SimpleSubPushHandler<OnStep1Params> OnStep1Handler { get; } = new SimpleSubPushHandler<OnStep1Params>();
        public async Task OnStep1(OnStep1Params arg)
        {
            if (arg.Data.IsDefault)
            {
                OnStep1Handler.Push(arg);
            }

            if (arg.RawData.TunnelName > (ulong)TunnelDefaults.MAX)
            {
                await clientsTunnel.NewBind(arg.Connection.ServerType, arg.RawData.TunnelName);
            }

            if (clientInfoCaching.GetTunnelPort(arg.RawData.TunnelName, out int localPort))
            {
                List<IPEndPoint> ips = arg.Data.LocalIps.Where(c => c.Equals(IPAddress.Any) == false).Select(c => new IPEndPoint(c, arg.Data.LocalPort)).ToList();
                ips.Add(new IPEndPoint(arg.Data.Ip, arg.Data.Port));

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
                        if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            targetSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                        }
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
        }

        public SimpleSubPushHandler<OnStep2Params> OnStep2Handler { get; } = new SimpleSubPushHandler<OnStep2Params>();
        public async Task OnStep2(OnStep2Params arg)
        {
            await Task.Run(async () =>
            {
                OnStep2Handler.Push(arg);

                if (connectTcpCache.TryGetValue(arg.RawData.FromId, out ConnectCacheModel cache) == false)
                {
                    return;
                }
                cache.Step1Timeout.Cancel();


                bool success = false;
                int interval = 0, times = cache.TryTimes;
                List<IPEndPoint> ips = new List<IPEndPoint>();

                if (UseLocalPort)
                {
                    if (registerState.RemoteInfo.Ip.Equals(arg.Data.Ip))
                    {
                        var locals = arg.Data.LocalIps.Where(c => c.Equals(IPAddress.Any) == false && c.AddressFamily == AddressFamily.InterNetwork).Select(c => new IPEndPoint(c, arg.Data.LocalPort)).ToList();
                        times += locals.Count;
                        ips.AddRange(locals);
                    }
                    else
                    {
                        foreach (var item in arg.Data.LocalIps)
                        {
                            if (IPAddress.Loopback.Equals(item) == false && NetworkHelper.Ping(item))
                            {
                                ips.Add(new IPEndPoint(item, arg.Data.LocalPort));
                                times += 1;
                            }
                        }
                    }
                }
                if (IPv6Support())
                {
                    ips.AddRange(arg.Data.LocalIps.Where(c => c.AddressFamily == AddressFamily.InterNetworkV6).Select(c => new IPEndPoint(c, arg.Data.Port)).ToList());
                }
                ips.Add(new IPEndPoint(arg.Data.Ip, arg.Data.Port));

                for (byte i = 0; i < times; i++)
                {
                    if (cache.Canceled)
                    {
                        break;
                    }
                    if (interval > 0)
                    {
                        await Task.Delay(interval);
                        interval = 0;
                    }

                    IPEndPoint ip = i >= ips.Count ? ips[^1] : ips[i];
                    if (NotIPv6Support(ip.Address))
                    {
                        continue;
                    }

                    Socket targetSocket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        targetSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                        targetSocket.KeepAlive(time: config.Client.TimeoutDelay / 1000 / 5);
                        targetSocket.ReuseBind(new IPEndPoint(ip.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, cache.LocalPort));
                        Logger.Instance.DebugDebug($"{ip} connect====================");
                        IAsyncResult result = targetSocket.BeginConnect(ip, null, null);
                        result.AsyncWaitHandle.WaitOne(2000, false);

                        if (result.IsCompleted)
                        {
                            if (cache.Canceled)
                            {
                                targetSocket.SafeClose();
                                break;
                            }
                            targetSocket.EndConnect(result);

                            if (arg.Data.IsDefault)
                            {
                                IConnection connection = tcpServer.BindReceive(targetSocket, bufferSize: config.Client.TcpBufferSize);
                                await CryptoSwap(connection);
                                await SendStep3(connection, arg.RawData.TunnelName, arg.RawData.FromId);
                            }
                            else
                            {
                                if (connectTcpCache.TryRemove(arg.RawData.FromId, out _))
                                {
                                    cache.Tcs.SetResult(new ConnectResultModel { State = true });
                                }
                            }
                            success = true;
                            break;
                        }
                        else
                        {
                            Logger.Instance.DebugError($"{ip} connect fail");
                            targetSocket.SafeClose();
                            targetSocket = null;
                            interval = 100;
                            await SendStep2Retry(arg, i).ConfigureAwait(false);
                        }
                    }
                    catch (SocketException ex)
                    {
                        Logger.Instance.DebugError($"{ip} connect fail:{ex}");
                        targetSocket.SafeClose();
                        targetSocket = null;
                        interval = 100;
                        if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                        {
                            interval = 2000;
                        }
                        else if (ex.SocketErrorCode == SocketError.AddressNotAvailable)
                        {
                            interval = 1000;
                            Logger.Instance.DebugError($"{ex.SocketErrorCode}:{ip}");
                        }
                        else
                        {
                            await SendStep2Retry(arg, i).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.DebugError($"{ip} connect fail:{ex}");
                    }
                }

                if (!success)
                {
                    await SendStep2Fail(arg).ConfigureAwait(false);
                }

            }).ConfigureAwait(false);
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

        public SimpleSubPushHandler<OnStep2RetryParams> OnStep2RetryHandler { get; } = new SimpleSubPushHandler<OnStep2RetryParams>();
        public void OnStep2Retry(OnStep2RetryParams e)
        {
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
                        targetSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                        targetSocket.Ttl = (short)(RouteLevel + e.RawData.Index);
                        //targetSocket.Ttl = (short)(RouteLevel);
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

        public SimpleSubPushHandler<OnStep2FailParams> OnStep2FailHandler { get; } = new SimpleSubPushHandler<OnStep2FailParams>();
        public void OnStep2Fail(OnStep2FailParams arg)
        {
            OnStep2FailHandler.Push(arg);
        }

        public void OnStep2Stop(OnStep2StopParams e)
        {
            Cancel(e.RawData.FromId);
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

        public SimpleSubPushHandler<OnStep3Params> OnStep3Handler { get; } = new SimpleSubPushHandler<OnStep3Params>();
        public async Task OnStep3(OnStep3Params arg)
        {
            OnStep3Handler.Push(arg);
            await SendStep4(arg);
        }

        public SimpleSubPushHandler<OnStep4Params> OnStep4Handler { get; } = new SimpleSubPushHandler<OnStep4Params>();
        public void OnStep4(OnStep4Params arg)
        {
            if (connectTcpCache.TryRemove(arg.Data.FromId, out ConnectCacheModel cache))
            {
                if (cache.Step3Timeout != null)
                {
                    cache.Step3Timeout.Cancel();
                }
                cache.Tcs.SetResult(new ConnectResultModel { State = true });
            }
            OnStep4Handler.Push(arg);
        }

        public async Task SendStep1(ConnectParams param)
        {
            Logger.Instance.DebugDebug($"before Send Step1, toid:{param.Id},fromid:{ConnectId}");
            await punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep1Info>
            {
                TunnelName = param.TunnelName,
                Connection = TcpServer,
                ToId = param.Id,
                Data = new PunchHoleStep1Info { Step = (byte)PunchHoleTcpNutssBSteps.STEP_1, PunchType = PunchHoleTypes.TCP_NUTSSB }
            }).ConfigureAwait(false);
            Logger.Instance.DebugDebug($"after Send Step1, toid:{param.Id},fromid:{ConnectId}");
        }
        private void SendStep1Timeout(WheelTimerTimeout<object> timeout)
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

                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }

        }

        public async Task SendStep2(OnStep1Params arg)
        {
            Logger.Instance.DebugDebug($"before Send Step2, toid:{arg.RawData.FromId},fromid:{ConnectId}");
            await punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep2Info>
            {
                TunnelName = arg.RawData.TunnelName,
                Connection = TcpServer,
                ToId = arg.RawData.FromId,
                Data = new PunchHoleStep2Info { Step = (byte)PunchHoleTcpNutssBSteps.STEP_2, PunchType = PunchHoleTypes.TCP_NUTSSB }
            }).ConfigureAwait(false);
            Logger.Instance.DebugDebug($"after Send Step2, toid:{arg.RawData.FromId},fromid:{ConnectId}");
        }
        private async Task SendStep2Retry(OnStep2Params arg, byte index)
        {
            Logger.Instance.DebugDebug($"before Send Step2Retry");
            await punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep2TryInfo>
            {
                TunnelName = arg.RawData.TunnelName,
                Connection = TcpServer,
                ToId = arg.RawData.FromId,
                Index = index,
                Data = new PunchHoleStep2TryInfo { Step = (byte)PunchHoleTcpNutssBSteps.STEP_2_TRY, PunchType = PunchHoleTypes.TCP_NUTSSB }
            }).ConfigureAwait(false);
            Logger.Instance.DebugDebug($"after Send Step2Retry");
        }
        public SimpleSubPushHandler<ulong> OnSendStep2FailHandler => new SimpleSubPushHandler<ulong>();
        private async Task SendStep2Fail(OnStep2Params arg)
        {

            if (connectTcpCache.TryRemove(arg.RawData.FromId, out ConnectCacheModel cache))
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
            if (arg.Data.IsDefault)
            {
                OnSendStep2FailHandler.Push(arg.RawData.FromId);
                Logger.Instance.DebugDebug($"before Send Step2Fail");
                await punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep2FailInfo>
                {
                    TunnelName = arg.RawData.TunnelName,
                    Connection = TcpServer,
                    ToId = arg.RawData.FromId,
                    Data = new PunchHoleStep2FailInfo { Step = (byte)PunchHoleTcpNutssBSteps.STEP_2_FAIL, PunchType = PunchHoleTypes.TCP_NUTSSB }
                }).ConfigureAwait(false);
                Logger.Instance.DebugDebug($"after Send Step2Fail");
            }
        }
        public async Task SendStep2Stop(ulong toid)
        {
            if (connectTcpCache.TryGetValue(toid, out ConnectCacheModel cache))
            {
                await punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep2StopInfo>
                {
                    TunnelName = cache.TunnelName,
                    Connection = TcpServer,
                    ToId = toid,
                    Data = new PunchHoleStep2StopInfo { Step = (byte)PunchHoleTcpNutssBSteps.STEP_2_STOP, PunchType = PunchHoleTypes.TCP_NUTSSB }
                }).ConfigureAwait(false);
                Cancel(toid);
            }
        }

        private void SendStep3Timeout(WheelTimerTimeout<object> timeout)
        {
            try
            {
                if (timeout.IsCanceled) return;

                ulong toid = (ulong)timeout.Task.State;
                timeout.Cancel();
                if (connectTcpCache.TryRemove(toid, out ConnectCacheModel cache))
                {
                    Logger.Instance.DebugDebug($"{toid} cache  timeout1");
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

                    OnSendStep2FailHandler.Push(toid);
                    punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep2FailInfo>
                    {
                        TunnelName = cache.TunnelName,
                        Connection = TcpServer,
                        ToId = toid,
                        Data = new PunchHoleStep2FailInfo { Step = (byte)PunchHoleTcpNutssBSteps.STEP_2_FAIL, PunchType = PunchHoleTypes.TCP_NUTSSB }
                    }).ConfigureAwait(false);
                }

            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }

        }
        private async Task SendStep3(IConnection connection, ulong tunnelName, ulong toid)
        {
            WheelTimerTimeout<object> step3Timeout = wheelTimer.NewTimeout(new WheelTimerTimeoutTask<object>
            {
                Callback = SendStep3Timeout,
                State = toid
            }, 2000);
            if (connectTcpCache.TryGetValue(toid, out ConnectCacheModel cache))
            {
                cache.Step3Timeout = step3Timeout;
            }

            Logger.Instance.DebugDebug($"before Send Step3, toid:{toid},fromid:{ConnectId}");
            await punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep3Info>
            {
                TunnelName = tunnelName,
                Connection = connection,
                Data = new PunchHoleStep3Info
                {
                    FromId = ConnectId,
                    Step = (byte)PunchHoleTcpNutssBSteps.STEP_3,
                    PunchType = PunchHoleTypes.TCP_NUTSSB
                }
            }).ConfigureAwait(false);
            Logger.Instance.DebugDebug($"after Send Step3, toid:{toid},fromid:{ConnectId}");
        }
        private async Task SendStep4(OnStep3Params arg)
        {
            Logger.Instance.DebugDebug($"before Send Step4, toid:{arg.RawData.FromId},fromid:{ConnectId}");
            await punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep4Info>
            {
                TunnelName = arg.RawData.TunnelName,
                Connection = arg.Connection,
                Data = new PunchHoleStep4Info
                {
                    FromId = ConnectId,
                    Step = (byte)PunchHoleTcpNutssBSteps.STEP_4,
                    PunchType = PunchHoleTypes.TCP_NUTSSB
                }
            });
            Logger.Instance.DebugDebug($"after Send Step4, toid:{arg.RawData.FromId},fromid:{ConnectId}");
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

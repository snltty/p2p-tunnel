using client.messengers.punchHole;
using client.messengers.punchHole.tcp;
using client.messengers.register;
using client.realize.messengers.crypto;
using common.libs;
using common.libs.extends;
using common.server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        public PunchHoleTcpNutssBMessengerSender(PunchHoleMessengerSender punchHoleMessengerSender, ITcpServer tcpServer,
            RegisterStateInfo registerState, CryptoSwap cryptoSwap, Config config, WheelTimer<object> wheelTimer)
        {
            this.punchHoleMessengerSender = punchHoleMessengerSender;
            this.tcpServer = tcpServer;
            this.registerState = registerState;
            this.cryptoSwap = cryptoSwap;
            this.config = config;
            this.wheelTimer = wheelTimer;
        }

        private IConnection TcpServer => registerState.TcpConnection;
        private ulong ConnectId => registerState.ConnectId;

        private int ClientTcpPort => registerState.LocalInfo.TcpPort;
        private int RouteLevel => registerState.LocalInfo.RouteLevel + 5;
#if DEBUG
        private bool UseLocalPort = false;
#else
        private bool UseLocalPort = true;
#endif
        private bool UseGuesstPort = false;

        private readonly ConcurrentDictionary<ulong, ConnectCacheModel> connectTcpCache = new();


        public SimpleSubPushHandler<ConnectParams> OnSendHandler => new SimpleSubPushHandler<ConnectParams>();
        public async Task<ConnectResultModel> Send(ConnectParams param)
        {
            TaskCompletionSource<ConnectResultModel> tcs = new TaskCompletionSource<ConnectResultModel>();
            connectTcpCache.TryAdd(param.Id, new ConnectCacheModel
            {
                TryTimes = param.TryTimes,
                Tcs = tcs,
                TunnelName = param.TunnelName,
            });

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

            List<IPEndPoint> ips = arg.Data.LocalIps.Select(c => new IPEndPoint(c, arg.Data.LocalPort)).ToList();
            ips.Add(new IPEndPoint(arg.Data.Ip, arg.Data.Port));

            foreach (IPEndPoint ip in ips)
            {
                using Socket targetSocket = new(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    targetSocket.Ttl = (short)(RouteLevel);
                    targetSocket.ReuseBind(new IPEndPoint(config.Client.BindIp, ClientTcpPort));
                    _ = targetSocket.ConnectAsync(ip);
                }
                catch (Exception)
                {
                }
                targetSocket.SafeClose();
            }

            if (arg.Data.GuessPort > 0)
            {
                int bindPort = NetworkHelper.GetRandomPort();
                int startPort = arg.Data.Port;
                int endPort = arg.Data.Port;
                if (arg.Data.GuessPort > 0)
                {
                    startPort = arg.Data.GuessPort;
                    endPort = startPort + 20;
                }
                if (endPort > 65535)
                {
                    endPort = 65535;
                }
                for (int i = startPort; i <= endPort; i++)
                {
                    IPEndPoint localEndPoint = new IPEndPoint(config.Client.BindIp, ClientTcpPort);

                    /*
                    var socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    socket.ReuseBind(localEndPoint);
                    socket.Listen(int.MaxValue);
                    _ = Task.Run(() =>
                    {
                        while (true)
                        {
                            var client = socket.Accept();
                            Console.WriteLine($"收到连接：{client.RemoteEndPoint}");
                        }
                    });
                    */

                    IPEndPoint target = new IPEndPoint(arg.Data.Ip, i);
                    using Socket targetSocket = new Socket(target.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    targetSocket.Ttl = (short)(RouteLevel);
                    targetSocket.ReuseBind(localEndPoint);
                    _ = targetSocket.ConnectAsync(target);

                    targetSocket.SafeClose();
                }
            }

            await SendStep2(arg);
        }

        public SimpleSubPushHandler<OnStep2Params> OnStep2Handler { get; } = new SimpleSubPushHandler<OnStep2Params>();
        public async Task OnStep2(OnStep2Params arg)
        {
            await Task.Run(async () =>
            {
                OnStep2Handler.Push(arg);

                List<Tuple<IPAddress, int>> ips = new List<Tuple<IPAddress, int>>();
                if (UseLocalPort && registerState.RemoteInfo.Ip.ToString() == arg.Data.Ip.ToString())
                {
                    ips = arg.Data.LocalIps.Select(c => new Tuple<IPAddress, int>(c, arg.Data.LocalPort)).ToList();
                }

                ips.Add(new Tuple<IPAddress, int>(arg.Data.Ip, arg.Data.Port));
                if (!connectTcpCache.TryGetValue(arg.RawData.FromId, out ConnectCacheModel cache))
                {
                    return;
                }


                bool success = false;
                int interval = 0, port = 0;
                for (int i = 0; i < cache.TryTimes; i++)
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

                    Tuple<IPAddress, int> ip = i >= ips.Count ? ips[^1] : ips[i];
                    if (port == 0)
                    {
                        port = ip.Item2;
                    }

                    IPEndPoint targetEndpoint = new IPEndPoint(ip.Item1, port);
                    Socket targetSocket = new Socket(targetEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        targetSocket.KeepAlive();
                        targetSocket.ReuseBind(new IPEndPoint(config.Client.BindIp, ClientTcpPort));
                        IAsyncResult result = targetSocket.BeginConnect(targetEndpoint, null, null);
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
                            targetSocket.SafeClose();
                            targetSocket = null;
                            interval = 100;
                            await SendStep2Retry(arg).ConfigureAwait(false);
                            if (arg.Data.GuessPort > 0)
                            {
                                interval = 0;
                                port = arg.Data.GuessPort + i;
                            }
                        }
                    }
                    catch (SocketException ex)
                    {
                        Logger.Instance.DebugError($"{targetEndpoint}--------{ex}");
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
                            Logger.Instance.Error($"{ex.SocketErrorCode}:{targetEndpoint}");
                        }
                        else
                        {
                            await SendStep2Retry(arg).ConfigureAwait(false);
                            if (arg.Data.GuessPort > 0)
                            {
                                interval = 0;
                                port = arg.Data.GuessPort + i;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error($"{targetEndpoint}--------{ex}");
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
            OnStep2RetryHandler.Push(e);
            int startPort = e.Data.Port;
            int endPort = e.Data.Port;
            if (e.Data.GuessPort > 0)
            {
                startPort = e.Data.GuessPort;
                endPort = startPort + 20;
            }
            if (endPort > 65535)
            {
                endPort = 65535;
            }
            for (int i = startPort; i <= endPort; i++)
            {

                Socket targetSocket = null;
                try
                {
                    IPEndPoint target = new IPEndPoint(e.Data.Ip, i);
                    targetSocket = new Socket(target.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    targetSocket.Ttl = (short)(RouteLevel);
                    targetSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    targetSocket.Bind(new IPEndPoint(config.Client.BindIp, ClientTcpPort));
                    _ = targetSocket.ConnectAsync(target);
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
            int port = 0;
            if (UseGuesstPort)
            {
                port = await punchHoleMessengerSender.GetGuessPort(common.server.model.ServerType.TCP);
            }

            Logger.Instance.DebugDebug($"before Send Step1, toid:{param.Id},fromid:{ConnectId}");
            await punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep1Info>
            {
                TunnelName = param.TunnelName,
                Connection = TcpServer,
                ToId = param.Id,
                GuessPort = port,
                Data = new PunchHoleStep1Info { Step = (byte)PunchHoleTcpNutssBSteps.STEP_1, PunchType = PunchHoleTypes.TCP_NUTSSB }
            }).ConfigureAwait(false);
            Logger.Instance.DebugDebug($"after Send Step1, toid:{param.Id},fromid:{ConnectId}");
        }
        public async Task SendStep2(OnStep1Params arg)
        {
            int port = 0;
            if (UseGuesstPort)
            {
                port = await punchHoleMessengerSender.GetGuessPort(common.server.model.ServerType.TCP);
            }

            Logger.Instance.DebugDebug($"before Send Step2, toid:{arg.RawData.FromId},fromid:{ConnectId}");
            await punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep2Info>
            {
                TunnelName = arg.RawData.TunnelName,
                Connection = TcpServer,
                ToId = arg.RawData.FromId,
                GuessPort = port,
                Data = new PunchHoleStep2Info { Step = (byte)PunchHoleTcpNutssBSteps.STEP_2, PunchType = PunchHoleTypes.TCP_NUTSSB }
            }).ConfigureAwait(false);
            Logger.Instance.DebugDebug($"after Send Step2, toid:{arg.RawData.FromId},fromid:{ConnectId}");
        }
        private async Task SendStep2Retry(OnStep2Params arg)
        {
            int port = 0;
            if (UseGuesstPort)
            {
                port = await punchHoleMessengerSender.GetGuessPort(common.server.model.ServerType.TCP);
            }
            Logger.Instance.DebugDebug($"before Send Step2Retry");
            await punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep2TryInfo>
            {
                TunnelName = arg.RawData.TunnelName,
                Connection = TcpServer,
                ToId = arg.RawData.FromId,
                GuessPort = port,
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
                }
                OnSendStep2FailHandler.Push(toid);
                punchHoleMessengerSender.Send(new SendPunchHoleArg<PunchHoleStep2FailInfo>
                {
                    TunnelName = string.Empty,
                    Connection = TcpServer,
                    ToId = toid,
                    Data = new PunchHoleStep2FailInfo { Step = (byte)PunchHoleTcpNutssBSteps.STEP_2_FAIL, PunchType = PunchHoleTypes.TCP_NUTSSB }
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }

        }
        private async Task SendStep3(IConnection connection, string tunnelName, ulong toid)
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
    }
}

using client.messengers.clients;
using client.messengers.punchHole;
using client.messengers.punchHole.tcp;
using client.messengers.singnin;
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
        private readonly SignInStateInfo signInState;
        private readonly CryptoSwap cryptoSwap;
        private readonly Config config;
        private readonly WheelTimer<object> wheelTimer;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly IClientsTunnel clientsTunnel;

        public PunchHoleTcpNutssBMessengerSender(PunchHoleMessengerSender punchHoleMessengerSender, ITcpServer tcpServer,
            SignInStateInfo signInState, CryptoSwap cryptoSwap, Config config, WheelTimer<object> wheelTimer, IClientInfoCaching clientInfoCaching, IClientsTunnel clientsTunnel)
        {
            this.punchHoleMessengerSender = punchHoleMessengerSender;
            this.tcpServer = tcpServer;
            this.signInState = signInState;
            this.cryptoSwap = cryptoSwap;
            this.config = config;
            this.wheelTimer = wheelTimer;
            this.clientInfoCaching = clientInfoCaching;
            this.clientsTunnel = clientsTunnel;
        }

        private IConnection Connection => signInState.Connection;

        private int RouteLevel => signInState.LocalInfo.RouteLevel + config.Client.TTL;
#if DEBUG
        private bool UseLocalPort = true;
#else
        private bool UseLocalPort = true;
#endif
        private readonly ConcurrentDictionary<ulong, ConnectCacheModel> connectTcpCache = new();

        public event IPunchHoleTcp.StepEvent OnStepHandler;

        public async Task<ConnectResultModel> Send(ConnectParams param)
        {
            if (param.NewTunnel == 1)
            {
                param.LocalPort = await clientsTunnel.NewBind(ServerType.TCP, Connection.ConnectId, param.Id);
            }
            else
            {
                clientInfoCaching.AddTunnelPort(param.Id, signInState.LocalInfo.Port);
            }

            var ceche = new ConnectCacheModel
            {
                Tcs = new TaskCompletionSource<ConnectResultModel>(),
                LocalPort = param.LocalPort,
                NewTunnel = param.NewTunnel,
            };
            connectTcpCache.TryAdd(param.Id, ceche);

            await SendStep1(param);
            return await ceche.Tcs.Task.ConfigureAwait(false);
        }
        public async Task InputData(PunchHoleStepModel model)
        {
            PunchHoleTcpNutssBSteps step = (PunchHoleTcpNutssBSteps)model.RawData.PunchStep;
            OnStepHandler?.Invoke(this, model);
            switch (step)
            {
                case PunchHoleTcpNutssBSteps.STEP_1:
                    {
                        PunchHoleNotifyInfo data = new PunchHoleNotifyInfo();
                        data.DeBytes(model.RawData.Data);
                        model.Data = data;
                        _ = OnStep1(model);
                    }
                    break;
                case PunchHoleTcpNutssBSteps.STEP_2:
                    {

                        PunchHoleNotifyInfo data = new PunchHoleNotifyInfo();
                        data.DeBytes(model.RawData.Data);
                        model.Data = data;
                        _ = OnStep2(model);
                    }
                    break;
                case PunchHoleTcpNutssBSteps.STEP_2_TRY:
                    {
                        PunchHoleNotifyInfo data = new PunchHoleNotifyInfo();
                        data.DeBytes(model.RawData.Data);
                        model.Data = data;
                        OnStep2Retry(model);
                    }
                    break;
                case PunchHoleTcpNutssBSteps.STEP_2_FAIL:
                    OnStep2Fail(model);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_2_STOP:
                    OnStep2Stop(model);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_3:
                    {

                        PunchHoleStep3Info data = new PunchHoleStep3Info();
                        data.DeBytes(model.RawData.Data);
                        model.Data = data;
                        _ = OnStep3(model);
                    }
                    break;
                case PunchHoleTcpNutssBSteps.STEP_4:
                    {
                        PunchHoleStep4Info data = new PunchHoleStep4Info();
                        data.DeBytes(model.RawData.Data);
                        model.Data = data;
                        OnStep4(model);
                    }
                    break;
                default:
                    break;
            }
            await Task.CompletedTask;
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
                if (connectTcpCache.TryGetValue(toid, out ConnectCacheModel cache))
                {
                    cache.Canceled = true;
                    _ = SendStep2Fail(toid, cache.NewTunnel);
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

        private async Task SendStep1(ConnectParams param)
        {
            AddSendTimeout(param.Id);
            bool res = await punchHoleMessengerSender.Request(new SendPunchHoleArg<PunchHoleStep1Info>
            {
                NewTunnel = param.NewTunnel,
                Connection = Connection,
                ToId = param.Id,
                Data = new PunchHoleStep1Info { Step = (byte)PunchHoleTcpNutssBSteps.STEP_1, PunchType = PunchHoleTypes.TCP_NUTSSB }
            }).ConfigureAwait(false);
        }
        private async Task OnStep1(PunchHoleStepModel model)
        {

            if (model.RawData.NewTunnel == 1)
            {
                await clientsTunnel.NewBind(ServerType.TCP, Connection.ConnectId, model.RawData.FromId);
            }
            else
            {
                clientInfoCaching.AddTunnelPort(model.RawData.FromId, signInState.LocalInfo.Port);
            }

            PunchHoleNotifyInfo data = model.Data as PunchHoleNotifyInfo;

            if (clientInfoCaching.GetTunnelPort(model.RawData.FromId, out int localPort))
            {
                List<IPEndPoint> ips = data.LocalIps.Where(c => c.Equals(IPAddress.Any) == false && c.Equals(IPAddress.IPv6Any) == false).Select(c => new IPEndPoint(c, data.LocalPort)).ToList();
                for (int i = 0; i <= 1; i++)
                {
                    if (data.Port + i < ushort.MaxValue)
                    {
                        ips.Add(new IPEndPoint(data.Ip, data.Port + i));
                    }
                }

                var sockets = ips.Where(c => NotIPv6Support(c.Address) == false).Select(ip =>
                {
                    using Socket targetSocket = new(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        targetSocket.IPv6Only(ip.Address.AddressFamily, false);
                        targetSocket.Ttl = (short)(RouteLevel);
                        targetSocket.ReuseBind(new IPEndPoint(ip.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, localPort));
                        _ = targetSocket.ConnectAsync(ip);
                        return targetSocket;
                    }
                    catch (Exception)
                    {
                    }
                    return null;
                });

                foreach (Socket item in sockets.Where(c => c != null && c.Connected == false))
                {
                    item.SafeClose();
                }
                await SendStep2(model);
            }
            else
            {
                Logger.Instance.Warning($"tcp OnStep1 未找到通道：{model.RawData.FromId}");
                await SendStep2Fail(model.RawData.FromId, model.RawData.NewTunnel).ConfigureAwait(false);
            }
        }

        private async Task CryptoSwap(IConnection connection)
        {
            if (config.Client.Encode)
            {
                ICrypto crypto = await cryptoSwap.Swap(connection, config.Client.EncodePassword);
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

        private async Task SendStep2(PunchHoleStepModel model)
        {
            AddSendTimeout(model.RawData.FromId);
            bool res = await punchHoleMessengerSender.Request(new SendPunchHoleArg<PunchHoleStep2Info>
            {
                NewTunnel = model.RawData.NewTunnel,
                Connection = Connection,
                ToId = model.RawData.FromId,
                Data = new PunchHoleStep2Info { Step = (byte)PunchHoleTcpNutssBSteps.STEP_2, PunchType = PunchHoleTypes.TCP_NUTSSB }
            }).ConfigureAwait(false);
        }
        public async Task OnStep2(PunchHoleStepModel model)
        {
            await Task.Run(async () =>
            {
                RemoveSendTimeout(model.RawData.FromId);
                if (connectTcpCache.TryGetValue(model.RawData.FromId, out ConnectCacheModel cache) == false)
                {
                    Logger.Instance.Warning($"OnStep2 未找到缓存：{model.RawData.FromId}");
                    await SendStep2Fail(model.RawData.FromId, model.RawData.NewTunnel).ConfigureAwait(false);
                    return;
                }

                PunchHoleNotifyInfo data = model.Data as PunchHoleNotifyInfo;

                List<IPEndPoint> ips = new List<IPEndPoint>();
                if (UseLocalPort)
                {
                    ips.AddRange(data.LocalIps
                        .Where(c => c.Equals(IPAddress.Any) == false && (c.AddressFamily == AddressFamily.InterNetwork || c.IsIPv4MappedToIPv6))
                        .Select(c => new IPEndPoint(c, data.LocalPort)).ToList());
                    ips.AddRange(data.LocalIps
                        .Where(c => c.Equals(IPAddress.Any) == false && c.Equals(IPAddress.Loopback) == false && (c.AddressFamily == AddressFamily.InterNetwork || c.IsIPv4MappedToIPv6))
                        .Select(c => new IPEndPoint(c, data.Port)).ToList());
                }
                if (IPv6Support())
                {
                    ips.AddRange(data.LocalIps
                        .Where(c => NotIPv6Support(c) == false && c.AddressFamily == AddressFamily.InterNetworkV6 && c.IsIPv4MappedToIPv6 == false)
                        .Select(c => new IPEndPoint(c, data.Port)).ToList());
                }
                ips.Add(new IPEndPoint(data.Ip, data.Port));
                ips.Add(new IPEndPoint(data.Ip, data.Port + 1));

                if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    Logger.Instance.Debug($"尝试连接:{string.Join("\n", ips.Select(c => c.ToString()).ToArray())}");

                for (byte i = 0; i < ips.Count; i++)
                {
                    if (cache.Canceled)
                    {
                        break;
                    }

                    IPEndPoint ip = i >= ips.Count ? ips[^1] : ips[i];
                    AddressFamily family = ip.AddressFamily;
                    IPAddress bindIp = family == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any;
                    if (cache.LocalPort == signInState.LocalInfo.Port)
                    {
                        family = config.Client.BindIp.AddressFamily;
                        bindIp = config.Client.BindIp;
                    }
                    Socket targetSocket = new Socket(family, SocketType.Stream, ProtocolType.Tcp);

                    try
                    {
                        targetSocket.IPv6Only(family, false);
                        targetSocket.KeepAlive(time: config.Client.TimeoutDelay / 1000 / 5);
                        targetSocket.ReuseBind(new IPEndPoint(bindIp, cache.LocalPort));
                        if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            Logger.Instance.Debug($"tcp {ip} connect");
                        IAsyncResult result = targetSocket.BeginConnect(ip, null, null);
                        result.AsyncWaitHandle.WaitOne(ip.IsLan() ? 50 : 1000, false);

                        if (result.IsCompleted == false)
                        {
                            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                Logger.Instance.Error($"tcp {ip} connect fail");
                            targetSocket.SafeClose();
                            targetSocket = null;
                            continue;
                        }

                        try
                        {
                            targetSocket.EndConnect(result);
                        }
                        catch (Exception)
                        {
                            continue;
                        }

                        if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            Logger.Instance.Warning($"tcp {ip} connect success");
                        IConnection connection = tcpServer.BindReceive(targetSocket, bufferSize: (byte)config.Client.TcpBufferSize * 1024);
                        await CryptoSwap(connection);
                        await SendStep3(connection, model.RawData.FromId, model.RawData.NewTunnel);
                        cache.Success = true;
                        break;
                    }
                    catch (SocketException ex)
                    {
                        targetSocket.SafeClose();
                        targetSocket = null;

                        if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            Logger.Instance.Error($"tcp {ip} connect fail:{ex}");
                        if (ex.SocketErrorCode == SocketError.AddressNotAvailable)
                        {
                            if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                                Logger.Instance.Error($"{ex.SocketErrorCode}:{ip}");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                            Logger.Instance.Error($"tcp {ip} connect fail:{ex}");
                    }
                }

                if (cache.Success == false)
                {
                    await SendStep2Fail(model.RawData.FromId, model.RawData.NewTunnel).ConfigureAwait(false);
                }

            }).ConfigureAwait(false);
        }

        public async Task SendStep2Retry(ulong toid, byte newTunnel)
        {
            if (connectTcpCache.TryGetValue(toid, out ConnectCacheModel cache))
            {
                await punchHoleMessengerSender.Request(new SendPunchHoleArg<PunchHoleStep2TryInfo>
                {
                    NewTunnel = newTunnel,
                    Connection = Connection,
                    ToId = toid,
                    Data = new PunchHoleStep2TryInfo { Step = (byte)PunchHoleTcpNutssBSteps.STEP_2_TRY, PunchType = PunchHoleTypes.TCP_NUTSSB }
                }).ConfigureAwait(false);
                //Cancel(toid);
            }
        }
        public void OnStep2Retry(PunchHoleStepModel model)
        {
            if (connectTcpCache.TryGetValue(model.RawData.FromId, out ConnectCacheModel cache) == false || cache.Success)
            {
                return;
            }

            PunchHoleNotifyInfo data = model.Data as PunchHoleNotifyInfo;
            if (clientInfoCaching.GetTunnelPort(model.RawData.FromId, out int localPort))
            {
                List<IPEndPoint> ips = data.LocalIps.Where(c => c.Equals(IPAddress.Any) == false && c.Equals(IPAddress.IPv6Any) == false).Select(c => new IPEndPoint(c, data.LocalPort)).ToList();
                for (int i = 0; i <= 1; i++)
                {
                    if (data.Port + i < ushort.MaxValue)
                    {
                        ips.Add(new IPEndPoint(data.Ip, data.Port + i));
                    }
                }

                var sockets = ips.Where(c => NotIPv6Support(c.Address) == false).Select(ip =>
                {
                    using Socket targetSocket = new(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        targetSocket.IPv6Only(ip.Address.AddressFamily, false);
                        targetSocket.Ttl = (short)(RouteLevel);
                        targetSocket.ReuseBind(new IPEndPoint(ip.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, localPort));
                        _ = targetSocket.ConnectAsync(ip);
                        return targetSocket;
                    }
                    catch (Exception)
                    {
                    }
                    return null;
                });

                foreach (Socket item in sockets.Where(c => c != null))
                {
                    item.SafeClose();
                }
            }
        }

        private async Task SendStep2Fail(ulong toid, byte newTunnel)
        {
            await punchHoleMessengerSender.Request(new SendPunchHoleArg<PunchHoleStep2FailInfo>
            {
                NewTunnel = newTunnel,
                Connection = Connection,
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
            else
            {
                OnStepHandler?.Invoke(this, new PunchHoleStepModel
                {
                    Connection = Connection,
                    RawData = new PunchHoleRequestInfo
                    {
                        PunchStep = (byte)PunchHoleTcpNutssBSteps.STEP_2_FAIL,
                        FromId = toid,
                        NewTunnel = 0
                    }
                });
            }

        }
        public void OnStep2Fail(PunchHoleStepModel model)
        {
            RemoveSendTimeout(model.RawData.FromId);
        }
        public async Task SendStep2Stop(ulong toid, byte newTunnel)
        {
            if (connectTcpCache.TryGetValue(toid, out ConnectCacheModel cache))
            {
                await punchHoleMessengerSender.Request(new SendPunchHoleArg<PunchHoleStep2StopInfo>
                {
                    NewTunnel = newTunnel,
                    Connection = Connection,
                    ToId = toid,
                    Data = new PunchHoleStep2StopInfo { Step = (byte)PunchHoleTcpNutssBSteps.STEP_2_STOP, PunchType = PunchHoleTypes.TCP_NUTSSB }
                }).ConfigureAwait(false);
                Cancel(toid);
            }
        }
        public void OnStep2Stop(PunchHoleStepModel model)
        {
            Cancel(model.RawData.FromId);
        }

        private async Task SendStep3(IConnection connection, ulong toid, byte newTunnel)
        {
            AddSendTimeout(toid);
            await punchHoleMessengerSender.Request(new SendPunchHoleArg<PunchHoleStep3Info>
            {
                NewTunnel = newTunnel,
                Connection = connection,
                Data = new PunchHoleStep3Info
                {
                    Step = (byte)PunchHoleTcpNutssBSteps.STEP_3,
                    PunchType = PunchHoleTypes.TCP_NUTSSB
                }
            }).ConfigureAwait(false);
        }
        public async Task OnStep3(PunchHoleStepModel model)
        {
            RemoveSendTimeout(model.RawData.FromId);
            await SendStep4(model);
        }

        private async Task SendStep4(PunchHoleStepModel model)
        {
            await punchHoleMessengerSender.Request(new SendPunchHoleArg<PunchHoleStep4Info>
            {
                NewTunnel = model.RawData.NewTunnel,
                Connection = model.Connection,
                Data = new PunchHoleStep4Info
                {
                    Step = (byte)PunchHoleTcpNutssBSteps.STEP_4,
                    PunchType = PunchHoleTypes.TCP_NUTSSB
                }
            });
        }
        public void OnStep4(PunchHoleStepModel model)
        {
            RemoveSendTimeout(model.RawData.FromId);
            if (connectTcpCache.TryRemove(model.RawData.FromId, out ConnectCacheModel cache))
            {
                cache.Tcs.SetResult(new ConnectResultModel { State = true });
            }
        }

        private bool NotIPv6Support(IPAddress ip)
        {
            return ip.AddressFamily == AddressFamily.InterNetworkV6 && (NetworkHelper.IPv6Support == false || signInState.LocalInfo.Ipv6s.Length == 0);
        }
        private bool IPv6Support()
        {
            return NetworkHelper.IPv6Support == true && signInState.LocalInfo.Ipv6s.Length > 0;
        }


    }
}

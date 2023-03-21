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

        private int RouteLevel => signInState.LocalInfo.RouteLevel + 2;
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

            RemoveSendTimeout(model.RawData.FromId);
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
                List<IPEndPoint> ips = data.LocalIps.Where(c => c.Equals(IPAddress.Any) == false).Select(c => new IPEndPoint(c, data.LocalPort)).ToList();
                ips.Add(new IPEndPoint(data.Ip, data.Port));
                ips.Add(new IPEndPoint(data.Ip, data.Port + 1));

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
                if (connectTcpCache.TryGetValue(model.RawData.FromId, out ConnectCacheModel cache) == false)
                {
                    Logger.Instance.Warning($"OnStep2 未找到缓存：{model.RawData.FromId}");
                    await SendStep2Fail(model.RawData.FromId, model.RawData.NewTunnel).ConfigureAwait(false);
                    return;
                }


                bool success = false;
                List<IPEndPoint> ips = new List<IPEndPoint>();
                PunchHoleNotifyInfo data = model.Data as PunchHoleNotifyInfo;

                if (UseLocalPort)
                {
                    var locals = data.LocalIps.Where(c => c.Equals(IPAddress.Any) == false && c.AddressFamily == AddressFamily.InterNetwork).Select(c => new IPEndPoint(c, data.LocalPort)).ToList();
                    ips.AddRange(locals);
                }
                if (IPv6Support() && data.Ip.IsLan() == false)
                {
                    var locals = data.LocalIps.Where(c => c.AddressFamily == AddressFamily.InterNetworkV6).Select(c => new IPEndPoint(c, data.Port)).ToList();
                    ips.AddRange(locals);
                }
                ips.Add(new IPEndPoint(data.Ip, data.Port));
                ips.Add(new IPEndPoint(data.Ip, data.Port + 1));

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
                            await SendStep3(connection, model.RawData.FromId, model.RawData.NewTunnel);
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
                    await SendStep2Fail(model.RawData.FromId, model.RawData.NewTunnel).ConfigureAwait(false);
                }

            }).ConfigureAwait(false);
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
                if (NotIPv6Support(data.Ip))
                {
                    return;
                }
                Socket targetSocket = null;
                try
                {
                    if (data.Ip.Equals(IPAddress.Any) == false || data.Ip.Equals(IPAddress.IPv6Any))
                    {
                        IPEndPoint target = new IPEndPoint(data.Ip, data.Port);
                        targetSocket = new Socket(target.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                        targetSocket.IPv6Only(target.AddressFamily, false);
                        targetSocket.Ttl = (short)(RouteLevel);
                        targetSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        targetSocket.Bind(new IPEndPoint(data.Ip.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, localPort));
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

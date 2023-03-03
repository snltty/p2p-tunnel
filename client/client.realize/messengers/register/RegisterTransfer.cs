using client.messengers.clients;
using client.messengers.register;
using client.realize.messengers.crypto;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using common.server.servers.rudp;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace client.realize.messengers.register
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class RegisterTransfer : IRegisterTransfer
    {
        private readonly RegisterMessengerSender registerMessengerSender;
        private readonly ITcpServer tcpServer;
        private readonly IUdpServer udpServer;
        private readonly Config config;
        private readonly RegisterStateInfo registerState;
        private readonly CryptoSwap cryptoSwap;
        private readonly IIPv6AddressRequest iPv6AddressRequest;
        private CancellationTokenSource cancellationToken = null;
        private int lockObject = 0;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="registerMessengerSender"></param>
        /// <param name="clientInfoCaching"></param>
        /// <param name="tcpServer"></param>
        /// <param name="udpServer"></param>
        /// <param name="config"></param>
        /// <param name="registerState"></param>
        /// <param name="cryptoSwap"></param>
        /// <param name="iPv6AddressRequest"></param>
        public RegisterTransfer(
            RegisterMessengerSender registerMessengerSender, IClientInfoCaching clientInfoCaching,
            ITcpServer tcpServer, IUdpServer udpServer,
            Config config, RegisterStateInfo registerState,
            CryptoSwap cryptoSwap, IIPv6AddressRequest iPv6AddressRequest
        )
        {
            this.registerMessengerSender = registerMessengerSender;
            this.tcpServer = tcpServer;
            this.udpServer = udpServer;
            this.config = config;
            this.registerState = registerState;
            this.cryptoSwap = cryptoSwap;
            this.iPv6AddressRequest = iPv6AddressRequest;

            AppDomain.CurrentDomain.ProcessExit += (s, e) => Exit();

            tcpServer.OnDisconnect.Sub((connection) => Disconnect(connection, registerState.TcpConnection));
            udpServer.OnDisconnect.Sub((connection) => Disconnect(connection, registerState.UdpConnection));
        }
        private void Disconnect(IConnection connection, IConnection regConnection)
        {
            if (regConnection == null || ReferenceEquals(regConnection, connection) == false || registerState.LocalInfo.IsConnecting)
            {
                return;
            }

            Logger.Instance.Error($"{connection.ServerType} register 断开~~~~${connection.Address}");
            //if (Interlocked.CompareExchange(ref lockObject, 1, 0) == 0)
            //{
            //    Register(true).ContinueWith((result) =>
            //    {
            //        Interlocked.Exchange(ref lockObject, 0);
            //    });
            //}
        }

        /// <summary>
        /// 
        /// </summary>
        public void Exit()
        {
            if (cancellationToken != null && cancellationToken.IsCancellationRequested == false)
            {
                cancellationToken.Cancel();
            }
            Exit1();
        }
        private void Exit1()
        {
            registerState.Offline();
            udpServer.Stop();
            tcpServer.Stop();
            GCHelper.FlushMemory();
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="autoReg">强行自动注册</param>
        /// <returns></returns>
        public async Task<CommonTaskResponseInfo<bool>> Register(bool autoReg = false)
        {
            cancellationToken = new CancellationTokenSource();
            CommonTaskResponseInfo<bool> success = new CommonTaskResponseInfo<bool> { Data = false, ErrorMsg = string.Empty };
            if (registerState.LocalInfo.IsConnecting)
            {
                success.ErrorMsg = "注册操作中...";
                return success;
            }
            if (config.Client.UseUdp == false && config.Client.UseTcp == false)
            {
                success.ErrorMsg = "udp tcp至少要启用一种...";
                return success;
            }

            return await Task.Run(async () =>
            {
                double interval = autoReg ? 5000 : 0;
                int times = autoReg ? 10000 : 2;
                for (int i = 0; i < times; i++)
                {
                    bool isex = false;
                    try
                    {
                        if (registerState.LocalInfo.IsConnecting)
                        {
                            success.ErrorMsg = "注册操作中...";
                            Logger.Instance.Error(success.ErrorMsg);
                            break;
                        }

                        //先退出
                        Exit1();

                        Logger.Instance.Info($"开始注册");
                        registerState.LocalInfo.IsConnecting = true;
                        if (interval > 0)
                        {
                            await Task.Delay((int)interval, cancellationToken.Token);
                        }

                        IPAddress serverAddress = NetworkHelper.GetDomainIp(config.Server.Ip);
                        config.Client.UseIpv6 = serverAddress.AddressFamily == AddressFamily.InterNetworkV6;
                        registerState.LocalInfo.UdpPort = registerState.LocalInfo.TcpPort = NetworkHelper.GetRandomPort();
                        registerState.OnRegisterBind.Push(true);

                        if (config.Client.UseUdp)
                        {
                            //绑定udp
                            await UdpBind(serverAddress);
                            if (registerState.UdpConnection == null)
                            {
                                registerState.LocalInfo.IsConnecting = false;
                                success.ErrorMsg = "udp连接失败";
                                Logger.Instance.Error(success.ErrorMsg);
                                continue;
                            }
                        }
                        if (config.Client.UseTcp)
                        {
                            //绑定tcp
                            TcpBind(serverAddress);
                        }
                        //交换密钥
                        if (config.Server.Encode)
                        {
                            await SwapCryptoTcp();
                        }

                        //注册
                        RegisterResult result = await GetRegisterResult();
                        config.Client.ShortId = result.Data.ShortId;
                        config.Client.GroupId = result.Data.GroupId;
                        registerState.RemoteInfo.Relay = result.Data.Relay;
                        registerState.Online(result.Data.Id, result.Data.Ip, result.Data.UdpPort, result.Data.TcpPort);
                        await registerMessengerSender.Notify().ConfigureAwait(false);

                        success.ErrorMsg = "注册成功~";
                        success.Data = true;
                        Logger.Instance.Debug(success.ErrorMsg);
                        break;
                    }
                    catch (TaskCanceledException tex)
                    {
                        Logger.Instance.Error(tex.Message);
                        success.ErrorMsg = tex.Message;
                        isex = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error(ex.Message);
                        success.ErrorMsg = ex.Message;
                    }

                    registerState.LocalInfo.IsConnecting = false;
                    if ((config.Client.AutoReg || autoReg) && isex == false)
                    {
                        interval *= 0.1;
                    }
                    else
                    {
                        break;
                    }
                }
                return success;
            });
        }
        private async Task UdpBind(IPAddress serverAddress)
        {
            //UDP 开始监听
            udpServer.Start(registerState.LocalInfo.UdpPort, config.Client.TimeoutDelay);
            if (udpServer is UdpServer udp)
            {
                udp.SetSpeedLimit(config.Client.UdpUploadSpeedLimit);
            }
            registerState.UdpConnection = await udpServer.CreateConnection(new IPEndPoint(serverAddress, config.Server.UdpPort));
        }
        private void TcpBind(IPAddress serverAddress)
        {
            //TCP 本地开始监听
            tcpServer.SetBufferSize(config.Client.TcpBufferSize);
            tcpServer.Start(registerState.LocalInfo.TcpPort);

            //TCP 连接服务器
            IPEndPoint bindEndpoint = new IPEndPoint(config.Client.BindIp, registerState.LocalInfo.TcpPort);
            Socket tcpSocket = new(bindEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            tcpSocket.KeepAlive(time: config.Client.TimeoutDelay / 1000 / 5);
            tcpSocket.IPv6Only(config.Client.BindIp.AddressFamily, false);
            tcpSocket.ReuseBind(bindEndpoint);
            tcpSocket.Connect(new IPEndPoint(serverAddress, config.Server.TcpPort));
            registerState.LocalInfo.LocalIp = (tcpSocket.LocalEndPoint as IPEndPoint).Address;
            registerState.TcpConnection = tcpServer.BindReceive(tcpSocket, config.Client.TcpBufferSize);
        }
        private async Task SwapCryptoTcp()
        {
            ICrypto crypto = await cryptoSwap.Swap(registerState.TcpConnection, registerState.UdpConnection, config.Server.EncodePassword);
            if (crypto == null)
            {
                throw new Exception("注册交换密钥失败，如果客户端设置了密钥，则服务器必须设置相同的密钥，如果服务器未设置密钥，则客户端必须留空");
            }

            registerState.TcpConnection?.EncodeEnable(crypto);
            registerState.UdpConnection?.EncodeEnable(crypto);

#if DEBUG
            await cryptoSwap.Test(registerState.OnlineConnection);
#endif
        }
        private async Task<RegisterResult> GetRegisterResult()
        {
            IPAddress[] localIps = new IPAddress[] { config.Client.LoopbackIp, registerState.LocalInfo.LocalIp };
            registerState.LocalInfo.Ipv6s = iPv6AddressRequest.GetIPV6();
            localIps = localIps.Concat(registerState.LocalInfo.Ipv6s).ToArray();

            RegisterResult result;
            do
            {
                result = await registerMessengerSender.Register(new RegisterParams
                {
                    ShortId = config.Client.ShortId,
                    ClientName = config.Client.Name,
                    GroupId = config.Client.GroupId,
                    LocalUdpPort = registerState.LocalInfo.UdpPort,
                    LocalTcpPort = registerState.LocalInfo.TcpPort,
                    LocalIps = localIps,
                    Timeout = 15 * 1000,
                    ClientAccess = config.Client.GetAccess()
                }).ConfigureAwait(false);

                if (result.Data.Code == RegisterResultInfo.RegisterResultInfoCodes.SAME_NAMES)
                {
                    await Task.Delay(1000);
                }

            } while (result.Data.Code == RegisterResultInfo.RegisterResultInfoCodes.SAME_NAMES);

            if (result.NetState.Code != MessageResponeCodes.OK)
            {
                throw new Exception($"注册失败，网络问题:{result.NetState.Code.GetDesc((byte)result.NetState.Code)}");
            }
            if (result.Data.Code != RegisterResultInfo.RegisterResultInfoCodes.OK)
            {
                throw new Exception($"注册失败:{result.Data.Code.GetDesc((byte)result.Data.Code)}");
            }
            return result;
        }

    }
}

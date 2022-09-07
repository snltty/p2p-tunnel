using client.messengers.register;
using client.realize.messengers.crypto;
using client.realize.messengers.heart;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.realize.messengers.register
{
    public class RegisterTransfer : IRegisterTransfer
    {
        private readonly RegisterMessengerSender registerMessageHelper;
        private readonly ITcpServer tcpServer;
        private readonly IUdpServer udpServer;
        private readonly Config config;
        private readonly RegisterStateInfo registerState;
        private readonly HeartMessengerSender heartMessengerSender;
        private readonly CryptoSwap cryptoSwap;
        private readonly object lockObject = new();

        public RegisterTransfer(
            RegisterMessengerSender registerMessageHelper, HeartMessengerSender heartMessengerSender,
            ITcpServer tcpServer, IUdpServer udpServer,
            Config config, RegisterStateInfo registerState,
            CryptoSwap cryptoSwap, WheelTimer<object> wheelTimer
        )
        {
            this.registerMessageHelper = registerMessageHelper;
            this.tcpServer = tcpServer;
            this.udpServer = udpServer;
            this.config = config;
            this.registerState = registerState;
            this.heartMessengerSender = heartMessengerSender;
            this.cryptoSwap = cryptoSwap;
            wheelTimer.NewTimeout(new WheelTimerTimeoutTask<object> { Callback = Heart }, 1000, true);

            AppDomain.CurrentDomain.ProcessExit += (s, e) => Exit();
            //安卓注释
            //Console.CancelKeyPress += (s, e) => Exit();

            tcpServer.OnDisconnect.Sub((connection) => Disconnect(connection, registerState.TcpConnection));
            udpServer.OnDisconnect.Sub((connection) => Disconnect(connection, registerState.UdpConnection));
        }
        private void Disconnect(IConnection connection, IConnection regConnection)
        {
            lock (lockObject)
            {
                if (regConnection != connection)
                {
                    return;
                }
                if (registerState.LocalInfo.IsConnecting)
                {
                    return;
                }
                _ = Register(true).Result;
            }
        }

        public void Exit()
        {
            registerMessageHelper.Exit().Wait();
            registerState.Offline();
            udpServer.Stop();
            tcpServer.Stop();
            GCHelper.FlushMemory();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="autoReg">强行自动注册</param>
        /// <returns></returns>
        public async Task<CommonTaskResponseInfo<bool>> Register(bool autoReg = false)
        {
            CommonTaskResponseInfo<bool> success = new CommonTaskResponseInfo<bool> { Data = false, ErrorMsg = string.Empty };
            if (registerState.LocalInfo.IsConnecting)
            {
                success.ErrorMsg = "注册操作中...";
                return success;
            }
            if (!config.Client.UseUdp && !config.Client.UseTcp)
            {
                success.ErrorMsg = "udp tcp至少要启用一种...";
                return success;
            }

            return await Task.Run(async () =>
            {

                int interval = autoReg ? config.Client.AutoRegDelay : 0;

                for (int i = 0; i < config.Client.AutoRegTimes; i++)
                {
                    try
                    {
                        if (registerState.LocalInfo.IsConnecting)
                        {
                            success.ErrorMsg = "注册操作中...";
                            break;
                        }

                        //先退出
                        Exit();
                        Logger.Instance.Info($"开始注册");

                        registerState.LocalInfo.IsConnecting = true;

                        if (interval > 0)
                        {
                            await Task.Delay(interval);
                        }

                        IPAddress serverAddress = NetworkHelper.GetDomainIp(config.Server.Ip);
                        registerState.LocalInfo.UdpPort = registerState.LocalInfo.TcpPort = NetworkHelper.GetRandomPort();
                        // registerState.LocalInfo.UdpPort = NetworkHelper.GetRandomPort(new System.Collections.Generic.List<int> { registerState.LocalInfo.TcpPort });
                        registerState.LocalInfo.Mac = string.Empty;

                        if (config.Client.UseUdp)
                        {
                            //绑定udp
                            await UdpBind(serverAddress);
                            if (registerState.UdpConnection == null)
                            {
                                success.ErrorMsg = "udp连接失败";
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
                        //上线
                        config.Client.GroupId = result.Data.GroupId;
                        registerState.RemoteInfo.TimeoutDelay = result.Data.TimeoutDelay;
                        registerState.RemoteInfo.Relay = result.Data.Relay;
                        registerState.Online(result.Data.Id, result.Data.Ip, result.Data.UdpPort, result.Data.TcpPort);
                        //上线通知
                        await registerMessageHelper.Notify().ConfigureAwait(false);

                        success.ErrorMsg = "注册成功~";
                        success.Data = true;
                        Logger.Instance.Debug(success.ErrorMsg);
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.DebugError(ex);
                        success.ErrorMsg = ex.Message;
                    }

                    Logger.Instance.Error(success.ErrorMsg);
                    registerState.LocalInfo.IsConnecting = false;
                    if (config.Client.AutoReg || autoReg)
                    {
                        interval = config.Client.AutoRegInterval;
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
            udpServer.Start(registerState.LocalInfo.UdpPort, config.Client.BindIp);
            registerState.UdpConnection = await udpServer.CreateConnection(new IPEndPoint(serverAddress, config.Server.UdpPort));
        }
        private void TcpBind(IPAddress serverAddress)
        {
            //TCP 本地开始监听
            tcpServer.SetBufferSize(config.Client.TcpBufferSize);
            tcpServer.Start(registerState.LocalInfo.TcpPort, config.Client.BindIp);
            //TCP 连接服务器
            IPEndPoint bindEndpoint = new IPEndPoint(config.Client.BindIp, registerState.LocalInfo.TcpPort);
            Socket tcpSocket = new(bindEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            tcpSocket.KeepAlive();
            tcpSocket.ReuseBind(bindEndpoint);
            tcpSocket.Connect(new IPEndPoint(serverAddress, config.Server.TcpPort));
            registerState.LocalInfo.LocalIp = (tcpSocket.LocalEndPoint as IPEndPoint).Address;
            if (config.Client.UseMac)
            {
                //registerState.LocalInfo.Mac = NetworkHelper.GetMacAddress(registerState.LocalInfo.LocalIp.ToString());
            }
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
            //注册
            RegisterResult result = await registerMessageHelper.Register(new RegisterParams
            {
                ClientName = config.Client.Name,
                GroupId = config.Client.GroupId,
                LocalUdpPort = registerState.LocalInfo.UdpPort,
                LocalTcpPort = registerState.LocalInfo.TcpPort,
                Mac = registerState.LocalInfo.Mac,
                LocalIps = new IPAddress[] { config.Client.LoopbackIp, registerState.LocalInfo.LocalIp },
                Key = config.Client.Key,
                Timeout = 15 * 1000
            }).ConfigureAwait(false);
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

        private void Heart(object state)
        {
            lock (lockObject)
            {
                if (!registerState.LocalInfo.IsConnecting)
                {
                    long time = DateTimeHelper.GetTimeStamp();
                    if (registerState.UdpOnline)
                    {
                        if (registerState.UdpConnection.IsTimeout(time, registerState.RemoteInfo.TimeoutDelay))
                        {
                            Exit();
                            return;
                        }
                        else if (registerState.UdpConnection.IsNeedHeart(time, registerState.RemoteInfo.TimeoutDelay))
                        {
                            _ = heartMessengerSender.Heart(registerState.UdpConnection);
                        }
                    }
                    if (registerState.TcpOnline)
                    {
                        if (registerState.TcpConnection.IsTimeout(time, registerState.RemoteInfo.TimeoutDelay))
                        {
                            Exit();
                        }
                        else if (registerState.TcpConnection.IsNeedHeart(time, registerState.RemoteInfo.TimeoutDelay))
                        {
                            _ = heartMessengerSender.Heart(registerState.TcpConnection);
                        }
                    }
                }
            }
        }
    }
}

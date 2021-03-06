using client.messengers.register;
using client.service.messengers.crypto;
using client.service.messengers.heart;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.messengers.register
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
            wheelTimer.NewTimeout(new WheelTimerTimeoutTask<object> { Callback = Heart }, 5000, true);

            AppDomain.CurrentDomain.ProcessExit += (s, e) => Exit();
            Console.CancelKeyPress += (s, e) => Exit();
            tcpServer.OnDisconnect.Sub((IConnection connection) =>
            {
                if (registerState.TcpConnection != null && registerState.TcpConnection.ConnectId == connection.ConnectId)
                {
                    _ = Register(true);
                }
            });
        }

        public void Exit()
        {
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
            return await Task.Run(async () =>
            {
                CommonTaskResponseInfo<bool> success = new CommonTaskResponseInfo<bool> { Data = false, ErrorMsg = string.Empty };
                int interval = 0;
                for (int i = 0; i < config.Client.AutoRegTimes; i++)
                {
                    try
                    {
                        if (registerState.LocalInfo.IsConnecting)
                        {
                            success.ErrorMsg = "注册操作中...";
                            break;
                        }
                        if (interval > 0)
                        {
                            await Task.Delay(interval);
                            interval = 0;
                        }

                        //先退出
                        Exit();

                        IPAddress serverAddress = NetworkHelper.GetDomainIp(config.Server.Ip);
                        registerState.LocalInfo.IsConnecting = true;
                        registerState.LocalInfo.TcpPort = registerState.LocalInfo.UdpPort = NetworkHelper.GetRandomPort();
                        registerState.LocalInfo.Mac = string.Empty;

                        //绑定udp
                        UdpBind(serverAddress);
                        if (registerState.UdpConnection == null)
                        {
                            success.ErrorMsg = "udp连接失败";
                        }
                        else
                        {
                            //绑定tcp
                            TcpBind(serverAddress);

                            //交换密钥
                            if (config.Server.Encode)
                            {
                                await SwapCryptoTcp();
                            }

                            //注册
                            RegisterResult result = await GetRegisterResult();
                            //上线
                            config.Client.GroupId = result.Data.GroupId;
                            registerState.RemoteInfo.Relay = result.Data.Relay;
                            registerState.Online(result.Data.Id, result.Data.Ip, result.Data.Port, result.Data.TcpPort);
                            //上线通知
                            await registerMessageHelper.Notify().ConfigureAwait(false);

                            success.ErrorMsg = "注册成功~";
                            success.Data = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        success.ErrorMsg = ex.Message;
                    }

                    if (!success.Data)
                    {
                        registerState.LocalInfo.IsConnecting = false;
                        Logger.Instance.Error(success.ErrorMsg);
                        if (config.Client.AutoReg || autoReg)
                        {
                            interval = 5000;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        Logger.Instance.Debug(success.ErrorMsg);
                        break;
                    }
                }
                return success;
            });
        }
        private void UdpBind(IPAddress serverAddress)
        {
            //UDP 开始监听
            udpServer.Start(registerState.LocalInfo.UdpPort, config.Client.BindIp);
            registerState.UdpConnection = udpServer.CreateConnection(new IPEndPoint(serverAddress, config.Server.UdpPort));
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
#if DEBUG
            registerState.LocalInfo.LocalIp = (tcpSocket.LocalEndPoint as IPEndPoint).Address;
#endif
            if (config.Client.UseMac)
            {
                registerState.LocalInfo.Mac = NetworkHelper.GetMacAddress(registerState.LocalInfo.LocalIp.ToString());
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
            registerState.TcpConnection.EncodeEnable(crypto);
            registerState.UdpConnection.EncodeEnable(crypto);

#if DEBUG
            await cryptoSwap.Test(registerState.TcpConnection);
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
            if (registerState.UdpConnection != null && registerState.UdpConnection.IsNeedHeart(DateTimeHelper.GetTimeStamp()))
            {
                _ = heartMessengerSender.Heart(registerState.UdpConnection);
            }
        }
    }
}

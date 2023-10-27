using client.messengers.signin;
using client.realize.messengers.crypto;
using client.realize.messengers.heart;
using common.libs;
using common.libs.extends;
using common.server;
using common.server.model;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace client.realize.messengers.signin
{
    public sealed class SignInTransfer : ISignInTransfer
    {
        private readonly SignInMessengerSender signinMessengerSender;
        private readonly ITcpServer tcpServer;
        private readonly IUdpServer udpServer;
        private readonly Config config;
        private readonly SignInStateInfo signInState;
        private readonly CryptoSwap cryptoSwap;
        private readonly IIPv6AddressRequest iPv6AddressRequest;
        private CancellationTokenSource cancellationToken = null;
        private readonly HeartMessengerSender heartMessengerSender;

        public SignInTransfer(
            SignInMessengerSender signinMessengerSender,
            ITcpServer tcpServer, IUdpServer udpServer,
            Config config, SignInStateInfo signInState,
            CryptoSwap cryptoSwap, IIPv6AddressRequest iPv6AddressRequest, HeartMessengerSender heartMessengerSender
        )
        {
            this.signinMessengerSender = signinMessengerSender;
            this.tcpServer = tcpServer;
            this.udpServer = udpServer;
            this.config = config;
            this.signInState = signInState;
            this.cryptoSwap = cryptoSwap;
            this.iPv6AddressRequest = iPv6AddressRequest;
            this.heartMessengerSender = heartMessengerSender;

            AppDomain.CurrentDomain.ProcessExit += (s, e) => Exit();
            Console.CancelKeyPress += (s, e) => Exit();

            SignInTask();
        }

        private void SignInTask()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    bool alive = await heartMessengerSender.Alive(signInState.Connection);
                    if (alive == false && signInState.LocalInfo.IsConnecting == false && config.Client.AutoReg)
                    {
                        await SignIn();
                    }
                    await Task.Delay(10000);
                }

            }, TaskCreationOptions.LongRunning);
        }

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
            signInState.Offline();
            udpServer.Stop();
            tcpServer.Stop();
            GCHelper.FlushMemory();
        }

        public async Task<CommonTaskResponseInfo<bool>> SignIn()
        {
            cancellationToken = new CancellationTokenSource();
            CommonTaskResponseInfo<bool> success = new CommonTaskResponseInfo<bool> { Data = false, ErrorMsg = string.Empty };

            try
            {
                //先退出
                Exit1();

                Logger.Instance.Info($"开始登入");
                signInState.LocalInfo.IsConnecting = true;

                IPAddress serverAddress = NetworkHelper.GetDomainIp(config.Server.Ip);
                signInState.LocalInfo.Port = NetworkHelper.GetRandomPort();
                config.Client.UseIpv6 = NetworkHelper.IPv6Support;
                signInState.LocalInfo.Ipv6s = iPv6AddressRequest.GetIPV6();


                TcpBind(serverAddress);
                //交换密钥
                await SwapCryptoTcp();

                //登入
                SignInResult result = await signinMessengerSender.SignIn().ConfigureAwait(false);
                if (result.NetState.Code != MessageResponeCodes.OK)
                {
                    throw new Exception($"登入失败，网络问题:{result.NetState.Code.GetDesc((byte)result.NetState.Code)}");
                }
                if (result.Data.Code != SignInResultInfo.SignInResultInfoCodes.OK)
                {
                    throw new Exception($"登入失败:{result.Data.Code.GetDesc((byte)result.Data.Code)}");
                }


                config.Client.ShortId = result.Data.ShortId;
                config.Client.GroupId = result.Data.GroupId;
                config.Client.ConnectId = result.Data.ConnectionId;
                signInState.RemoteInfo.Access = result.Data.UserAccess;
                signInState.Online(result.Data.ConnectionId, result.Data.Ip);

                await config.SaveConfig(config);
                await signinMessengerSender.Notify().ConfigureAwait(false);

                success.ErrorMsg = "登入成功~";
                success.Data = true;
                Logger.Instance.Debug(success.ErrorMsg);
            }
            catch (TaskCanceledException tex)
            {
                success.ErrorMsg = tex.Message;
                signInState.LocalInfo.IsConnecting = false;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
                success.ErrorMsg = ex.Message;
                signInState.LocalInfo.IsConnecting = false;
            }


            return success;
        }
        private void TcpBind(IPAddress serverAddress)
        {
            signInState.OnBind?.Invoke(true);
            //TCP 本地开始监听
            tcpServer.SetBufferSize((1 << (byte)config.Client.TcpBufferSize) * 1024);
            tcpServer.Start(signInState.LocalInfo.Port);

            //TCP 连接服务器
            IPEndPoint bindEndpoint = new IPEndPoint(config.Client.BindIp, signInState.LocalInfo.Port);
            Socket tcpSocket = new(bindEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            tcpSocket.KeepAlive(time: config.Client.TimeoutDelay / 1000 / 5);
            tcpSocket.IPv6Only(config.Client.BindIp.AddressFamily, false);
            tcpSocket.ReuseBind(bindEndpoint);
            tcpSocket.Connect(new IPEndPoint(serverAddress, config.Server.TcpPort));
            signInState.LocalInfo.LocalIp = (tcpSocket.LocalEndPoint as IPEndPoint).Address;
            signInState.Connection = tcpServer.BindReceive(tcpSocket, (1 << (byte)config.Client.TcpBufferSize) * 1024);
        }
        private async Task SwapCryptoTcp()
        {
            if (config.Server.Encode == false)
            {
                return;
            }
            ICrypto crypto = await cryptoSwap.Swap(signInState.Connection, config.Server.EncodePassword);
            if (crypto == null)
            {
                throw new Exception("登入交换密钥失败，如果客户端设置了密钥，则服务器必须设置相同的密钥，如果服务器未设置密钥，则客户端必须留空");
            }

            signInState.Connection?.EncodeEnable(crypto);

#if DEBUG
            await cryptoSwap.Test(signInState.Connection);
#endif
        }
    }
}

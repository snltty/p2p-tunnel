using common.libs;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace common.socks5
{
    /// <summary>
    /// socks5客户端
    /// </summary>
    public class Socks5ClientHandler : ISocks5ClientHandler
    {
        private readonly ISocks5MessengerSender socks5MessengerSender;
        private readonly ISocks5ClientListener socks5ClientListener;
        private readonly ISocks5DstEndpointProvider socks5DstEndpointProvider;

        protected Dictionary<Socks5EnumStep, Func<Socks5Info, Task<bool>>> handles = new Dictionary<Socks5EnumStep, Func<Socks5Info, Task<bool>>>();
        protected Dictionary<Socks5EnumStep, Action<Socks5Info>> buildHandles = new Dictionary<Socks5EnumStep, Action<Socks5Info>>();

        public Socks5ClientHandler(ISocks5MessengerSender socks5MessengerSender, ISocks5DstEndpointProvider socks5DstEndpointProvider, ISocks5ClientListener socks5ClientListener)
        {
            this.socks5MessengerSender = socks5MessengerSender;
            this.socks5DstEndpointProvider = socks5DstEndpointProvider;
            this.socks5ClientListener = socks5ClientListener;

            socks5ClientListener.OnData = OnData;
            socks5ClientListener.OnClose = OnClose;

            handles = new Dictionary<Socks5EnumStep, Func<Socks5Info, Task<bool>>>
            {
                { Socks5EnumStep.Request,HandleRequest},
                { Socks5EnumStep.Auth,HandleAuth},
                { Socks5EnumStep.Command,HandleCommand},
                { Socks5EnumStep.Forward,HndleForward},
                { Socks5EnumStep.ForwardUdp,HndleForwardUdp},
            };
            buildHandles = new Dictionary<Socks5EnumStep, Action<Socks5Info>> {
                {Socks5EnumStep.Request, RequestResponseData},
                {Socks5EnumStep.Auth, AuthResponseData},
                {Socks5EnumStep.Command, CommandResponseData},
                {Socks5EnumStep.Forward, ForwardResponseData},
                {Socks5EnumStep.ForwardUdp, ForwardUdpResponseData},
            };

        }
        /// <summary>
        /// 输入数据
        /// </summary>
        /// <param name="connection"></param>
        public async Task InputData(Socks5Info info)
        {
            if (info.Data.Length == 0)
            {
                socks5ClientListener.Close(info.Id);
            }
            else
            {
                if (buildHandles.TryGetValue(info.Socks5Step, out Action<Socks5Info> func))
                {
                    func(info);
                    await socks5ClientListener.Response(info);
                }
            }
        }

        /// <summary>
        /// 接收到数据
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        protected async Task<bool> OnData(Socks5Info info)
        {
            if (handles.TryGetValue(info.Socks5Step, out Func<Socks5Info, Task<bool>> func))
            {
                if (info.Socks5Step == Socks5EnumStep.Auth)
                {
                    info.Version = info.Data.Span[0];
                }
                return await func(info);
            }
            return false;
        }

        /// <summary>
        /// 收到关闭
        /// </summary>
        /// <param name="info"></param>
        protected virtual void OnClose(Socks5Info info)
        {
            socks5MessengerSender.RequestClose(info);
        }
        /// <summary>
        /// 构建request回复数据
        /// </summary>
        /// <param name="info"></param>
        protected void RequestResponseData(Socks5Info info)
        {
            Socks5EnumAuthType type = (Socks5EnumAuthType)info.Data.Span[0];
            if (type == Socks5EnumAuthType.NotSupported)
            {
                info.Data = Helper.EmptyArray;
            }
            else
            {
                if (type == Socks5EnumAuthType.NoAuth)
                {
                    info.Socks5Step = Socks5EnumStep.Command;
                }
                else
                {
                    info.Socks5Step = Socks5EnumStep.Auth;
                }
                info.Data = new byte[] { 5, (byte)type };
            }
        }
        /// <summary>
        /// 构建验证response数据
        /// </summary>
        /// <param name="info"></param>
        protected void AuthResponseData(Socks5Info info)
        {
            Socks5EnumAuthState type = (Socks5EnumAuthState)info.Data.Span[0];
            if (type != Socks5EnumAuthState.Success)
            {
                info.Data = Helper.EmptyArray;
            }
            else
            {
                if (type == Socks5EnumAuthState.Success)
                {
                    info.Socks5Step = Socks5EnumStep.Command;
                }
                info.Data = new byte[] { info.Version, (byte)type };
            }
        }

        /// <summary>
        /// 构建命令response数据
        /// </summary>
        /// <param name="info"></param>
        protected void CommandResponseData(Socks5Info info)
        {
            Socks5EnumResponseCommand type = (Socks5EnumResponseCommand)info.Data.Span[0];
            if (type != Socks5EnumResponseCommand.ConnecSuccess)
            {
                info.Data = Helper.EmptyArray;
            }
            else
            {
                info.Socks5Step = Socks5EnumStep.Forward;
                IPEndPoint endpoint = socks5DstEndpointProvider.Get(socks5ClientListener.Port);
                info.Data = Socks5Parser.MakeConnectResponse(endpoint, (byte)type);
            }
        }
        /// <summary>
        /// 构建转发回复数据
        /// </summary>
        /// <param name="info"></param>
        protected void ForwardResponseData(Socks5Info info)
        {
            info.Socks5Step = Socks5EnumStep.Forward;
        }
        /// <summary>
        /// 构建udp转发回复数据
        /// </summary>
        /// <param name="info"></param>
        protected void ForwardUdpResponseData(Socks5Info info)
        {
            info.Data = Socks5Parser.MakeUdpResponse(info.TargetEP, info.Data);
        }


        /// <summary>
        /// 收到request
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual async Task<bool> HandleRequest(Socks5Info data)
        {
            data.AuthType = Socks5EnumAuthType.NoAuth;
            data.Response[0] = (byte)data.AuthType;
            data.Data = data.Response;
            RequestResponseData(data);
            await socks5ClientListener.Response(data);
            return true;
        }
        /// <summary>
        /// 收到auth
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual async Task<bool> HandleAuth(Socks5Info data)
        {
            data.Response[0] = (byte)Socks5EnumAuthState.Success;
            data.Data = data.Response;
            AuthResponseData(data);
            await socks5ClientListener.Response(data);
            return true;
        }
        /// <summary>
        /// 收到command
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual async Task<bool> HandleCommand(Socks5Info data)
        {
            /*
            data.Response[0] = (byte)Socks5EnumResponseCommand.ConnecSuccess;
            data.Data = data.Response;
            CommandResponseData(data);
            await socks5ClientListener.Response(data);
            return true;
            */
            //0.0.0.0  或者:0 都直接通过，这是udp中继的时候可能出现的情况
            if (Socks5Parser.GetIsAnyAddress(data.Data))
            {
                data.Response[0] = (byte)Socks5EnumResponseCommand.ConnecSuccess;
                data.Data = data.Response;
                CommandResponseData(data);
                await socks5ClientListener.Response(data);
                return true;
            }
            return await socks5MessengerSender.Request(data);
        }


        /// <summary>
        /// 收到转发
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual async Task<bool> HndleForward(Socks5Info data)
        {
            return await socks5MessengerSender.Request(data);
        }
        /// <summary>
        /// 收到udp转发
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual async Task<bool> HndleForwardUdp(Socks5Info data)
        {
            //return true;
            return await socks5MessengerSender.Request(data);
        }

    }
}

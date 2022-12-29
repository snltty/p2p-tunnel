using client.messengers.clients;
using client.messengers.register;
using common.libs;
using common.server;
using common.socks5;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace client.service.socks5
{
    /// <summary>
    /// socks5客户端
    /// </summary>
    public class Socks5ClientHandler : ISocks5ClientHandler
    {
        private readonly ISocks5MessengerSender socks5MessengerSender;
        private readonly ISocks5ClientListener socks5ClientListener;
        private readonly RegisterStateInfo registerStateInfo;
        private readonly common.socks5.Config config;
        private IConnection connection;
        private IClientInfoCaching clientInfoCaching;

        /// <summary>
        /// 
        /// </summary>
        protected Dictionary<Socks5EnumStep, Func<Socks5Info, bool>> handles = new Dictionary<Socks5EnumStep, Func<Socks5Info, bool>>();
        /// <summary>
        /// 
        /// </summary>
        protected Dictionary<Socks5EnumStep, Action<Socks5Info>> buildHandles = new Dictionary<Socks5EnumStep, Action<Socks5Info>>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="socks5MessengerSender"></param>
        /// <param name="registerStateInfo"></param>
        /// <param name="config"></param>
        /// <param name="clientInfoCaching"></param>
        /// <param name="socks5ClientListener"></param>
        public Socks5ClientHandler(ISocks5MessengerSender socks5MessengerSender, RegisterStateInfo registerStateInfo, common.socks5.Config config, IClientInfoCaching clientInfoCaching, ISocks5ClientListener socks5ClientListener)
        {
            this.socks5MessengerSender = socks5MessengerSender;
            this.registerStateInfo = registerStateInfo;
            this.config = config;
            this.clientInfoCaching = clientInfoCaching;
            this.socks5ClientListener = socks5ClientListener;

            socks5ClientListener.OnData = OnData;
            socks5ClientListener.OnClose = OnClose;

            handles = new Dictionary<Socks5EnumStep, Func<Socks5Info, bool>>
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
        public void InputData(IConnection connection)
        {
            Socks5Info info = Socks5Info.Debytes(connection.ReceiveRequestWrap.Payload);
            if (info.Data.Length == 0)
            {
                socks5ClientListener.Close(info.Id);
            }
            else
            {
                if (buildHandles.TryGetValue(info.Socks5Step, out Action<Socks5Info> func))
                {
                    func(info);
                    socks5ClientListener.Response(info);
                }
            }
        }

        /// <summary>
        /// 接收到数据
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        protected bool OnData(Socks5Info info)
        {
            if (handles.TryGetValue(info.Socks5Step, out Func<Socks5Info, bool> func))
            {
                //验证一下数据包是否已经完整，不完整的，等待下次数据继续拼接
                switch (info.Socks5Step)
                {
                    case Socks5EnumStep.Request:
                        if (ValidateRequestData(info) == false) return true;
                        break;
                    case Socks5EnumStep.Auth:
                        info.Version = info.Data.Span[0];
                        if (ValidateAuthData(info) == false) return true;
                        break;
                    case Socks5EnumStep.Command:
                        if (ValidateCommandData(info) == false) return true;
                        break;
                    case Socks5EnumStep.Forward:
                        break;
                    case Socks5EnumStep.ForwardUdp:
                        break;
                    default:
                        break;
                }

                return func(info);
            }
            return false;
        }
        private bool ValidateRequestData(Socks5Info info)
        {
            if (info.Buffer.Length == 0)
            {
                var span = info.Data.Span;
                if (span.Length >= 2 && span.Length >= 2 + span[1])
                {
                    return true;
                }

            }

            return false;
        }
        private bool ValidateAuthData(Socks5Info info)
        {
            return true;
        }
        private bool ValidateCommandData(Socks5Info info)
        {
            return true;
        }



        /// <summary>
        /// 收到关闭
        /// </summary>
        /// <param name="info"></param>
        protected virtual void OnClose(Socks5Info info)
        {
            GetConnection();
            socks5MessengerSender.RequestClose(info.Id, connection);
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
                info.Data = new byte[] { socks5ClientListener.Version, (byte)type };
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
                info.Data = Socks5Parser.MakeConnectResponse(socks5ClientListener.DistEndpoint, (byte)type);
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
        protected virtual bool HandleRequest(Socks5Info data)
        {
            data.Response[0] = (byte)Socks5EnumAuthType.NoAuth;
            data.Data = data.Response.AsMemory(0, 1);
            RequestResponseData(data);
            socks5ClientListener.Response(data);
            return true;
        }
        /// <summary>
        /// 收到auth
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual bool HandleAuth(Socks5Info data)
        {
            return true;
        }
        /// <summary>
        /// 收到command
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual bool HandleCommand(Socks5Info data)
        {
            //0.0.0.0  或者:0 都直接通过，这是udp中继的时候可能出现的情况
            if (Socks5Parser.GetIsAnyAddress(data.Data))
            {
                data.Response[0] = (byte)Socks5EnumResponseCommand.ConnecSuccess;
                data.Data = data.Response.AsMemory(0, 1);
                CommandResponseData(data);
                socks5ClientListener.Response(data);
                return true;
            }
            GetConnection();
            return socks5MessengerSender.Request(data, connection);
        }


        /// <summary>
        /// 收到转发
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual bool HndleForward(Socks5Info data)
        {
            GetConnection();
            return socks5MessengerSender.Request(data, connection);
        }
        /// <summary>
        /// 收到udp转发
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual bool HndleForwardUdp(Socks5Info data)
        {
            GetConnection();
            return socks5MessengerSender.Request(data, connection);
        }
        /// <summary>
        /// 刷新
        /// </summary>
        public virtual void Flush()
        {
            connection = null;
            GetConnection();
        }

        private void GetConnection()
        {
            if (connection == null || connection.Connected == false)
            {
                if (string.IsNullOrWhiteSpace(config.TargetName))
                {
                    connection = registerStateInfo.OnlineConnection;
                }
                else
                {
                    if (clientInfoCaching.GetByName(config.TargetName, out ClientInfo client))
                    {
                        connection = client.Connection;
                    }
                }
            }
        }
    }
}

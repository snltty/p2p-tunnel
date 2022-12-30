using System.Buffers;

namespace socks5
{
    /// <summary>
    /// socks5客户端
    /// </summary>
    public class Socks5ClientHandler : ISocks5ClientHandler
    {
        private readonly ISocks5ClientListener socks5ClientListener;

        protected Dictionary<Socks5EnumStep, Func<Socks5Info, bool>> handles = new Dictionary<Socks5EnumStep, Func<Socks5Info, bool>>();
        protected Dictionary<Socks5EnumStep, Action<Socks5Info>> buildHandles = new Dictionary<Socks5EnumStep, Action<Socks5Info>>();

        public Func<Socks5Info, bool> OnSendRequest { get; set; } = (data) => { return true; };
        public Action<Socks5Info> OnSendClose { get; set; } = (data) => { };

        public Socks5ClientHandler(ISocks5ClientListener socks5ClientListener)
        {
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
        public void InputData(Memory<byte> data)
        {
            Socks5Info info = Socks5Info.Debytes(data);
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
                if (info.Socks5Step == Socks5EnumStep.Auth)
                {
                    info.Version = info.Data.Span[0];
                }
                if (Validate(info) == false)
                {
                    return true;
                }

                return func(info);
            }
            return false;
        }

        private bool Validate(Socks5Info info)
        {
            //验证一下数据包是否已经完整，不完整的，等待下次数据继续拼接
            bool res = _Validate(info);
            if (res == false)
            {
                bool first = info.Buffer == null || info.Buffer[0] == 0;
                if (info.Buffer == null)
                {
                    info.Buffer = ArrayPool<byte>.Shared.Rent(256);
                    info.Buffer[0] = 0;
                }
                info.Data.CopyTo(info.Buffer.AsMemory(1 + info.Buffer[0]));
                info.Buffer[0] += (byte)info.Data.Length;

                //不是第一次拼接缓存，那就再验证一次
                if (first == false)
                {
                    res = _Validate(info);
                    if (res)
                    {
                        info.Data = info.Buffer.AsMemory(1, info.Buffer[0]);
                        info.Buffer[0] = 0;
                    }
                }
            }

            return res;
        }
        private bool _Validate(Socks5Info info) => info.Socks5Step switch
        {
            Socks5EnumStep.Request => ValidateRequestData(info),
            Socks5EnumStep.Command => ValidateCommandData(info),
            _ => true
        };
        private bool ValidateRequestData(Socks5Info info)
        {
            if (info.Buffer == null || info.Buffer[0] == 0)
            {
                return info.Data.Length >= 2 && info.Data.Length >= 2 + info.Data.Span[1];
            }
            return info.Buffer[0] >= 2 && info.Buffer[0] >= 2 + info.Buffer[2];
        }
        private bool ValidateCommandData(Socks5Info info)
        {
            if (info.Buffer == null || info.Buffer[0] == 0)
            {
                return info.Data.Length > 4 && info.Data.Length >= Socks5Parser.GetCommandTrueLength(info.Data);
            }
            return info.Buffer[0] > 4 && info.Buffer[0] >= Socks5Parser.GetCommandTrueLength(info.Buffer.AsMemory(1));
        }


        /// <summary>
        /// 收到关闭
        /// </summary>
        /// <param name="info"></param>
        protected virtual void OnClose(Socks5Info info)
        {
            OnSendClose(info);
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
                if (type == Socks5EnumResponseCommand.ConnecSuccess)
                {
                    info.Socks5Step = Socks5EnumStep.Forward;
                }
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
            #region 省略掉验证部分
            data.Response[0] = (byte)Socks5EnumAuthType.NoAuth;
            data.Data = data.Response;
            RequestResponseData(data);
            socks5ClientListener.Response(data);
            return true;
            #endregion

            #region 去往目标端验证
            //GetConnection();
            //return socks5MessengerSender.Request(data, connection);
            #endregion
        }
        /// <summary>
        /// 收到auth
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual bool HandleAuth(Socks5Info data)
        {
            return true;
            //GetConnection();
            // return socks5MessengerSender.Request(data, connection);
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

            bool res = OnSendRequest(data);
            data.Return();

            return res;
        }
        /// <summary>
        /// 收到转发
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual bool HndleForward(Socks5Info data)
        {
            return OnSendRequest(data);
        }
        /// <summary>
        /// 收到udp转发
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual bool HndleForwardUdp(Socks5Info data)
        {
            return OnSendRequest(data);
        }
        /// <summary>
        /// 刷新
        /// </summary>
        public virtual void Flush()
        {
        }

    }
}

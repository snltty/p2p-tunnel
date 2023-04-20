using common.libs;
using common.proxy;
using common.server.model;
using System;

namespace common.httpProxy
{
    public interface IHttpProxyPlugin : IProxyPlugin
    {
    }

    public class HttpProxyPlugin : IHttpProxyPlugin
    {
        public virtual byte Id => config.Plugin;
        public virtual EnumBufferSize BufferSize => config.BufferSize;
        public virtual ushort Port => config.ListenPort;
        public virtual bool Enable => config.ListenEnable;

        private readonly Config config;
        public HttpProxyPlugin(Config config)
        {
            this.config = config;
        }

        public EnumProxyValidateDataResult ValidateData(ProxyInfo info)
        {
            return EnumProxyValidateDataResult.Equal;
        }

        public virtual bool HandleRequestData(ProxyInfo info)
        {
            return true;
        }

        public virtual bool ValidateAccess(ProxyInfo info)
        {
            return Enable;
        }

        public virtual void HandleAnswerData(ProxyInfo info)
        {
            if (info.Step == EnumProxyStep.Command)
            {
                EnumProxyCommandStatus enumProxyCommandStatus = (EnumProxyCommandStatus)info.Data.Span[0];
                if (enumProxyCommandStatus == EnumProxyCommandStatus.ConnecSuccess)
                {
                    info.Data = HttpParser.ConnectSuccessMessage();
                }
                else
                {
                    info.Data = HttpParser.ConnectErrorMessage();
                }
                info.Step = EnumProxyStep.ForwardTcp;
            }
        }

    }

    [Flags]
    public enum HttpProxyMessengerIds : ushort
    {
        Min = 700,
        GetSetting = 701,
        Setting = 702,
        Max = 799,
    }
}

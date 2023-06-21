using common.proxy;
using common.server;
using common.server.model;
using System;
using System.Net;

namespace server.service.vea
{
    public interface IVeaSocks5ProxyPlugin : IProxyPlugin { }


    public class VeaSocks5ProxyPlugin: IVeaSocks5ProxyPlugin, IVeaValidator
    {
        public  byte Id => config.Plugin;
        public  bool ConnectEnable => config.Enable;
        public  EnumBufferSize BufferSize =>  EnumBufferSize.KB_8;
        public  IPAddress BroadcastBind => IPAddress.Any;
        public  HttpHeaderCacheInfo Headers { get; set; }
        public  Memory<byte> HeadersBytes { get; set; }
        public  uint Access => common.vea.Config.Access;
        public  string Name => "vea";
        public  ushort Port => 0;

        private readonly Config config;
        private readonly IServiceAccessValidator serviceAccessValidator;
        public VeaSocks5ProxyPlugin(Config config, IServiceAccessValidator serviceAccessValidator) 
        {
            this.config = config;
            this.serviceAccessValidator = serviceAccessValidator;
        }

        public bool HandleRequestData(ProxyInfo info)
        {
            return true;
        }
        public EnumProxyValidateDataResult ValidateData(ProxyInfo info)
        {
            return EnumProxyValidateDataResult.Equal;
        }
        public bool HandleAnswerData(ProxyInfo info)
        {
            return true;
        }

        public bool Validate(ulong connectionId)
        {
            return config.Enable || serviceAccessValidator.Validate(connectionId, config.Access);
        }
    }
}

using common.proxy;
using common.server;
using common.server.model;
using common.vea;
using server.messengers.signin;
using System;
using System.Net;

namespace server.service.vea
{
    public interface IVeaSocks5ProxyPlugin : IProxyPlugin { }


    public class VeaSocks5ProxyPlugin : IVeaSocks5ProxyPlugin, IVeaAccessValidator
    {
        public byte Id => config.Plugin;
        public bool ConnectEnable => config.Enable;
        public EnumBufferSize BufferSize => EnumBufferSize.KB_8;
        public IPAddress BroadcastBind => IPAddress.Any;
        public HttpHeaderCacheInfo Headers { get; set; }
        public Memory<byte> HeadersBytes { get; set; }
        public uint Access => common.vea.Config.access;
        public string Name => "vea";
        public ushort Port => 0;

        private readonly common.vea.Config config;
        private readonly IServiceAccessValidator serviceAccessValidator;
        private readonly IClientSignInCaching clientSignInCaching;
        public VeaSocks5ProxyPlugin(common.vea.Config config, IServiceAccessValidator serviceAccessValidator, IClientSignInCaching clientSignInCaching)
        {
            this.config = config;
            this.serviceAccessValidator = serviceAccessValidator;
            this.clientSignInCaching = clientSignInCaching;
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

        public bool Validate(ulong connectionId, out VeaAccessValidateResult result)
        {
            result = null;
            if (clientSignInCaching.Get(connectionId, out SignInCacheInfo sign) == false)
            {
                return false;
            }
            result = new VeaAccessValidateResult
            {
                Connection = sign.Connection,
                Key = sign.GroupId,
                Name = sign.Name
            };

            return config.Enable || serviceAccessValidator.Validate(connectionId, config.Access);
        }
    }
}

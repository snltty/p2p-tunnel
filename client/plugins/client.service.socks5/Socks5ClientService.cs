using client.service.ui.api.clientServer;
using common.libs.extends;
using common.socks5;
using System;

namespace client.service.socks5
{
    public class Socks5ClientService : IClientService
    {
        private readonly common.socks5.Config config;
        private readonly ISocks5ClientListener socks5ClientListener;
        private readonly Socks5Transfer socks5Transfer;
        private readonly ISocks5ClientHandler socks5ClientHandler;

        public Socks5ClientService(common.socks5.Config config, ISocks5ClientListener socks5ClientListener, Socks5Transfer socks5Transfer, ISocks5ClientHandler socks5ClientHandler)
        {
            this.config = config;
            this.socks5ClientListener = socks5ClientListener;
            this.socks5Transfer = socks5Transfer;
            this.socks5ClientHandler = socks5ClientHandler;
        }

        public common.socks5.Config Get(ClientServiceParamsInfo arg)
        {
            return config;
        }

        public void Set(ClientServiceParamsInfo arg)
        {;
            var conf = arg.Content.DeJson<common.socks5.Config>();

            socks5ClientListener.Stop();
            if (conf.ListenEnable)
            {
                try
                {
                    socks5ClientListener.Start(conf.ListenPort,config.BufferSize);
                }
                catch (Exception ex)
                {
                    arg.SetCode(ClientServiceResponseCodes.Error, ex.Message);
                }
            }

            config.ListenEnable = conf.ListenEnable;
            config.ConnectEnable = conf.ConnectEnable;
            config.ListenPort = conf.ListenPort;
            config.BufferSize = conf.BufferSize;
            config.IsPac = conf.IsPac;
            config.IsCustomPac = conf.IsCustomPac;
            config.TargetName = conf.TargetName;
            config.TunnelType = conf.TunnelType;
            config.LanConnectEnable = conf.LanConnectEnable;
            //config.NumConnections = conf.NumConnections;
            
            config.SaveConfig().Wait();

            socks5Transfer.ClearPac();
            socks5ClientHandler.Flush();
        }

        public string GetPac(ClientServiceParamsInfo arg)
        {
            return socks5Transfer.GetPac();
        }

        public void SetPac(ClientServiceParamsInfo arg)
        {
            PacSetParamsInfo model = arg.Content.DeJson<PacSetParamsInfo>();
            string msg = socks5Transfer.UpdatePac(model);
            if (!string.IsNullOrWhiteSpace(msg))
            {
                arg.SetCode(ClientServiceResponseCodes.Error, msg);
            }
        }
    }
}

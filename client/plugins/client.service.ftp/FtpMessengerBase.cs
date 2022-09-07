using client.messengers.clients;
using client.service.ftp.commands;
using common.libs;
using common.libs.extends;
using common.server;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace client.service.ftp
{
    public abstract class FtpMessengerBase : IMessenger
    {
        private readonly Config config;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly Dictionary<FtpCommand, IFtpCommandPluginBase> plugins;
        protected FtpMessengerBase(Config config, IClientInfoCaching clientInfoCaching, Dictionary<FtpCommand, IFtpCommandPluginBase> plugins)
        {
            this.config = config;
            this.clientInfoCaching = clientInfoCaching;
            this.plugins = plugins;
        }

        protected async Task<byte[]> Execute(IConnection connection)
        {
            if (!config.Enable)
            {
                return new FtpResultInfo
                {
                    Code = FtpResultInfo.FtpResultCodes.DISABLE
                }.ToBytes();
            }
            else
            {
                FtpCommand cmd = (FtpCommand)connection.ReceiveRequestWrap.Memory.Span[0];
                FtpPluginParamWrap wrap = new FtpPluginParamWrap
                {
                    Connection = connection,
                };
                if (clientInfoCaching.Get(connection.FromConnection.ConnectId, out ClientInfo client))
                {
                    wrap.Client = client;
                    if (plugins.ContainsKey(cmd))
                    {
                        try
                        {
                            FtpResultInfo res = await plugins[cmd].Execute(wrap).ConfigureAwait(false);
                            if (res != null)
                            {
                                return res.ToBytes();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.DebugError(ex);
                            return new FtpResultInfo
                            {
                                Code = FtpResultInfo.FtpResultCodes.UNKNOW,
                                Data = ex.Message.ToBytes()
                            }.ToBytes();
                        }
                    }
                }
            }

            return null;
        }
    }
}

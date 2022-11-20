using client.messengers.clients;
using client.messengers.register;
using common.libs;
using common.server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.wakeup
{
    public class WakeUpTransfer
    {
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly WakeUpMessengerSender wakeUpMessengerSender;
        private readonly RegisterStateInfo registerStateInfo;
        private readonly ConcurrentDictionary<string, List<ConfigItem>> macs = new ConcurrentDictionary<string, List<ConfigItem>>();

        public WakeUpTransfer(IClientInfoCaching clientInfoCaching, WakeUpMessengerSender wakeUpMessengerSender, Config config, RegisterStateInfo registerStateInfo)
        {
            this.clientInfoCaching = clientInfoCaching;
            this.wakeUpMessengerSender = wakeUpMessengerSender;
            this.registerStateInfo = registerStateInfo;

            clientInfoCaching.OnOnline.Sub((client) =>
            {
                UpdateMac();
            });
            clientInfoCaching.OnOffline.Sub((client) =>
            {
                macs.TryRemove(client.Name, out _);
            });
        }

        public void OnNotify(IConnection connection)
        {
            if (connection.FromConnection != null)
            {
                if (clientInfoCaching.Get(connection.FromConnection.ConnectId, out ClientInfo client))
                {
                    UpdateMac(client, Config.DeBytes(connection.ReceiveRequestWrap.Payload));
                }
            }
        }
        private void UpdateMac(ClientInfo client, List<ConfigItem> mac)
        {
            macs.AddOrUpdate(client.Name, mac, (a, b) => mac);
        }
        public void UpdateMac()
        {
            foreach (var item in clientInfoCaching.All().Where(c => c.Id != registerStateInfo.ConnectId))
            {
                IConnection connection = item.Connection;
                var client = item;
                if (connection != null)
                {
                    wakeUpMessengerSender.Mac(connection).ContinueWith((result) =>
                    {
                        UpdateMac(client, result.Result);
                    });
                }
            }
        }

        public Dictionary<string, List<ConfigItem>> Get()
        {
            return macs.ToDictionary(c => c.Key, d => d.Value);
        }

        public async Task<bool> WakeUp(string name, string mac)
        {
            if (clientInfoCaching.GetByName(name, out ClientInfo client))
            {
                await wakeUpMessengerSender.WakeUp(client.Connection, mac);
            }
            else
            {
                WakeUp(mac);
            }
            return true;
        }
        public void WakeUp(string mac)
        {
            var packet = NetworkHelper.MagicPacket(mac);

            using UdpClient client = new UdpClient();
            for (int i = 0; i < 10; i++)
            {
                client.Send(packet, new IPEndPoint(IPAddress.Parse("255.255.255.255"), 59410));
            }
        }
    }
}

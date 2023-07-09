using client.messengers.clients;
using client.messengers.singnin;
using client.service.vea.platforms;
using common.libs;
using common.libs.extends;
using common.proxy;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.vea
{
    /// <summary>
    /// 组网
    /// </summary>
    public sealed class VeaTransfer
    {
        private readonly ConcurrentDictionary<uint, IPAddressCacheInfo> ips = new ConcurrentDictionary<uint, IPAddressCacheInfo>();
        private readonly ConcurrentDictionary<uint, IPAddressCacheInfo> lanips = new ConcurrentDictionary<uint, IPAddressCacheInfo>();
        private readonly ConcurrentDictionary<ulong, VeaLanIPAddressOnLine> onlines = new ConcurrentDictionary<ulong, VeaLanIPAddressOnLine>();

        public ConcurrentDictionary<uint, IPAddressCacheInfo> IPList => ips;
        public ConcurrentDictionary<uint, IPAddressCacheInfo> LanIPList => lanips;
        public ConcurrentDictionary<ulong, VeaLanIPAddressOnLine> Onlines => onlines;

        private readonly Config config;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly VeaMessengerSender veaMessengerSender;
        private readonly SignInStateInfo signInStateInfo;
        private readonly IProxyServer proxyServer;
        private readonly IVeaPlatform veaPlatform;

        public VeaTransfer(Config config, IClientInfoCaching clientInfoCaching, VeaMessengerSender veaMessengerSender, SignInStateInfo signInStateInfo, IProxyServer proxyServer, IVeaPlatform veaPlatform)
        {
            this.config = config;
            this.clientInfoCaching = clientInfoCaching;
            this.veaMessengerSender = veaMessengerSender;
            this.signInStateInfo = signInStateInfo;
            this.proxyServer = proxyServer;
            this.veaPlatform = veaPlatform;

            clientInfoCaching.OnOnline += (client) =>
            {
                UpdateIp();
            };
            clientInfoCaching.OnOffline += (client) =>
            {
                var value = ips.FirstOrDefault(c => c.Value.Client.ConnectionId == client.ConnectionId);
                if (value.Key != 0 && ips.TryRemove(value.Key, out IPAddressCacheInfo cache))
                {
                    RemoveLanMasks(cache.LanIPs);
                }
            };
            signInStateInfo.OnChange += (state) =>
            {
                if (state)
                {
                    Task.Run(async () =>
                    {
                        await Run();
                    });
                }
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, e) => Stop();
            Console.CancelKeyPress += (sender, e) => Stop();
        }
        /// <summary>
        /// 收到某个客户端的ip信息
        /// </summary>
        /// <param name="connectionid"></param>
        /// <param name="ips"></param>
        public void OnIPs(ulong connectionid, IPAddressInfo ips)
        {
            if (clientInfoCaching.Get(connectionid, out ClientInfo client))
            {
                UpdateIp(client, ips);
            }

        }
        /// <summary>
        /// 收到某个客户端的在线设备信息
        /// </summary>
        /// <param name="connectionid"></param>
        /// <param name="online"></param>
        public void OnOnline(ulong connectionid, VeaLanIPAddressOnLine online)
        {
            onlines.AddOrUpdate(connectionid, online, (a, b) => online);
        }
        public async Task<EnumProxyCommandStatusMsg> Test(string host, int port)
        {
            if (config.ListenEnable == false)
            {
                return EnumProxyCommandStatusMsg.Listen;
            }
            if (ProxyPluginLoader.GetPlugin(config.Plugin, out IProxyPlugin plugin) == false)
            {
                return EnumProxyCommandStatusMsg.Listen;
            }

            if (IPAddress.TryParse(host, out IPAddress ip) == false)
            {
                ip = Dns.GetHostEntry(host).AddressList[0];
            }
            if (ip.Equals(IPAddress.Any)) ip = IPAddress.Loopback;

            byte[] ips = ip.GetAddressBytes();
            byte[] ports = BitConverter.GetBytes(port);
            IPEndPoint target = new IPEndPoint(IPAddress.Loopback, config.ListenPort);
            try
            {
                using Socket socket = new Socket(target.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(target);

                byte[] bytes = new byte[ProxyHelper.MagicData.Length];
                //request
                await socket.SendAsync(new byte[] { 0x05, 0x01, 0 }, SocketFlags.None);
                await socket.ReceiveAsync(bytes, SocketFlags.None);
                //command
                byte[] socks5Data = new byte[] { 0x05, 0x01, 0, 0x01, ips[0], ips[1], ips[2], ips[3], ports[1], ports[0] };
                byte[] data = new byte[bytes.Length + socks5Data.Length];
                socks5Data.AsSpan().CopyTo(data);
                ProxyHelper.MagicData.AsSpan().CopyTo(data.AsSpan(socks5Data.Length));
                await socket.SendAsync(data, SocketFlags.None);

                int length = await socket.ReceiveAsync(bytes, SocketFlags.None);

                EnumProxyCommandStatusMsg statusMsg = EnumProxyCommandStatusMsg.Listen;
                if (length > 0)
                {
                    statusMsg = (EnumProxyCommandStatusMsg)bytes[0];
                }
                return statusMsg;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex + "");
            }
            return EnumProxyCommandStatusMsg.Listen;
        }

        /// <summary>
        /// 更新ip
        /// </summary>
        public void UpdateIp()
        {
            foreach (var item in clientInfoCaching.All().Where(c => c.ConnectionId != signInStateInfo.ConnectId))
            {
                var connection = item.Connection;
                var client = item;
                if (connection != null)
                {
                    veaMessengerSender.UpdateIp(connection).ContinueWith((result) =>
                    {
                        UpdateIp(client, result.Result);
                    });
                }
            }
        }
        private void UpdateIp(ClientInfo client, IPAddressInfo _ips)
        {
            if (client == null || _ips == null) return;



            lock (this)
            {
                var cache = ips.Values.FirstOrDefault(c => c.Client.ConnectionId == client.ConnectionId);
                if (cache != null)
                {
                    ips.TryRemove(cache.IP, out _);
                    RemoveLanMasks(cache.LanIPs);
                }

                cache = new IPAddressCacheInfo { Client = client, IP = _ips.IP, LanIPs = _ips.LanIPs, MaskLength = 32, MaskValue = 0xffffffff };
                ips.AddOrUpdate(_ips.IP, cache, (a, b) => cache);
                AddLanMasks(_ips, client);

                AddRoute();
            }
        }

        /// <summary>
        /// 开启
        /// </summary>
        public async Task<bool> Run()
        {
            bool res = true;
            Stop();

            uint ip = await veaMessengerSender.AssignIP(signInStateInfo.Connection, (byte)(BinaryPrimitives.ReadUInt32BigEndian(config.IP.GetAddressBytes()) & 0xff));
            if (ip > 0)
            {
                config.IP = new IPAddress(ip.ToBytes());
            }
            else
            {
                Logger.Instance.Warning($"未能从服务器分配到组网IP，直接返回本地配置文件配置IP");
            }

            if (config.ListenEnable)
            {
                res = veaPlatform.Run();
                if (res)
                {
                    if (proxyServer.Start((ushort)config.ListenPort, config.Plugin) == false)
                    {
                        Stop();
                    }
                }
            }
            UpdateIp();
            return res;
        }
        /// <summary>
        /// 停止
        /// </summary>
        public void Stop()
        {
            proxyServer.Stop(config.Plugin);
            veaPlatform.Kill();
        }

        private void AddRoute()
        {
            foreach (var item in ips)
            {
                VeaLanIPAddress[] _lanips = ExcludeLanIP(item.Value.LanIPs, item.Value.Client);
                veaPlatform.AddRoute(_lanips);
            }
        }

        private void RemoveLanMasks(VeaLanIPAddress[] _lanips)
        {
            foreach (var item in _lanips)
            {
                lanips.TryRemove(item.NetWork, out _);
            }
            veaPlatform.DelRoute(_lanips);
        }
        private void AddLanMasks(IPAddressInfo ips, ClientInfo client)
        {
            var _lanips = ExcludeLanIP(ips.LanIPs, client);
            foreach (var item in _lanips)
            {
                IPAddressCacheInfo cache = new IPAddressCacheInfo
                {
                    Client = client,
                    IP = item.IPAddress,
                    LanIPs = ips.LanIPs,
                    NetWork = item.IPAddress & item.MaskValue,
                    MaskLength = item.MaskLength,
                    MaskValue = item.MaskValue
                };
                lanips.AddOrUpdate(cache.NetWork, cache, (a, b) => cache);
            }
        }
        /// <summary>
        /// 排除与目标客户端连接的ip
        /// </summary>
        /// <param name="lanips"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        private VeaLanIPAddress[] ExcludeLanIP(VeaLanIPAddress[] lanips, ClientInfo client)
        {
            uint ip = BinaryPrimitives.ReadUInt32BigEndian(client.IPAddress.Address.GetAddressBytes());
            uint ip1 = BinaryPrimitives.ReadUInt32BigEndian(signInStateInfo.LocalInfo.LocalIp.GetAddressBytes());
            return lanips.Where(c =>
            {
                //取网络号不一样的
                return (ip & c.MaskValue) != c.NetWork && (ip1 & c.MaskValue) != c.NetWork;
            }).ToArray();
        }

    }

    /// <summary>
    /// ip缓存
    /// </summary>
    public sealed class IPAddressCacheInfo
    {
        /// <summary>
        /// ip 小端
        /// </summary>
        public uint IP { get; set; }
        /// <summary>
        /// 局域网网段
        /// </summary>
        public VeaLanIPAddress[] LanIPs { get; set; }
        /// <summary>
        /// 客户端
        /// </summary>

        [System.Text.Json.Serialization.JsonIgnore]
        public ClientInfo Client { get; set; }

        /// <summary>
        /// 网络号，小端
        /// </summary>
        public uint NetWork { get; set; }
        /// <summary>
        /// 掩码长度 24 16 8什么的
        /// </summary>
        public byte MaskLength { get; set; }
        /// <summary>
        /// 掩码具体值 24就是 0xffffff00
        /// </summary>
        public uint MaskValue { get; set; }
    }

    /// <summary>
    /// ip更新消息模型
    /// </summary>
    public sealed class IPAddressInfo
    {
        /// <summary>
        /// ip 小端
        /// </summary>
        public uint IP { get; set; }
        /// <summary>
        /// 局域网网段
        /// </summary>
        public VeaLanIPAddress[] LanIPs { get; set; }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            var bytes = new byte[
                1   //ip length
                + 4 //ip
                + 1 // LanIPs length
                + LanIPs.Length * 17];
            var span = bytes.AsSpan();

            int index = 0;
            bytes[index] = 4;
            index += 1;
            IP.ToBytes(bytes.AsMemory(index));
            index += 4;

            bytes[index] = (byte)LanIPs.Length;
            index += 1;
            for (int i = 0; i < LanIPs.Length; i++)
            {
                LanIPs[i].IPAddress.ToBytes(bytes.AsMemory(index));
                index += 4;
                bytes[index] = LanIPs[i].MaskLength;
                index += 1;
                LanIPs[i].MaskValue.ToBytes(bytes.AsMemory(index));
                index += 4;
                LanIPs[i].NetWork.ToBytes(bytes.AsMemory(index));
                index += 4;
                LanIPs[i].Broadcast.ToBytes(bytes.AsMemory(index));
                index += 4;
            }

            return bytes;
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="memory"></param>
        public void DeBytes(ReadOnlyMemory<byte> memory)
        {
            var span = memory.Span;

            int index = 0;

            byte ipLength = span[index];
            index += 1;

            IP = span.Slice(index, ipLength).ToUInt32();
            index += ipLength;

            byte lanipLength = span[index];
            index += 1;

            LanIPs = new VeaLanIPAddress[lanipLength];
            for (int i = 0; i < lanipLength; i++)
            {
                ReadOnlyMemory<byte> ip = memory.Slice(index, 4);
                index += 4;
                byte mask = span[index];
                index += 1;

                ReadOnlyMemory<byte> maskvalue = memory.Slice(index, 4);
                index += 4;

                ReadOnlyMemory<byte> network = memory.Slice(index, 4);
                index += 4;

                ReadOnlyMemory<byte> broadcast = memory.Slice(index, 4);
                index += 4;

                LanIPs[i] = new VeaLanIPAddress
                {
                    IPAddress = ip.ToUInt32(),
                    MaskLength = mask,
                    MaskValue = maskvalue.ToUInt32(),
                    NetWork = network.ToUInt32(),
                    Broadcast = broadcast.ToUInt32(),
                };
            }
        }
    }

    /// <summary>
    /// 局域网在线设备
    /// </summary>
    public sealed class VeaLanIPAddressOnLine
    {
        public Dictionary<uint, VeaLanIPAddressOnLineItem> Items { get; set; } = new Dictionary<uint, VeaLanIPAddressOnLineItem>();

        public byte[] ToBytes()
        {
            if (Items.Count == 0) return Helper.EmptyArray;

            MemoryStream memoryStream = new MemoryStream();
            byte[] keyBytes = new byte[4];
            foreach (var item in Items.ToArray())
            {
                item.Key.ToBytes(keyBytes);
                memoryStream.Write(keyBytes, 0, keyBytes.Length);

                memoryStream.WriteByte((byte)(item.Value.Online ? 1 : 0));

                ReadOnlySpan<byte> name = item.Value.Name.GetUTF16Bytes();
                memoryStream.WriteByte((byte)name.Length);
                memoryStream.WriteByte((byte)item.Value.Name.Length);
                memoryStream.Write(name);
            }

            return memoryStream.ToArray();
        }

        public void DeBytes(ReadOnlyMemory<byte> memory)
        {
            if (memory.Length == 0) return;

            ReadOnlySpan<byte> span = memory.Span;

            int index = 0;
            while (index < memory.Length)
            {
                uint key = span.Slice(index).ToUInt32();
                index += 4;

                bool online = span[index] == 1;
                index += 1;

                string name = span.Slice(index + 2, span[index]).GetUTF16String(span[index + 1]);
                index += 1 + 1 + span[index];

                Items[key] = new VeaLanIPAddressOnLineItem { Online = online, Name = name };
            }
        }

    }
    /// <summary>
    /// 局域网在线设备
    /// </summary>
    public sealed class VeaLanIPAddressOnLineItem
    {
        public bool Online { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

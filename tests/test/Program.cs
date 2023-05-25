using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using common.libs;
using common.libs.extends;
using Iced.Intel;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;

namespace invokeSpeed
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int len = 0;
            for (int i = 0; i < 65536; i++)
            {
                if( i % 16384 == 8080)
                {
                    len++;
                }
            }
            Console.WriteLine(len);
            //var summary = BenchmarkRunner.Run<Test>();
        }
    }



    [MemoryDiagnoser]
    public unsafe class Test
    {
        public static FirewallCacheType[] Firewall0 { get; } = new FirewallCacheType[2];
        public static Dictionary<ushort, FirewallCacheType[]> Firewalls { get; } = new Dictionary<ushort, FirewallCacheType[]>();

        public ProxyInfo proxyInfo = new ProxyInfo
        {
            ListenPort = 50,
            PluginId = 1,
            RequestId = 0,
            Step = EnumProxyStep.Command,
            Command = EnumProxyCommand.Connect,
            TargetAddress = new byte[] { 127, 0, 0, 1 },
            TargetPort = 50,

        };


        [GlobalSetup]
        public void Startup()
        {
            for (ushort i = 0; i < 65535; i++)
            {
                FirewallCacheType[] firewallCacheTypes = new FirewallCacheType[2];
                firewallCacheTypes[0] = new FirewallCacheType
                {
                    Ips = new ulong[] { 0 },
                    PluginIds = 3,
                    Protocols = FirewallProtocolType.TCP_UDP,
                    Type = FirewallType.Allow
                };
                Firewalls.Add(i, firewallCacheTypes);
            }
        }


        FirewallCacheType[] data = new FirewallCacheType[8];
        [Benchmark]
        public void Test1()
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == null) { }
            }
            //FirewallDenied(proxyInfo);
        }


        public static bool FirewallDenied(ProxyInfo info)
        {
            FirewallProtocolType protocolType = info.Step == EnumProxyStep.Command && info.Command == EnumProxyCommand.Connect ? FirewallProtocolType.TCP : FirewallProtocolType.UDP;
            //阻止IPV6的内网ip
            if (info.TargetAddress.Length == EndPointExtends.ipv6Loopback.Length)
            {
                Span<byte> span = info.TargetAddress.Span;
                return span.SequenceEqual(EndPointExtends.ipv6Loopback.Span) || span.SequenceEqual(EndPointExtends.ipv6Multicast.Span) || (span[0] == EndPointExtends.ipv6Local.Span[0] && span[1] == EndPointExtends.ipv6Local.Span[1]);
            }
            //IPV4的，防火墙验证
            else if (info.TargetAddress.Length == 4)
            {
                uint ip = BinaryPrimitives.ReadUInt32BigEndian(info.TargetAddress.Span);
                byte aloow = (byte)FirewallType.Allow;
                byte denied = (byte)FirewallType.Denied;

                Firewalls.TryGetValue(info.TargetPort, out FirewallCacheType[] cache);
                //黑名单
                if (Comparison(info, Firewall0[denied], ip, protocolType) || (cache != null && Comparison(info, cache[denied], ip, protocolType)))
                {
                    return true;
                }
                //白名单
                if (info.TargetAddress.IsLan() || info.TargetAddress.GetIsBroadcastAddress())
                {
                    if (Comparison(info, Firewall0[aloow], ip, protocolType) || (cache != null && Comparison(info, cache[aloow], ip, protocolType)))
                    {
                        return false;
                    }
                    return true;
                }
            }
            //其它的直接通过
            return false;
        }

        public static bool Comparison(ProxyInfo info, FirewallCacheType cache, uint ip, FirewallProtocolType protocolType)
        {
            if (cache == null) return false;
            if ((cache.PluginIds & info.PluginId) == info.PluginId && (cache.Protocols & protocolType) == protocolType)
            {
                for (int j = 0; j < cache.Ips.Length; j++)
                {
                    FirewallCacheIp ipcache = new FirewallCacheIp(cache.Ips[j]);
                    if ((ip & ipcache.MaskValue) == ipcache.NetWork) return true;
                }
            }
            return false;
        }
    }

    public enum EnumProxyStep : byte
    {
        Command = 1,
        /// <summary>
        /// TCP转发
        /// </summary>
        ForwardTcp = 2,
        /// <summary>
        /// UDP转发
        /// </summary>
        ForwardUdp = 3
    }
    public enum EnumProxyCommand : byte
    {
        /// <summary>
        /// 连接上游服务器
        /// </summary>
        Connect = 1,
        /// <summary>
        /// 绑定，客户端会接收来自代理服务器的链接，著名的FTP被动模式
        /// </summary>
        Bind = 2,
        /// <summary>
        /// UDP中继
        /// </summary>
        UdpAssociate = 3
    }
    public sealed class ProxyInfo
    {
        public byte Rsv { get; set; }
        public EnumProxyStep Step { get; set; } = EnumProxyStep.Command;
        public EnumProxyCommand Command { get; set; } = EnumProxyCommand.Connect;
        public byte PluginId { get; set; }
        public uint RequestId { get; set; }
        public IPEndPoint SourceEP { get; set; }
        public Memory<byte> TargetAddress { get; set; }
        public ushort TargetPort { get; set; }
        public Memory<byte> Data { get; set; }
        /// <summary>
        /// 监听的端口
        /// </summary>
        public ushort ListenPort { get; set; }
    }

    public sealed class FirewallCacheType
    {
        public FirewallProtocolType Protocols { get; set; }
        public FirewallType Type { get; set; }
        public byte PluginIds { get; set; }
        public ulong[] Ips { get; set; } = Array.Empty<ulong>();

        public FirewallCacheType()
        {

        }


    }
    [StructLayout(LayoutKind.Explicit)]
    public struct FirewallCacheIp
    {
        [FieldOffset(0)]
        public ulong Value;
        [FieldOffset(0)]
        public uint MaskValue;
        [FieldOffset(4)]
        public uint NetWork;
        public FirewallCacheIp(ulong value)
        {
            Value = value;
        }
        public FirewallCacheIp(uint maskValue, uint netWork)
        {
            MaskValue = maskValue;
            NetWork = netWork;
        }
    }
    public enum FirewallProtocolType : byte
    {
        TCP = 1,
        UDP = 2,
        TCP_UDP = TCP | UDP,
    }
    public enum FirewallType : byte
    {
        Allow = 0,
        Denied = 1,
    }

}
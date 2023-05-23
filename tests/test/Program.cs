using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using common.libs;
using common.libs.extends;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace invokeSpeed
{
    internal class Program
    {
        static void Main(string[] args)
        {
           
            var summary = BenchmarkRunner.Run<Test>();
        }



    }

    public sealed class FirewallCache
    {
        public FirewallCacheIp[] IPs { get; set; } = Array.Empty<FirewallCacheIp>();
    }
    public sealed class FirewallCacheIp
    {
        public uint MaskValue { get; set; }
        public uint NetWork { get; set; }
    }


    [MemoryDiagnoser]
    public unsafe class Test
    {

        public Dictionary<uint, FirewallCache> AllowFirewalls { get; set; } = new Dictionary<uint, FirewallCache>();
        public Dictionary<uint, FirewallCache> DeniedFirewalls { get; set; } = new Dictionary<uint, FirewallCache>();
        public ProxyInfo proxyInfo = new ProxyInfo
        {
            ListenPort = 8080,
            PluginId = 1,
            RequestId = 0,
            Step = EnumProxyStep.Command,
            Command = EnumProxyCommand.Connect,
            TargetAddress = new byte[] { 127, 0, 0, 1 },
            TargetPort = 8080,

        };
        [StructLayout(LayoutKind.Explicit)]
        public readonly struct FirewallKey
        {
            [FieldOffset(0)]
            public readonly uint Memory;

            [FieldOffset(0)]
            public readonly ushort Port;

            [FieldOffset(2)]
            public readonly FirewallProtocolType Protocol;

            [FieldOffset(3)]
            public readonly byte PluginId;

            public FirewallKey(ushort port, FirewallProtocolType protocol, byte pluginId)
            {
                Port = port;
                Protocol = protocol;
                PluginId = pluginId;
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
        public enum FirewallProtocolType : byte
        {
            TCP = 1,
            UDP = 2,
            TCP_UDP = TCP | UDP,
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


        [GlobalSetup]
        public void Startup()
        {
            for (uint i = 0; i < 100; i++)
            {
                AllowFirewalls.Add(i, new FirewallCache { IPs = new FirewallCacheIp[] { new FirewallCacheIp { MaskValue = 0, NetWork = 0 } } });
                DeniedFirewalls.Add(i, new FirewallCache { IPs = new FirewallCacheIp[] { new FirewallCacheIp { MaskValue = 0, NetWork = 0 } } });
            }
        }

        [Benchmark]
        public void Test1()
        {
            FirewallDenied(proxyInfo);
        }


        private bool FirewallDenied(ProxyInfo info)
        {
            FirewallCache cache = new FirewallCache { IPs = new FirewallCacheIp[] { new FirewallCacheIp { MaskValue=0, NetWork=0 } } };
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

                uint keyGlobal = new FirewallKey(info.TargetPort, protocolType, 0).Memory;
                uint keyGlobal0 = new FirewallKey(0, protocolType, 0).Memory;
                uint keyPlugin = new FirewallKey(info.TargetPort, protocolType, info.PluginId).Memory;
                uint keyPlugin0 = new FirewallKey(0, protocolType, info.PluginId).Memory;

                //黑名单
                if (DeniedFirewalls.Count > 0)
                {
                    //全局0 || 全局端口 || 局部0 || 局部端口
                    bool res = false;
                    /*DeniedFirewalls.TryGetValue(keyGlobal0, out FirewallCache cache)
                        || DeniedFirewalls.TryGetValue(keyGlobal, out cache)
                        || DeniedFirewalls.TryGetValue(keyPlugin0, out cache)
                        || DeniedFirewalls.TryGetValue(keyPlugin, out cache);
                    */
                    if (cache != null)
                    {
                        //有一项匹配就不通过
                        for (int i = 0; i < cache.IPs.Length; i++)
                        {
                           // if ((ip & cache.IPs[i].MaskValue) == cache.IPs[i].NetWork) return true;
                        }
                    }
                }
                //局域网或者组播，验证白名单
                if (info.TargetAddress.IsLan() || info.TargetAddress.GetIsBroadcastAddress())
                {
                    if (AllowFirewalls.Count > 0)
                    {
                        //全局0 || 全局端口 || 局部0 || 局部端口
                        bool res = false;
                        /*
                         * AllowFirewalls.TryGetValue(keyGlobal0, out FirewallCache cache)
                            || AllowFirewalls.TryGetValue(keyGlobal, out cache)
                            || AllowFirewalls.TryGetValue(keyPlugin0, out cache)
                            || AllowFirewalls.TryGetValue(keyPlugin, out cache);
                        */
                        if (cache != null)
                        {
                            //有一项通过就通过
                            for (int i = 0; i < cache.IPs.Length; i++)
                            {
                                //if ((ip & cache.IPs[i].MaskValue) == cache.IPs[i].NetWork) return false;
                            }
                        }
                    }
                    return true;
                }

            }
            //其它的直接通过
            return false;
        }
    }
}
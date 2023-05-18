using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace invokeSpeed
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var entry = Dns.GetHostEntryAsync(IPAddress.Parse("192.168.1.3")).WaitAsync(TimeSpan.FromSeconds(5)).Result;
            Console.WriteLine(entry.HostName);
            //var summary = BenchmarkRunner.Run<Test>();
        }


    }

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

    public enum FirewallProtocolType : byte
    {
        TCP = 0,
        UDP = 1,
    }



    [MemoryDiagnoser]
    public unsafe class Test
    {
        [Benchmark]
        public void Test1()
        {
            FirewallKey firewallKey = new FirewallKey(1, FirewallProtocolType.UDP, 1);
            uint value = firewallKey.Memory;
        }
    }
}
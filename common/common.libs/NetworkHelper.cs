using common.libs.extends;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace common.libs
{
    public static class NetworkHelper
    {
        [DllImport("ws2_32.dll")]
        private static extern int inet_addr(string cp);
        [DllImport("IPHLPAPI.dll")]
        private static extern int SendARP(Int32 DestIP, Int32 SrcIP, ref Int64 pMacAddr, ref Int32 PhyAddrLen);
        public static string GetMacAddress(string hostip)
        {
            string mac;
            try
            {
                int ldest = inet_addr(hostip);
                long macinfo = new();
                int len = 6;
                SendARP(ldest, 0, ref macinfo, ref len);
                string tmpMac = Convert.ToString(macinfo, 16).PadLeft(12, '0');
                mac = tmpMac.Substring(0, 2).ToUpper();
                for (int i = 2; i < tmpMac.Length; i += 2)
                {
                    mac = tmpMac.Substring(i, 2).ToUpper() + ":" + mac;
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
            return mac;
        }

        /// <summary>
        /// 获取路由层数，自己与外网距离几个网关，用于发送一个对方网络收不到没有回应的数据包
        /// </summary>
        /// <returns></returns>
        public static ushort GetRouteLevel()
        {
            try
            {
                List<string> starts = new() { "10.", "100.", "192.168.", "172." };
                IEnumerable<IPAddress> list = GetTraceRoute("www.baidu.com");
                for (ushort i = 0; i < list.Count(); i++)
                {
                    string ip = list.ElementAt(i).ToString();
                    if (ip.StartsWith(starts[0]) || ip.StartsWith(starts[1]) || ip.StartsWith(starts[2]))
                    {

                    }
                    else
                    {
                        return i;
                    }
                }
            }
            catch (Exception)
            {
            }
            return 0;
        }
        public static IEnumerable<IPAddress> GetTraceRoute(string hostNameOrAddress)
        {
            return GetTraceRoute(hostNameOrAddress, 1);
        }
        private static IEnumerable<IPAddress> GetTraceRoute(string hostNameOrAddress, int ttl)
        {
            Ping pinger = new();
            // 创建PingOptions对象
            PingOptions pingerOptions = new(ttl, true);
            int timeout = 100;
            byte[] buffer = Encoding.ASCII.GetBytes("11");
            // 创建PingReply对象
            // 发送ping命令
            PingReply reply = pinger.Send(hostNameOrAddress, timeout, buffer, pingerOptions);

            // 处理返回结果
            List<IPAddress> result = new();
            if (reply.Status == IPStatus.Success)
            {
                result.Add(reply.Address);
            }
            else if (reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.TimedOut)
            {
                //增加当前这个访问地址
                if (reply.Status == IPStatus.TtlExpired)
                {
                    result.Add(reply.Address);
                }

                if (ttl <= 10)
                {
                    //递归访问下一个地址
                    IEnumerable<IPAddress> tempResult = GetTraceRoute(hostNameOrAddress, ttl + 1);
                    result.AddRange(tempResult);
                }
            }
            else
            {
                //失败
            }
            return result;
        }

        public static bool Ping(IPAddress address, int timeout = 100)
        {
            try
            {
                using Ping pinger = new();
                PingReply reply = pinger.Send(address, timeout);
                if (reply.Status == IPStatus.Success)
                {
                    return true;
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

        /// <summary>
        /// 获取一个随机端口
        /// </summary>
        /// <param name="usedPorts"></param>
        /// <returns></returns>
        public static ushort GetRandomPort(List<ushort> usedPorts = null)
        {

            List<ushort> allPorts = GetUsedPort();
            if (usedPorts != null)
            {
                allPorts.AddRange(usedPorts);
            }
            Random rd = new();
            while (true)
            {
                ushort port = (ushort)rd.Next(10000, 56000);
                if (!allPorts.Contains(port))
                {
                    return port;
                }
            }
        }
        /// <summary>
        /// 获取已使用过的端口
        /// </summary>
        /// <returns></returns>
        public static List<ushort> GetUsedPort()
        {
            try
            {
                //获取本地计算机的网络连接和通信统计数据的信息
                IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

                //返回本地计算机上的所有Tcp监听程序
                IPEndPoint[] ipsTCP = ipGlobalProperties.GetActiveTcpListeners();

                //返回本地计算机上的所有UDP监听程序
                IPEndPoint[] ipsUDP = ipGlobalProperties.GetActiveUdpListeners();

                //返回本地计算机上的Internet协议版本4(IPV4 传输控制协议(TCP)连接的信息。
                TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

                List<ushort> allPorts = new();
                foreach (IPEndPoint ep in ipsTCP)
                {
                    allPorts.Add((ushort)ep.Port);
                }

                foreach (IPEndPoint ep in ipsUDP)
                {
                    allPorts.Add((ushort)ep.Port);
                }

                foreach (TcpConnectionInformation conn in tcpConnInfoArray)
                {
                    allPorts.Add((ushort)conn.LocalEndPoint.Port);
                }
                return allPorts;
            }
            catch (Exception)
            {
            }
            return new List<ushort>();

        }

        /// <summary>
        /// 域名解析
        /// </summary>
        /// <param name="domain"></param>                      
        /// <returns></returns>
        public static IPAddress GetDomainIp(string domain)
        {
            if (IPAddress.TryParse(domain, out IPAddress ip))
            {
                return ip;
            }
            var list = Dns.GetHostEntry(domain).AddressList;
            return list[0];
        }

        /// <summary>
        /// 地址转数组，端口必须2字节
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static Memory<byte> EndpointToArray(string ip, ushort port)
        {
            return EndpointToArray(ip.ToBytes(), port.ToBytes());
        }
        /// <summary>
        /// 地址转数组，端口必须2字节
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static Memory<byte> EndpointToArray(Memory<byte> ip, Memory<byte> port)
        {
            Memory<byte> endpoint = new byte[ip.Length + port.Length];
            ip.CopyTo(endpoint.Slice(0, ip.Length));
            port.CopyTo(endpoint.Slice(ip.Length, port.Length));

            return endpoint;
        }
        /// <summary>
        /// 从数组获取端口
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static ushort PortFromArray(Memory<byte> array)
        {
            return array.Span.Slice(array.Length - 2, 2).ToUInt16();
        }
        /// <summary>
        /// 从数组中解析地址，端口必须2字节
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static IPEndPoint EndpointFromArray(Memory<byte> array)
        {
            var span = array.Span;
            string ip = span.Slice(0, array.Length - 2).GetString();
            int port = span.Slice(array.Length - 2, 2).ToUInt16();
            return new IPEndPoint(GetDomainIp(ip), port);
        }


        public static byte[] Mac2Bytes(string mac)
        {
            mac = mac.Replace("-", "").Replace(":", "");
            byte[] res = new byte[mac.Length / 2];
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = Convert.ToByte(mac.Substring(i * 2, 2), 16);
            }
            return res;
        }
        public static byte[] MagicPacket(string mac)
        {
            var macBytes = Mac2Bytes(mac);
            var res = new byte[6 + 16 * macBytes.Length];
            for (int i = 0; i < 6; i++)
            {
                res[i] = 0xff;
            }
            for (int i = 1; i <= 16; i++)
            {
                Array.Copy(macBytes, 0, res, i * 6, macBytes.Length);
            }

            return res;
        }



#if DISABLE_IPV6 || (!UNITY_EDITOR && ENABLE_IL2CPP && !UNITY_2018_3_OR_NEWER)
            public static bool  IPv6Support = false;
#elif !UNITY_2019_1_OR_NEWER && !UNITY_2018_4_OR_NEWER && (!UNITY_EDITOR && ENABLE_IL2CPP && UNITY_2018_3_OR_NEWER)
           public static bool   IPv6Support = Socket.OSSupportsIPv6 && int.Parse(UnityEngine.Application.unityVersion.Remove(UnityEngine.Application.unityVersion.IndexOf('f')).Split('.')[2]) >= 6;
#elif UNITY_2018_2_OR_NEWER
           public static bool   IPv6Support = Socket.OSSupportsIPv6;
#elif UNITY
#pragma warning disable 618
           public static bool   IPv6Support = Socket.SupportsIPv6;
#pragma warning restore 618
#else
        public static bool IPv6Support = Socket.OSSupportsIPv6;
#endif
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace common.libs
{
    public static class NetworkHelper
    {
        /// <summary>
        /// 获取路由层数，自己与外网距离几个网关，用于发送一个对方网络收不到没有回应的数据包
        /// </summary>
        /// <returns></returns>
        public static ushort GetRouteLevel(out List<IPAddress> ips)
        {
            ips = new List<IPAddress>();
            try
            {
                List<string> starts = new() { "10.", "100.", "192.168.", "172." };
                var list = GetTraceRoute("www.baidu.com").ToList();
                for (ushort i = 0; i < list.Count(); i++)
                {
                    string ip = list.ElementAt(i).ToString();
                    if (ip.StartsWith(starts[0], StringComparison.Ordinal) || ip.StartsWith(starts[1], StringComparison.Ordinal) || ip.StartsWith(starts[2], StringComparison.Ordinal) || ip.StartsWith(starts[3], StringComparison.Ordinal))
                    {
                        if (ip.StartsWith(starts[2], StringComparison.Ordinal) == false)
                            ips.Add(list.ElementAt(i));
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
                ushort port = (ushort)rd.Next(20000, 60000);
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
        /// 域名解析
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public static IPAddress GetDomainIpV4(string domain)
        {
            if (IPAddress.TryParse(domain, out IPAddress ip))
            {
                return ip;
            }
            var list = Dns.GetHostEntry(domain).AddressList;
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i].AddressFamily == AddressFamily.InterNetwork) return list[i];
            }
            return list[0];
        }


        public static byte MaskLength(uint ip)
        {
            byte maskLength = 32;
            for (int i = 0; i < sizeof(uint); i++)
            {
                if (((ip >> (i * 8)) & 0x000000ff) != 0)
                {
                    break;
                }
                maskLength -= 8;
            }
            return maskLength;
        }
        public static uint MaskValue(byte maskLength)
        {
            //最多<<31 所以0需要单独计算
            if (maskLength < 1) return 0;
            return 0xffffffff << (32 - maskLength);
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

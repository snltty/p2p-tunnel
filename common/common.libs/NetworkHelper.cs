using common.libs.extends;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
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
        public static int GetRouteLevel()
        {
            try
            {
                List<string> starts = new() { "10.", "100.", "192.168.", "172." };
                IEnumerable<IPAddress> list = GetTraceRoute("www.baidu.com");
                for (int i = 0; i < list.Count(); i++)
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
            return -1;
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
        public static int GetRandomPort(List<int> usedPorts = null)
        {

            List<int> allPorts = GetUsedPort();
            if (usedPorts != null)
            {
                allPorts.AddRange(usedPorts);
            }
            Random rd = new();
            while (true)
            {
                int port = rd.Next(32000, 56000);
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
        public static List<int> GetUsedPort()
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

                List<int> allPorts = new();
                foreach (IPEndPoint ep in ipsTCP)
                {
                    allPorts.Add(ep.Port);
                }

                foreach (IPEndPoint ep in ipsUDP)
                {
                    allPorts.Add(ep.Port);
                }

                foreach (TcpConnectionInformation conn in tcpConnInfoArray)
                {
                    allPorts.Add(conn.LocalEndPoint.Port);
                }
                return allPorts;
            }
            catch (Exception)
            {
            }
            return new List<int>();

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
            return Dns.GetHostEntry(domain).AddressList[0];
        }

        /// <summary>
        /// 地址转数组，端口必须4字节
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static Memory<byte> EndpointToArray(string ip, int port)
        {
            return EndpointToArray(ip.ToBytes(), port.ToBytes());
        }
        /// <summary>
        /// 地址转数组，端口必须4字节
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
        public static int PortFromArray(Memory<byte> array)
        {
            return array.Span.Slice(array.Length - 4, 4).ToInt32();
        }
        /// <summary>
        /// 从数组中解析地址，端口必须4字节
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static IPEndPoint EndpointFromArray(Memory<byte> array)
        {
            var span = array.Span;
            string ip = span.Slice(0, array.Length - 4).GetString();
            int port = span.Slice(array.Length - 4, 4).ToInt32();
            return new IPEndPoint(GetDomainIp(ip), port);
        }

    }
}

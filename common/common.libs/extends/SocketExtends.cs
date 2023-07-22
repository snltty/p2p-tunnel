using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace common.libs.extends
{
    public static class SocketExtends
    {
        /// <summary>
        /// windows平台的udp无连接bug
        /// </summary>
        /// <param name="socket"></param>
        public static void WindowsUdpBug(this Socket socket)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    const uint IOC_IN = 0x80000000;
                    int IOC_VENDOR = 0x18000000;
                    int SIO_UDP_CONNRESET = (int)(IOC_IN | IOC_VENDOR | 12);
                    socket.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
                }
                catch (Exception)
                {
                }
            }
        }
        public static void IPv6Only(this Socket socket, AddressFamily family, bool val)
        {
            if (NetworkHelper.IPv6Support && family == AddressFamily.InterNetworkV6)
            {
                try
                {
                    socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, val);
                }
                catch (Exception)
                {
                }
            }
        }
        public static void SafeClose(this Socket socket)
        {
            if (socket != null)
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                    //调试注释
                    socket.Disconnect(false);
                }
                catch (Exception)
                {
                }
                finally
                {
                    socket.Close();
                }
            }
        }
        public static void Reuse(this Socket socket, bool reuse = true)
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, reuse);
        }
        public static void ReuseBind(this Socket socket, IPEndPoint ip)
        {
            socket.Reuse(true);
            socket.Bind(ip);
        }
        public static void KeepAlive(this Socket socket, int time = 60, int interval = 5, int retryCount = 5)
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, interval);
            //socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, retryCount);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, time);
        }
        private static byte[] keepaliveData = null;
        public static byte[] GetKeepAliveData()
        {
            if (keepaliveData == null)
            {
                uint dummy = 0;
                byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
                BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
                BitConverter.GetBytes((uint)3000).CopyTo(inOptionValues, Marshal.SizeOf(dummy));//keep-alive间隔
                BitConverter.GetBytes((uint)500).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);// 尝试间隔
                keepaliveData = inOptionValues;
            }
            return keepaliveData;
        }
    }
}

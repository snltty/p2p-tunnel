using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace common.libs.extends
{
    public static class SocketExtends
    {

        public static void SafeClose(this Socket socket)
        {
            if (socket != null)
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="time">多久没数据活动就发送一次</param>
        /// <param name="interval">间隔多久尝试一次</param>
        /// <param name="retryCount">尝试几次</param>
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

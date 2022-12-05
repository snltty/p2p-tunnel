using System.Net.Sockets;

namespace common.server.servers.iocp
{
    /// <summary>
    /// 
    /// </summary>
    public class BufferManager
    {
        int numBytes;
        byte[] buffer;
        int currentIndex;
        int bufferSize;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="totalBytes"></param>
        /// <param name="bufferSize"></param>
        public BufferManager(int totalBytes, int bufferSize)
        {
            numBytes = totalBytes;
            currentIndex = 0;
            this.bufferSize = bufferSize;
        }
        /// <summary>
        /// 
        /// </summary>
        public void InitBuffer()
        {
            buffer = new byte[numBytes];
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if ((numBytes - bufferSize) < currentIndex)
            {
                return false;
            }
            args.SetBuffer(buffer, currentIndex, bufferSize);
            currentIndex += bufferSize;

            return true;
        }
    }
}

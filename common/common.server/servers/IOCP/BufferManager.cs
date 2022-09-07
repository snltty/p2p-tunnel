using System.Net.Sockets;

namespace common.server.servers.iocp
{
    public class BufferManager
    {
        int numBytes;
        byte[] buffer;
        int currentIndex;
        int bufferSize;

        public BufferManager(int totalBytes, int bufferSize)
        {
            numBytes = totalBytes;
            currentIndex = 0;
            this.bufferSize = bufferSize;
        }

        public void InitBuffer()
        {
            buffer = new byte[numBytes];
        }

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

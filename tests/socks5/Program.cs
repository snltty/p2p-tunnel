namespace socks5
{
    internal class Program
    {
        static void Main(string[] args)
        {

            //客户端==============================================================

            ISocks5ClientListener listener = new Socks5ClientListener();
            ISocks5ServerHandler server = null;
            ISocks5ClientHandler client = new Socks5ClientHandler(listener);
            //客户端需要发送数据给服务端
            client.OnSendRequest = (data) =>
            {
                //这里应该是通过socket发送到服务端
                var bytes = data.ToBytes(out int length);

                Socks5Info info = Socks5Info.Debytes(bytes.AsMemory(0, length));
                //放一点自定义数据
                info.Tag = new ConnectionInfo();
                //把客户端来的数据输入给服务端去处理
                server.InputData(info);
                return true;
            };
            //客户端需要发送关闭消息给服务端
            client.OnSendClose = (data) => { };

            //客户端监听
            listener.Start(5000, 8 * 1024);


            //服务端===============================================================
            WheelTimer<object> wheelTimer = new WheelTimer<object>();
            server = new Socks5ServerHandler(wheelTimer);
            //服务端需要回复数据给客户端
            server.OnSendResponse = (data) =>
            {
                //拿到自定义数据
                ConnectionInfo info = data.Tag as ConnectionInfo;

                //这里应该是通过socket发送到客户端
                var bytes = data.ToBytes(out int length);
                //把客户端来的数据输入给服务端去处理
                client.InputData(bytes.AsMemory(0, length));
            };
            //服务端需要发送关闭消息给客户端
            server.OnSendClose = (data) => { };


           


            Console.ReadLine();
        }
    }

    public class ConnectionInfo
    {
        /// <summary>
        /// 客户端唯一id
        /// </summary>
        public ulong Id { get; set; }
    }
}
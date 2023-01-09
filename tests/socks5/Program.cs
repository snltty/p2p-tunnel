using common.libs;
using common.socks5;

namespace socks5
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //定时任务
            WheelTimer<object> wheelTimer = new WheelTimer<object>();
            //目标ip提供
            ISocks5DstEndpointProvider socks5DstEndpointProvider = new Socks5DstEndpointProvider();
            //消息发送
            Socks5MessengerSender socks5MessengerSender = new Socks5MessengerSender();
            //配置
            Config config = new Config { BufferSize = 8 * 1027, ConnectEnable = true, ListenPort = 5412 };
            //socks5验证，是否能连接什么的
            ISocks5Validator validator = new DefaultSocks5Validator(config);

            //客户端监听
            ISocks5ClientListener listener = new Socks5ClientListener();
            listener.Start(config.ListenPort, config.BufferSize);

            //客户端处理
            ISocks5ClientHandler client = new Socks5ClientHandler(socks5MessengerSender, socks5DstEndpointProvider, listener);
            //服务端处理
            ISocks5ServerHandler server = new Socks5ServerHandler(socks5MessengerSender, config, wheelTimer, validator);

            socks5MessengerSender.Client = client;
            socks5MessengerSender.Server = server;

            Console.WriteLine($"已开启:{config.ListenPort}");

            Console.ReadLine();
        }
    }

    /// <summary>
    /// 自定义客户端服务端通信
    /// 如果是socks5客户端和socks5服务端都放在服务器，不分离，那就如下面这也，直接流转即可
    /// 分离的话，就要自己使用socket和服务端通信
    /// </summary>
    public class Socks5MessengerSender : ISocks5MessengerSender
    {
        public ISocks5ClientHandler Client { get; set; }
        public ISocks5ServerHandler Server { get; set; }

        private ulong clientid = 1;

        //发给服务端
        public bool Request(Socks5Info data)
        {
            Socks5Info info = new Socks5Info { ClientId = clientid, Version = data.Version, Id = data.Id, Data = data.Data, Socks5Step = data.Socks5Step, SourceEP = data.SourceEP, TargetEP = data.TargetEP };
            Server.InputData(info);
            return true;
        }
        public void RequestClose(Socks5Info data)
        {
            Socks5Info info = new Socks5Info { ClientId = clientid, Version = data.Version, Id = data.Id, Data = Helper.EmptyArray, Socks5Step = Socks5EnumStep.Forward };
            Server.InputData(info);
        }
        //发给客户端
        public void Response(Socks5Info data)
        {
            Client.InputData(data);
        }
        public void ResponseClose(Socks5Info data)
        {
            data.Data = Helper.EmptyArray;
            Client.InputData(data);
        }
    }

}
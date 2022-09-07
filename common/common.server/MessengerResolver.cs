using common.libs;
using common.libs.extends;
using common.server.middleware;
using common.server.model;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace common.server
{
    public class MessengerResolver
    {

        private readonly Dictionary<ReadOnlyMemory<byte>, MessengerCacheInfo> messengers = new(new MemoryByteDictionaryComparer());

        private readonly ITcpServer tcpserver;
        private readonly IUdpServer udpserver;
        private readonly MessengerSender messengerSender;
        private readonly MiddlewareTransfer middlewareTransfer;
        private readonly ISourceConnectionSelector sourceConnectionSelector;


        public MessengerResolver(IUdpServer udpserver, ITcpServer tcpserver, MessengerSender messengerSender, MiddlewareTransfer middlewareTransfer, ISourceConnectionSelector sourceConnectionSelector)
        {
            this.tcpserver = tcpserver;
            this.udpserver = udpserver;
            this.messengerSender = messengerSender;
            this.middlewareTransfer = middlewareTransfer;

            this.tcpserver.OnPacket.Sub((IConnection connection) =>
            {
                InputData(connection).Wait();
            });
            this.udpserver.OnPacket.Sub((IConnection connection) =>
            {
                connection.UpdateTime(DateTimeHelper.GetTimeStamp());
                InputData(connection).Wait();
            });
            this.sourceConnectionSelector = sourceConnectionSelector;   
        }
        public void LoadMessenger(Type type, object obj)
        {
            Type voidType = typeof(void);
            string path = type.Name.Replace("Messenger", "");
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                Memory<byte> key = $"{path}/{method.Name}".ToLower().ToBytes().AsMemory();
                if (!messengers.ContainsKey(key))
                {
                    MessengerCacheInfo cache = new MessengerCacheInfo
                    {
                        IsVoid = method.ReturnType == voidType,
                        Method = method,
                        Target = obj,
                        IsTask = method.ReturnType.GetProperty("IsCompleted") != null && method.ReturnType.GetMethod("GetAwaiter") != null,
                        IsTaskResult = method.ReturnType.GetProperty("Result") != null
                    };
                    messengers.TryAdd(key, cache);
                }

            }
        }

        public async Task InputData(IConnection connection)
        {
            var receive = connection.ReceiveData;
            var responseWrap = connection.ReceiveResponseWrap;
            var requestWrap = connection.ReceiveRequestWrap;

            MessageTypes type = (MessageTypes)receive.Span[0];
            //回复的消息
            if (type == MessageTypes.RESPONSE)
            {
                responseWrap.FromArray(receive);
                if (connection.EncodeEnabled)
                {
                    responseWrap.Memory = connection.Crypto.Decode(responseWrap.Memory);
                }
                messengerSender.Response(responseWrap);
                return;
            }

            requestWrap.FromArray(receive);
            connection.FromConnection = sourceConnectionSelector.Select(connection);
            if (connection.EncodeEnabled)
            {
                requestWrap.Memory = connection.Crypto.Decode(requestWrap.Memory);
            }
            try
            {
                //404,没这个插件
                if (!messengers.ContainsKey(requestWrap.MemoryPath))
                {

                    Logger.Instance.Error($"{requestWrap.MemoryPath.Span.GetString()},{receive.Length},{connection.ServerType}, not found");
                    await messengerSender.ReplyOnly(new MessageResponseWrap
                    {
                        Connection = connection,
                        RequestId = requestWrap.RequestId,
                        Code = MessageResponeCodes.NOT_FOUND
                    }).ConfigureAwait(false);
                    return;
                }

                MessengerCacheInfo plugin = messengers[requestWrap.MemoryPath];

                if (middlewareTransfer != null)
                {
                    var res = middlewareTransfer.Execute(connection);
                    if (!res.Item1)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            RequestId = requestWrap.RequestId,
                            Code = MessageResponeCodes.ERROR,
                            Memory = res.Item2
                        }).ConfigureAwait(false);
                        return;
                    }
                }

                object resultAsync = plugin.Method.Invoke(plugin.Target, new object[] { connection });
                //void的，task的 没有返回值，不回复，需要回复的可以返回任意类型
                if (plugin.IsVoid)
                {
                    return;
                }

                byte[] resultObject = Helper.EmptyArray ;
                if (plugin.IsTask)
                {
                    if (plugin.IsTaskResult)
                    {
                        var task = resultAsync as Task<byte[]>;
                        await task.ConfigureAwait(false);
                        resultObject = task.Result;
                    }
                    else
                    {
                        return;
                    }
                }
                else if(resultAsync != null)
                {
                    resultObject = resultAsync as byte[];
                }
                await messengerSender.ReplyOnly(new MessageResponseWrap
                {
                    Connection = connection,
                    Memory = resultObject,
                    RequestId = requestWrap.RequestId
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
                await messengerSender.ReplyOnly(new MessageResponseWrap
                {
                    Connection = connection,
                    RequestId = requestWrap.RequestId,
                    Code = MessageResponeCodes.ERROR
                }).ConfigureAwait(false);
            }
            finally
            {
                requestWrap.Reset();
            }
        }

        private struct MessengerCacheInfo
        {
            public object Target { get; set; }
            public MethodInfo Method { get; set; }
            public bool IsVoid { get; set; }
            public bool IsTask { get; set; }
            public bool IsTaskResult { get; set; }
        }
    }
}
using common.libs;
using common.server.middleware;
using common.server.model;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace common.server
{
    public class MessengerResolver
    {

        private readonly Dictionary<ushort, MessengerCacheInfo> messengers = new();

        private readonly ITcpServer tcpserver;
        private readonly IUdpServer udpserver;
        private readonly MessengerSender messengerSender;
        private readonly MiddlewareTransfer middlewareTransfer;
        private readonly ISourceConnectionSelector sourceConnectionSelector;
        private readonly IRelayValidator relayValidator;

        public MessengerResolver(IUdpServer udpserver, ITcpServer tcpserver, MessengerSender messengerSender, MiddlewareTransfer middlewareTransfer, ISourceConnectionSelector sourceConnectionSelector, IRelayValidator relayValidator)
        {
            this.tcpserver = tcpserver;
            this.udpserver = udpserver;
            this.messengerSender = messengerSender;
            this.middlewareTransfer = middlewareTransfer;

            this.tcpserver.OnPacket = InputData;
            this.udpserver.OnPacket = InputData;
            this.sourceConnectionSelector = sourceConnectionSelector;
            this.relayValidator = relayValidator;
        }

        public void LoadMessenger(Type type, object obj)
        {
            Type voidType = typeof(void);
            Type midType = typeof(MessengerIdAttribute);
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                MessengerIdAttribute mid = method.GetCustomAttribute(midType) as MessengerIdAttribute;
                if (messengers.ContainsKey(mid.Id) == false)
                {
                    MessengerCacheInfo cache = new MessengerCacheInfo
                    {
                        IsVoid = method.ReturnType == voidType,
                        Method = method,
                        Target = obj,
                        IsTask = method.ReturnType.GetProperty("IsCompleted") != null && method.ReturnType.GetMethod("GetAwaiter") != null,
                        IsTaskResult = method.ReturnType.GetProperty("Result") != null
                    };
                    messengers.TryAdd(mid.Id, cache);
                }
                else
                {
                    Logger.Instance.Error($"{type.Name}->{method.Name}->{mid.Id} 消息id已存在");
                }
            }
        }

        public async Task InputData(IConnection connection)
        {
            var receive = connection.ReceiveData;
            //去掉表示数据长度的4字节
            var readReceive = receive.Slice(4);
            var responseWrap = connection.ReceiveResponseWrap;
            var requestWrap = connection.ReceiveRequestWrap;

            try
            {
                //回复的消息
                if ((MessageTypes)readReceive.Span[0] == MessageTypes.RESPONSE)
                {
                    responseWrap.FromArray(readReceive);
                    //是中继的回复
                    if (responseWrap.RelayId > 0)
                    {
                        //目的地连接对象
                        IConnection _connection = sourceConnectionSelector.SelectTarget(connection, responseWrap.RelaySourceId, responseWrap.RelayId);
                        if (_connection == null) return;

                        //接下来的目的地是最终的节点
                        if (_connection.RelayConnectId == responseWrap.RelayId)
                        {
                            MessageResponseWrap.WriteRelayId(readReceive, 0);
                        }
                        //中继数据不再次序列化，直接在原数据上更新数据然后发送
                        await _connection.Send(receive).ConfigureAwait(false);
                    }
                    else
                    {
                        if (connection.EncodeEnabled)
                        {
                            responseWrap.Payload = connection.Crypto.Decode(responseWrap.Payload);
                        }
                        messengerSender.Response(responseWrap);
                    }
                    return;
                }

                //新的请求
                requestWrap.FromArray(readReceive);
                //是中继数据
                if (requestWrap.RelayId > 0)
                {

                    if ((requestWrap.MessengerId >= (ushort)RelayMessengerIds.Min && requestWrap.MessengerId <= (ushort)RelayMessengerIds.Max) || relayValidator.Validate(connection))
                    {
                        //第一个节点收到中继数据，它知道是哪里来的，填写一下数据来源
                        if (requestWrap.RelaySourceId == 0)
                        {
                            requestWrap.RelaySourceId = connection.ConnectId;
                            MessageRequestWrap.WriteRelaySourceId(readReceive, connection.ConnectId);
                        }

                        //目的地连接对象
                        IConnection _connection = sourceConnectionSelector.SelectTarget(connection, requestWrap.RelaySourceId, requestWrap.RelayId);
                        if (_connection == null) return;

                        //接下来的目的地是最终的节点
                        if (_connection.RelayConnectId == requestWrap.RelayId)
                        {
                            MessageRequestWrap.WriteRelayId(readReceive, 0);
                        }
                        //中继数据不再次序列化，直接在原数据上更新数据然后发送
                        await _connection.Send(receive).ConfigureAwait(false);
                    }
                    return;
                }



                if (connection.EncodeEnabled)
                {
                    requestWrap.Payload = connection.Crypto.Decode(requestWrap.Payload);
                }
                connection.FromConnection = sourceConnectionSelector.SelectSource(connection, requestWrap.RelaySourceId);

                //404,没这个插件
                if (!messengers.ContainsKey(requestWrap.MessengerId))
                {

                    Logger.Instance.Error($"{requestWrap.MessengerId},{receive.Length},{connection.ServerType}, not found");
                    await messengerSender.ReplyOnly(new MessageResponseWrap
                    {
                        Connection = connection,
                        RequestId = requestWrap.RequestId,
                        RelayId = requestWrap.RelaySourceId,
                        RelaySourceId = requestWrap.RelayId,
                        Code = MessageResponeCodes.NOT_FOUND
                    }).ConfigureAwait(false);
                    return;
                }

                MessengerCacheInfo plugin = messengers[requestWrap.MessengerId];

                if (middlewareTransfer != null)
                {
                    var middleres = await middlewareTransfer.Execute(connection);
                    if (middleres.Item1 == false)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            RequestId = requestWrap.RequestId,
                            RelayId = requestWrap.RelaySourceId,
                            RelaySourceId = requestWrap.RelayId,
                            Code = MessageResponeCodes.ERROR,
                            Payload = middleres.Item2
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

                byte[] resultObject = Helper.EmptyArray;
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
                else if (resultAsync != null)
                {
                    resultObject = resultAsync as byte[];
                }
                bool res = await messengerSender.ReplyOnly(new MessageResponseWrap
                {
                    Connection = connection,
                    Payload = resultObject,
                    RelayId = requestWrap.RelaySourceId,
                    RelaySourceId = requestWrap.RelayId,
                    RequestId = requestWrap.RequestId
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
                await messengerSender.ReplyOnly(new MessageResponseWrap
                {
                    Connection = connection,
                    RelayId = requestWrap.RelaySourceId,
                    RelaySourceId = requestWrap.RelayId,
                    RequestId = requestWrap.RequestId,
                    Code = MessageResponeCodes.ERROR
                }).ConfigureAwait(false);
            }
            finally
            {
                //requestWrap.Reset();
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
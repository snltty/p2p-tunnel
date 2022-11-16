using common.libs;
using common.libs.extends;
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
                if ((MessageTypes)(readReceive.Span[0] & MessageRequestWrap.TypeBits) == MessageTypes.RESPONSE)
                {
                    responseWrap.FromArray(readReceive);
                    //是中继的回复
                    if (responseWrap.RelayIdLength > 0)
                    {
                        ulong currentRelayid = responseWrap.RelayIds.Span.ToUInt64();
                        //目的地连接对象
                        IConnection _connection = sourceConnectionSelector.SelectTarget(connection, currentRelayid);
                        if (_connection == null) return;

                        //去掉一个id   然后 length-1
                        //0000 type length:2 11111111 00000000 ----> 00000000 0000 type length:1 00000000
                        receive.Slice(0, MessageRequestWrap.HeaderLength).CopyTo(receive.Slice(MessageRequestWrap.RelayIdSize));
                        receive.Span[MessageRequestWrap.RelayIdLengthPos + MessageRequestWrap.RelayIdSize] = (byte)(responseWrap.RelayIdLength - 1);
                        //中继数据不再次序列化，直接在原数据上更新数据然后发送
                        await _connection.Send(receive.Slice(MessageRequestWrap.RelayIdSize)).ConfigureAwait(false);
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
                if (requestWrap.Relay)
                {
                    //还在路上
                    if (requestWrap.RelayIdLength - 1 - requestWrap.RelayIdIndex >= 0)
                    {
                        if (relayValidator.Validate(connection))
                        {
                            //需要等待回复则 有index， 不等待回复则没有，那么。同意这么计算偏移量，requestWrap.RelayIdIndex * MessageRequestWrap.RelayIdSize
                            //只需要 没有index时，默认为0即可
                            ulong currentRelayid = requestWrap.RelayIds.Span.Slice(requestWrap.RelayIdIndex * MessageRequestWrap.RelayIdSize).ToUInt64();

                            //目的地连接对象
                            IConnection _connection = sourceConnectionSelector.SelectTarget(connection, currentRelayid);
                            if (_connection == null) return;

                            if (requestWrap.Reply)
                            {
                                //RelayIdIndex 后移一位
                                receive.Span[MessageRequestWrap.RelayIdIndexPos]++;
                            }
                            else
                            {
                                //去掉一个id   然后 length-1
                                //0000 type length:2 11111111 00000000 ----> 00000000 0000 type length:1 00000000
                                receive.Slice(0, MessageRequestWrap.HeaderLength).CopyTo(receive.Slice(MessageRequestWrap.RelayIdSize));
                                receive.Span[MessageRequestWrap.RelayIdLengthPos + MessageRequestWrap.RelayIdSize] -= 1;
                                receive = receive.Slice(MessageRequestWrap.RelayIdSize);
                            }
                            //中继数据不再次序列化，直接在原数据上更新数据然后发送
                            await _connection.Send(receive.Slice(MessageRequestWrap.RelayIdSize)).ConfigureAwait(false);
                        }
                        return;
                    }
                }

                if (connection.EncodeEnabled)
                {
                    requestWrap.Payload = connection.Crypto.Decode(requestWrap.Payload);
                }

                connection.FromConnection = connection;
                if (requestWrap.Relay)
                {
                    connection.FromConnection = sourceConnectionSelector.SelectSource(connection, requestWrap.RelayIds.Span.ToUInt64());
                }
               

                //404,没这个插件
                if (!messengers.ContainsKey(requestWrap.MessengerId))
                {

                    Logger.Instance.Error($"{requestWrap.MessengerId},{receive.Length},{connection.ServerType}, not found");
                    if (requestWrap.Reply)
                    {
                        await messengerSender.ReplyOnly(new MessageResponseWrap
                        {
                            Connection = connection,
                            RequestId = requestWrap.RequestId,
                            RelayIds = requestWrap.RelayIds,
                            Code = MessageResponeCodes.NOT_FOUND
                        }).ConfigureAwait(false);
                    }
                    return;
                }

                MessengerCacheInfo plugin = messengers[requestWrap.MessengerId];

                if (middlewareTransfer != null)
                {
                    var middleres = await middlewareTransfer.Execute(connection);
                    if (middleres.Item1 == false)
                    {
                        if (requestWrap.Reply)
                        {
                            await messengerSender.ReplyOnly(new MessageResponseWrap
                            {
                                Connection = connection,
                                RequestId = requestWrap.RequestId,
                                RelayIds = requestWrap.RelayIds,
                                Code = MessageResponeCodes.ERROR,
                                Payload = middleres.Item2
                            }).ConfigureAwait(false);
                        }

                        return;
                    }
                }
                object resultAsync = plugin.Method.Invoke(plugin.Target, new object[] { connection });
                //void的，task的 没有返回值，不回复，需要回复的可以返回任意类型
                if (plugin.IsVoid || requestWrap.Reply == false)
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
                    RelayIds = requestWrap.RelayIds,
                    RequestId = requestWrap.RequestId
                }).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
                if (requestWrap.Reply)
                {
                    await messengerSender.ReplyOnly(new MessageResponseWrap
                    {
                        Connection = connection,
                        RelayIds = requestWrap.RelayIds,
                        RequestId = requestWrap.RequestId,
                        Code = MessageResponeCodes.ERROR
                    }).ConfigureAwait(false);
                }
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
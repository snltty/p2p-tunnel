using common.libs;
using common.libs.extends;
using common.server.model;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace common.server
{
    /// <summary>
    /// 消息处理总线
    /// </summary>
    public sealed class MessengerResolver
    {

        private readonly Dictionary<ushort, MessengerCacheInfo> messengers = new();

        private readonly ITcpServer tcpserver;
        private readonly IUdpServer udpserver;
        private readonly MessengerSender messengerSender;
        private readonly IRelaySourceConnectionSelector sourceConnectionSelector;
        private readonly IRelayValidator relayValidator;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="udpserver"></param>
        /// <param name="tcpserver"></param>
        /// <param name="messengerSender"></param>
        /// <param name="sourceConnectionSelector"></param>
        /// <param name="relayValidator"></param>
        public MessengerResolver(IUdpServer udpserver, ITcpServer tcpserver, MessengerSender messengerSender, IRelaySourceConnectionSelector sourceConnectionSelector, IRelayValidator relayValidator)
        {
            this.tcpserver = tcpserver;
            this.udpserver = udpserver;
            this.messengerSender = messengerSender;

            this.tcpserver.OnPacket = InputData;
            this.udpserver.OnPacket = InputData;
            this.sourceConnectionSelector = sourceConnectionSelector;
            this.relayValidator = relayValidator;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="obj"></param>
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

        /// <summary>
        /// 收到消息
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
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
                    if (responseWrap.Relay && responseWrap.RelayIdLength - 1 - responseWrap.RelayIdIndex >= 0)
                    {
                        ulong nextId = responseWrap.RelayIds.Span.Slice(responseWrap.RelayIdIndex * MessageRequestWrap.RelayIdSize).ToUInt64();

                        //目的地连接对象
                        IConnection _connection = sourceConnectionSelector.Select(connection, nextId);
                        if (_connection == null || ReferenceEquals(connection, _connection)) return;

                        //RelayIdIndex 后移一位
                        receive.Span[MessageRequestWrap.RelayIdIndexPos]++;

                        _connection.WaitOne();
                        await _connection.Send(receive).ConfigureAwait(false);
                        _connection.Release();
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
                            ulong nextId = requestWrap.RelayIds.Span.Slice(requestWrap.RelayIdIndex * MessageRequestWrap.RelayIdSize).ToUInt64();

                            //目的地连接对象
                            IConnection _connection = sourceConnectionSelector.Select(connection, nextId);
                            if (_connection == null || ReferenceEquals(connection, _connection)) return;

                            //RelayIdIndex 后移一位
                            receive.Span[MessageRequestWrap.RelayIdIndexPos]++;

                            _connection.WaitOne();
                            //中继数据不再次序列化，直接在原数据上更新数据然后发送
                            await _connection.Send(receive).ConfigureAwait(false);
                            _connection.Release();
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
                    connection.FromConnection = sourceConnectionSelector.Select(connection, requestWrap.RelayIds.Span.ToUInt64());
                }


                //404,没这个插件
                if (messengers.ContainsKey(requestWrap.MessengerId) == false)
                {
                    Logger.Instance.DebugError($"{requestWrap.MessengerId},{connection.ServerType}, not found");
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
                object resultAsync = plugin.Method.Invoke(plugin.Target, new object[] { connection });
                Memory<byte> resultObject = null;
                if (plugin.IsVoid)
                {
                    if (connection.ResponseDataLength <= 0) return;
                    resultObject = connection.ResponseData.AsMemory(0, connection.ResponseDataLength);
                }
                else
                {
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
                            var task = resultAsync as Task;
                            await task.ConfigureAwait(false);

                            if (connection.ResponseDataLength <= 0) return;
                            resultObject = connection.ResponseData.AsMemory(0, connection.ResponseDataLength);
                        }
                    }
                    else if (resultAsync != null)
                    {
                        resultObject = resultAsync as byte[];
                    }
                }

                if (requestWrap.Reply == true && resultObject.Length > 0)
                {
                    bool res = await messengerSender.ReplyOnly(new MessageResponseWrap
                    {
                        Connection = connection,
                        Payload = resultObject,
                        RelayIds = requestWrap.RelayIds,
                        RequestId = requestWrap.RequestId
                    }).ConfigureAwait(false);
                }

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
                connection.Return();
            }

        }

        /// <summary>
        /// 消息插件缓存
        /// </summary>
        private struct MessengerCacheInfo
        {
            /// <summary>
            /// 对象
            /// </summary>
            public object Target { get; set; }
            /// <summary>
            /// 方法
            /// </summary>
            public MethodInfo Method { get; set; }
            /// <summary>
            /// 是否void
            /// </summary>
            public bool IsVoid { get; set; }
            /// <summary>
            /// 是否task
            /// </summary>
            public bool IsTask { get; set; }
            /// <summary>
            /// 是否task 带result
            /// </summary>
            public bool IsTaskResult { get; set; }
        }
    }
}
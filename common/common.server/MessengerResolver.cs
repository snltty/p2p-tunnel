using common.libs;
using common.libs.extends;
using common.server.model;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace common.server
{
    /// <summary>
    /// 消息处理总线
    /// </summary>
    public sealed class MessengerResolver
    {
        delegate void VoidDelegate(IConnection connection);
        delegate Task TaskDelegate(IConnection connection);

        private readonly Dictionary<ushort, MessengerCacheInfo> messengers = new();

        private readonly ITcpServer tcpserver;
        private readonly IUdpServer udpserver;
        private readonly MessengerSender messengerSender;
        private readonly IRelaySourceConnectionSelector sourceConnectionSelector;
        private readonly IRelayValidator relayValidator;
        private readonly ServiceProvider serviceProvider;


        public MessengerResolver(IUdpServer udpserver, ITcpServer tcpserver, MessengerSender messengerSender,
            IRelaySourceConnectionSelector sourceConnectionSelector, IRelayValidator relayValidator, ServiceProvider serviceProvider)
        {
            this.tcpserver = tcpserver;
            this.udpserver = udpserver;
            this.messengerSender = messengerSender;

            this.tcpserver.OnPacket = InputData;
            this.udpserver.OnPacket = InputData;
            this.sourceConnectionSelector = sourceConnectionSelector;
            this.relayValidator = relayValidator;
            this.serviceProvider = serviceProvider;
        }

        public void LoadMessenger(Assembly[] assemblys)
        {
            Type voidType = typeof(void);
            Type midType = typeof(MessengerIdAttribute);

            foreach (Type type in ReflectionHelper.GetInterfaceSchieves(assemblys, typeof(IMessenger)).Distinct())
            {
                object obj = serviceProvider.GetService(type);

                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    MessengerIdAttribute mid = method.GetCustomAttribute(midType) as MessengerIdAttribute;
                    if (mid != null)
                    {
                        if (messengers.ContainsKey(mid.Id) == false)
                        {
                            MessengerCacheInfo cache = new MessengerCacheInfo
                            {
                                Target = obj
                            };
                            if (method.ReturnType == voidType)
                            {
                                cache.VoidMethod = (VoidDelegate)Delegate.CreateDelegate(typeof(VoidDelegate), obj, method);
                            }
                            else if (method.ReturnType.GetProperty("IsCompleted") != null && method.ReturnType.GetMethod("GetAwaiter") != null)
                            {
                                cache.TaskMethod = (TaskDelegate)Delegate.CreateDelegate(typeof(TaskDelegate), obj, method);
                            }

                            messengers.TryAdd(mid.Id, cache);
                        }
                        else
                        {
                            Logger.Instance.Error($"{type.Name}->{method.Name}->{mid.Id} 消息id已存在");
                        }
                    }
                }
            }


            List<Tuple<string, ushort, ushort>> ushorts = ReflectionHelper.GetEnums(assemblys).Where(c => c.Name.EndsWith("MessengerIds")).Distinct().Select(item =>
            {
                var fields = item.GetFields(BindingFlags.Static | BindingFlags.Public).Select(c => (ushort)c.GetValue(null));
                return new Tuple<string, ushort, ushort>(item.Name, fields.Min(), fields.Max());
            }).OrderBy(c => c.Item2).ToList();

            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
            Logger.Instance.Debug($"枚举类型ushort，已存在消息列表如下:");
            foreach (var item in ushorts)
            {
                Logger.Instance.Info($"{item.Item1.PadLeft(32, '-')}  {item.Item2}-{item.Item3}");
            }
            Logger.Instance.Warning(string.Empty.PadRight(Logger.Instance.PaddingWidth, '='));
        }

        public bool GetMessenger(ushort id, out object obj)
        {
            obj = null;
            if (messengers.TryGetValue(id, out MessengerCacheInfo cache))
            {
                obj = cache.Target;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 收到消息
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public async Task InputData(IConnection connection)
        {
            Memory<byte> receive = connection.ReceiveData;
            //去掉表示数据长度的4字节
            Memory<byte> readReceive = receive.Slice(4);
            MessageResponseWrap responseWrap = connection.ReceiveResponseWrap;
            MessageRequestWrap requestWrap = connection.ReceiveRequestWrap;

            /*
             * 中继
             * request   A<-->B<-->C  计入A流量
             * response  C<-->B<-->A  计入A流量
             */

            connection.FromConnection = connection;
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
                        if(_connection.SendDenied == 0)
                        {
                            await _connection.WaitOne();
                            await _connection.Send(receive).ConfigureAwait(false);
                            _connection.SentBytes += (ulong)receive.Length;
                            _connection.Release();
                        }
                    }
                    else
                    {
                        if (connection.EncodeEnabled && responseWrap.Encode)
                        {
                            if (responseWrap.Relay)
                            {
                                connection.FromConnection = sourceConnectionSelector.Select(connection, responseWrap.RelayIds.Span.ToUInt64());
                            }
                            responseWrap.Payload = connection.FromConnection.Crypto.Decode(responseWrap.Payload);
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
                            if (_connection.SendDenied == 0)
                            {
                                await _connection.WaitOne();
                                //中继数据不再次序列化，直接在原数据上更新数据然后发送
                                await _connection.Send(receive).ConfigureAwait(false);
                                connection.SentBytes += (ulong)receive.Length;
                                _connection.Release();
                            }
                        }
                        return;
                    }
                }

                if (requestWrap.Relay)
                {
                    connection.FromConnection = sourceConnectionSelector.Select(connection, requestWrap.RelayIds.Span.ToUInt64());
                }
                IConnection responseConnection = connection;
                if (connection.EncodeEnabled && requestWrap.Encode)
                {
                    responseConnection = connection.FromConnection;
                    requestWrap.Payload = connection.FromConnection.Crypto.Decode(requestWrap.Payload);
                }
                //404,没这个插件
                if (messengers.ContainsKey(requestWrap.MessengerId) == false)
                {
                    Logger.Instance.DebugError($"{requestWrap.MessengerId},{connection.ServerType}, not found");
                    return;
                }

                MessengerCacheInfo plugin = messengers[requestWrap.MessengerId];
                if (plugin.VoidMethod != null)
                {
                    plugin.VoidMethod(connection);
                }
                else if (plugin.TaskMethod != null)
                {
                    await plugin.TaskMethod(connection);
                }

                if (requestWrap.Reply == true)
                {
                    bool res = await messengerSender.ReplyOnly(new MessageResponseWrap
                    {
                        Connection = responseConnection,
                        Encode = requestWrap.Encode,
                        Payload = connection.ResponseData,
                        RelayIds = requestWrap.RelayIds,
                        RequestId = requestWrap.RequestId
                    }).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.DebugError(ex);
                if (receive.Length > 1024)
                {
                    Logger.Instance.Error($"{connection.Address}:{string.Join(",", receive.Slice(0, 1024).ToArray())}");
                }
                else
                {
                    Logger.Instance.Error($"{connection.Address}:{string.Join(",", receive.ToArray())}");
                }
                //connection.Disponse();
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
            /// 空返回方法
            /// </summary>
            public VoidDelegate VoidMethod { get; set; }
            /// <summary>
            /// Task返回方法
            /// </summary>
            public TaskDelegate TaskMethod { get; set; }
        }
    }
}
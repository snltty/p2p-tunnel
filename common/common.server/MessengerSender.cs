using common.libs;
using common.libs.extends;
using common.server.model;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace common.server
{
    /// <summary>
    /// 消息发送器
    /// </summary>
    public class MessengerSender
    {
        private NumberSpace requestIdNumberSpace = new NumberSpace(0);
        private WheelTimer<TimeoutState> wheelTimer = new WheelTimer<TimeoutState>();
        private ConcurrentDictionary<ulong, WheelTimerTimeout<TimeoutState>> sends = new ConcurrentDictionary<ulong, WheelTimerTimeout<TimeoutState>>();
        private Memory<byte> sendOnlyPath = "relay/sendonly".ToBytes();
        private Memory<byte> sendReplyPath = "relay/sendreply".ToBytes();

        public MessengerSender()
        {
        }

        /// <summary>
        /// 发送并等待回复
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task<MessageResponeInfo> SendReply(MessageRequestWrap msg)
        {
            if (msg.RequestId == 0)
            {
                msg.RequestId = requestIdNumberSpace.Increment();
            }
            WheelTimerTimeout<TimeoutState> timeout = NewReply(msg.RequestId, msg.Timeout);
            if (!await SendOnly(msg).ConfigureAwait(false))
            {
                sends.TryRemove(msg.RequestId, out _);
                timeout.Cancel();
                timeout.Task.State.Tcs.SetResult(new MessageResponeInfo { Code = MessageResponeCodes.NOT_CONNECT });
            }
            return await timeout.Task.State.Tcs.Task.ConfigureAwait(false);
        }
        /// <summary>
        /// 只发送，不等回复
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task<bool> SendOnly(MessageRequestWrap msg)
        {
            try
            {
                bool reply = msg.RequestId > 0;
                if (msg.RequestId == 0)
                {
                    msg.RequestId = requestIdNumberSpace.Increment();
                }
                if (msg.Connection == null)
                {
                    return false;
                }

                if (msg.Connection.Relay)
                {
                    msg.Memory = new RelayParamsInfo
                    {
                        Data = msg.Memory,
                        Path = msg.MemoryPath,
                        ToId = msg.Connection.ConnectId
                    }.ToBytes();
                    msg.MemoryPath = reply ? sendReplyPath : sendOnlyPath;
                }
                if (msg.Connection.EncodeEnabled)
                {
                    msg.Memory = msg.Connection.Crypto.Encode(msg.Memory);
                }

                byte[] bytes = msg.ToArray(msg.Connection.ServerType, out int length, true);
                bool res = await msg.Connection.Send(bytes, length).ConfigureAwait(false);
                msg.Return(bytes);

                if (res)
                {
                    msg.Connection.UpdateTime(DateTimeHelper.GetTimeStamp());
                }
                return res;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
            return false;
        }
        /// <summary>
        /// 回复远程消息，收到某个连接的消息后，通过这个再返回消息给它
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async ValueTask ReplyOnly(MessageResponseWrap msg)
        {
            try
            {

                if (msg.Connection.EncodeEnabled)
                {
                    msg.Memory = msg.Connection.Crypto.Encode(msg.Memory);
                }

                (byte[] bytes, int length) = msg.ToArray(msg.Connection.ServerType, true);
                bool res = await msg.Connection.Send(bytes, length).ConfigureAwait(false);
                msg.Return(bytes);

                if (res)
                {
                    msg.Connection.UpdateTime(DateTimeHelper.GetTimeStamp());
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Debug(ex);
            }
        }
        /// <summary>
        /// 回复本地消息，发送消息后，socket收到消息，通过这个方法回复给刚刚发送的对象
        /// </summary>
        /// <param name="wrap"></param>
        public void Response(MessageResponseWrap wrap)
        {
            if (sends.TryRemove(wrap.RequestId, out WheelTimerTimeout<TimeoutState> timeout))
            {
                timeout.Cancel();
                timeout.Task.State.Tcs.SetResult(new MessageResponeInfo { Code = wrap.Code, Data = wrap.Memory });
            }
        }

        private WheelTimerTimeout<TimeoutState> NewReply(ulong requestId, int timeoutDelay = 15000)
        {
            if (timeoutDelay <= 0)
            {
                timeoutDelay = 15000;
            }
            WheelTimerTimeout<TimeoutState> timeout = wheelTimer.NewTimeout(new WheelTimerTimeoutTask<TimeoutState>
            {
                Callback = TimeoutCallback,
                State = new TimeoutState { RequestId = requestId, Tcs = new TaskCompletionSource<MessageResponeInfo>() }
            }, timeoutDelay);
            sends.TryAdd(requestId, timeout);
            return timeout;
        }
        private void TimeoutCallback(WheelTimerTimeout<TimeoutState> timeout)
        {
            sends.TryRemove(timeout.Task.State.RequestId, out _);
            timeout.Task.State.Tcs.SetResult(new MessageResponeInfo { Code = MessageResponeCodes.TIMEOUT });
        }
    }

    public class MessageResponeInfo
    {
        public MessageResponeCodes Code { get; set; } = MessageResponeCodes.OK;
        public ReadOnlyMemory<byte> Data { get; set; } = Helper.EmptyArray;
    }

    public class TimeoutState
    {
        public ulong RequestId { get; set; }
        public TaskCompletionSource<MessageResponeInfo> Tcs { get; set; }
    }
}


using common.libs;
using common.server.model;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace common.server
{
    /// <summary>
    /// 消息发送器
    /// </summary>
    public sealed class MessengerSender
    {
        /// <summary>
        /// 
        /// </summary>
        public NumberSpaceUInt32 requestIdNumberSpace = new NumberSpaceUInt32(0);
        private WheelTimer<TimeoutState> wheelTimer = new WheelTimer<TimeoutState>();
        private ConcurrentDictionary<uint, WheelTimerTimeout<TimeoutState>> sends = new ConcurrentDictionary<uint, WheelTimerTimeout<TimeoutState>>();

        /// <summary>
        /// 
        /// </summary>
        public MessengerSender()
        {
        }

        /// <summary>
        /// 发送并等待回复
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task<MessageResponeInfo> SendReply(MessageRequestWrap msg)
        {
            if (msg.Connection == null)
            {
                return new MessageResponeInfo { Code = MessageResponeCodes.NOT_CONNECT };
            }
            await msg.Connection.WaitOne();
            if (msg.RequestId == 0)
            {
                uint id = msg.RequestId;
                Interlocked.CompareExchange(ref id, requestIdNumberSpace.Increment(), 0);
                msg.RequestId = id;
            }
            WheelTimerTimeout<TimeoutState> timeout = NewReply(msg);

            bool res = await SendOnly(msg, true).ConfigureAwait(false);

            if (res == false)
            {
                sends.TryRemove(msg.RequestId, out _);
                timeout.Cancel();
                timeout.Task.State.Tcs.SetResult(new MessageResponeInfo { Code = MessageResponeCodes.NOT_CONNECT });
            }
            msg.Connection.Release();

            return await timeout.Task.State.Tcs.Task.ConfigureAwait(false);
        }

        /// <summary>
        /// 只发送，不等回复
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task<bool> SendOnly(MessageRequestWrap msg, bool locked = false)
        {
            if (msg.Connection == null)
            {
                return false;
            }

            if (locked == false)
                await msg.Connection.WaitOne();
            try
            {
                if (msg.RequestId == 0)
                {
                    uint id = msg.RequestId;
                    Interlocked.CompareExchange(ref id, requestIdNumberSpace.Increment(), 0);
                    msg.RequestId = id;
                }

                msg.Relay = msg.Connection.Relay || msg.RelayId.Length > 0;
                if (msg.Connection.Relay && msg.RelayId.Length == 0)
                {
                    msg.RelayId = msg.Connection.RelayId;
                }

                msg.Encode = msg.Connection.EncodeEnabled && msg.Encode;
                if (msg.Encode)
                {
                    msg.Payload = msg.Connection.Crypto.Encode(msg.Payload);
                }

                byte[] bytes = msg.ToArray(out int length);
                bool res = await msg.Connection.Send(bytes.AsMemory(0, length)).ConfigureAwait(false);
                msg.Return(bytes);
                return res;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
            finally
            {
                if (locked == false)
                    msg.Connection.Release();
            }
            return false;
        }

        /// <summary>
        /// 回复远程消息，收到某个连接的消息后，通过这个再返回消息给它
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async ValueTask<bool> ReplyOnly(MessageResponseWrap msg)
        {
            if (msg.Connection == null)
            {
                return false;
            }

            await msg.Connection.WaitOne();
            try
            {
                msg.Encode = msg.Connection.EncodeEnabled && msg.Encode;
                if (msg.Encode)
                {
                    msg.Payload = msg.Connection.Crypto.Encode(msg.Payload);
                }

                byte[] bytes = msg.ToArray(out int length);
                bool res = await msg.Connection.Send(bytes.AsMemory(0, length)).ConfigureAwait(false);
                msg.Return(bytes);
                return res;
            }
            catch (Exception ex)
            {
                Logger.Instance.DebugError(ex);
            }
            finally
            {
                msg.Connection.Release();
            }
            return false;
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
                timeout.Task.State.Tcs.SetResult(new MessageResponeInfo { Code = wrap.Code, Data = wrap.Payload });
            }
        }

        private WheelTimerTimeout<TimeoutState> NewReply(MessageRequestWrap msg)
        {
            msg.Reply = true;
            if (msg.Timeout <= 0)
            {
                msg.Timeout = 15000;
            }
            WheelTimerTimeout<TimeoutState> timeout = wheelTimer.NewTimeout(new WheelTimerTimeoutTask<TimeoutState>
            {
                Callback = TimeoutCallback,
                State = new TimeoutState { RequestId = msg.RequestId, Tcs = new TaskCompletionSource<MessageResponeInfo>() }
            }, msg.Timeout);
            sends.TryAdd(msg.RequestId, timeout);
            return timeout;
        }
        private void TimeoutCallback(WheelTimerTimeout<TimeoutState> timeout)
        {
            sends.TryRemove(timeout.Task.State.RequestId, out _);
            timeout.Task.State.Tcs.SetResult(new MessageResponeInfo { Code = MessageResponeCodes.TIMEOUT });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class MessageResponeInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public MessageResponeCodes Code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ReadOnlyMemory<byte> Data { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class TimeoutState
    {
        /// <summary>
        /// 
        /// </summary>
        public uint RequestId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public TaskCompletionSource<MessageResponeInfo> Tcs { get; set; }
    }
}


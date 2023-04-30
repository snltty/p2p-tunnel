using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;

namespace invokeSpeed
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //var summary = BenchmarkRunner.Run<Test>();
            Console.ReadLine();
        }


    }
    [MemoryDiagnoser]
    public unsafe class Test
    {
        byte[] data = new byte[1024];
        [Benchmark]
        public void Test1()
        {
            MessageRequestWrap messageRequestWrap = new MessageRequestWrap();
           
            for (int i = 0; i < 100; i++)
            {
                messageRequestWrap.Payload = data.AsMemory();
                Test11(ref messageRequestWrap);
            }
        }

        [Benchmark]
        public void Test2()
        {
            MessageRequestWrap1 messageRequestWrap = new MessageRequestWrap1();
           
            for (int i = 0; i < 100; i++)
            {

                messageRequestWrap.Payload = data.AsMemory();
                Test22(messageRequestWrap);
            }
        }

        private void Test11(ref MessageRequestWrap messageRequestWrap)
        {

        }

        private void Test22(MessageRequestWrap1 messageRequestWrap)
        {

        }
    }


    public struct MessageRequestWrap
    {
        #region 字段
        /// <summary>
        /// 
        /// </summary>
        public const int RelayIdLengthPos = 5;
        /// <summary>
        /// 
        /// </summary>
        public const int RelayIdIndexPos = RelayIdLengthPos + 1;
        /// <summary>
        /// 
        /// </summary>
        public const int RelayIdSize = 8;
        /// <summary>
        /// Relay + Reply + EncodeBit + 00001
        /// </summary>
        public const byte RelayBit = 0b10000000;
        /// <summary>
        /// 
        /// </summary>
        public const byte ReplyBit = 0b01000000;
        /// <summary>
        /// 
        /// </summary>
        public const byte EncodeBit = 0b00100000;
        /// <summary>
        /// 
        /// </summary>
        public const byte TypeBits = 0b00000001;

        /// <summary>
        /// 超时时间，发送待回复时设置
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// 消息id
        /// </summary>
        public ushort MessengerId { get; set; }
        /// <summary>
        /// 每条数据都有个id，自动设置，无需填写【只发发数据的话，不用填这里】
        /// </summary>
        public uint RequestId { get; set; }
        /// <summary>
        /// 是否等待回复，自动设置，无需填写
        /// </summary>
        public bool Reply { get; internal set; }

        /// <summary>
        /// 是否中继，自动设置，无需填写
        /// </summary>
        public bool Relay { get; set; }
        /// <summary>
        /// 加密，自动设置，无需填写
        /// </summary>
        public bool Encode { get; set; }


        /// <summary>
        /// 中继节点id列表
        /// </summary>
        public Memory<ulong> RelayId { get; set; }
        /// <summary>
        /// 中继节点id列表，读取用，自动设置，无需填写
        /// </summary>
        public Memory<byte> RelayIds { get; private set; }
        /// <summary>
        /// 中继经过节点数量，自动设置，无需填写
        /// </summary>
        public byte RelayIdLength { get; private set; }
        /// <summary>
        /// 中继下，当前所在节点，自动设置，无需填写
        /// </summary>
        public byte RelayIdIndex { get; private set; }

        /// <summary>
        /// 数据荷载
        /// </summary>
        public Memory<byte> Payload { get; set; }
        #endregion


    }

    public sealed class MessageRequestWrap1
    {
        #region 字段
        /// <summary>
        /// 
        /// </summary>
        public const int RelayIdLengthPos = 5;
        /// <summary>
        /// 
        /// </summary>
        public const int RelayIdIndexPos = RelayIdLengthPos + 1;
        /// <summary>
        /// 
        /// </summary>
        public const int RelayIdSize = 8;
        /// <summary>
        /// Relay + Reply + EncodeBit + 00001
        /// </summary>
        public const byte RelayBit = 0b10000000;
        /// <summary>
        /// 
        /// </summary>
        public const byte ReplyBit = 0b01000000;
        /// <summary>
        /// 
        /// </summary>
        public const byte EncodeBit = 0b00100000;
        /// <summary>
        /// 
        /// </summary>
        public const byte TypeBits = 0b00000001;

        /// <summary>
        /// 超时时间，发送待回复时设置
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// 消息id
        /// </summary>
        public ushort MessengerId { get; set; }
        /// <summary>
        /// 每条数据都有个id，自动设置，无需填写【只发发数据的话，不用填这里】
        /// </summary>
        public uint RequestId { get; set; }
        /// <summary>
        /// 是否等待回复，自动设置，无需填写
        /// </summary>
        public bool Reply { get; internal set; }

        /// <summary>
        /// 是否中继，自动设置，无需填写
        /// </summary>
        public bool Relay { get; set; }
        /// <summary>
        /// 加密，自动设置，无需填写
        /// </summary>
        public bool Encode { get; set; }


        /// <summary>
        /// 中继节点id列表
        /// </summary>
        public Memory<ulong> RelayId { get; set; }
        /// <summary>
        /// 中继节点id列表，读取用，自动设置，无需填写
        /// </summary>
        public Memory<byte> RelayIds { get; private set; }
        /// <summary>
        /// 中继经过节点数量，自动设置，无需填写
        /// </summary>
        public byte RelayIdLength { get; private set; }
        /// <summary>
        /// 中继下，当前所在节点，自动设置，无需填写
        /// </summary>
        public byte RelayIdIndex { get; private set; }

        /// <summary>
        /// 数据荷载
        /// </summary>
        public Memory<byte> Payload { get; set; }
        #endregion


    }

}
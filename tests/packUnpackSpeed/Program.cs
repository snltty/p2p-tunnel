using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using common.libs.extends;
using common.server;
using common.tcpforward;
using MessagePack;
using System.Buffers;

namespace toarray
{
    internal class Program
    {
        static void Main(string[] args)
        {
            /*
                A客户端    ---->       B客户端
                序列化     网络发送     反序列化
             */

            var summary = BenchmarkRunner.Run<TcpForwardTest>();
            Console.ReadLine();
        }
    }

    [MemoryDiagnoser, RPlotExporter]
    public class TcpForwardTest
    {

        TcpForwardInfo tcpForwardInfo = new TcpForwardInfo
        {
            AliveType = TcpForwardAliveTypes.Web,
            Buffer = new byte[1024],
            DataType = TcpForwardDataTypes.Forward,
            ForwardType = TcpForwardTypes.Forward,
            RequestId = 1,
            SourcePort = 5000,
            StateType = TcpForwardStateTypes.Success
        };
        Memory<byte> data1kb = new byte[1 * 1024];
        Memory<byte> data2kb = new byte[2 * 1024];
        Memory<byte> data8kb = new byte[8 * 1024];
        Memory<byte> data16kb = new byte[16 * 1024];
        Memory<byte> data32kb = new byte[32 * 1024];


        Memory<byte> data1kbMessagePack;
        Memory<byte> data1kbToarray;
        int data1kbToarrayLength = 0;

        Memory<byte> data2kbMessagePack;
        Memory<byte> data2kbToarray;
        int data2kbToarrayLength = 0;

        Memory<byte> data8kbMessagePack;
        Memory<byte> data8kbToarray;
        int data8kbToarrayLength = 0;

        Memory<byte> data16kbMessagePack;
        Memory<byte> data16kbToarray;
        int data16kbToarrayLength = 0;

        Memory<byte> data32kbMessagePack;
        Memory<byte> data32kbToarray;
        int data32kbToarrayLength = 0;

        [GlobalSetup]
        public void Setup()
        {
            tcpForwardInfo.Buffer = data1kb;
            data1kbMessagePack = MessagePackSerializer.Serialize(tcpForwardInfo);
            data1kbToarray = tcpForwardInfo.ToBytes(out data1kbToarrayLength);


            tcpForwardInfo.Buffer = data2kb;
            data2kbMessagePack = MessagePackSerializer.Serialize(tcpForwardInfo);
            data2kbToarray = tcpForwardInfo.ToBytes(out data2kbToarrayLength);

            tcpForwardInfo.Buffer = data8kb;
            data8kbMessagePack = MessagePackSerializer.Serialize(tcpForwardInfo);
            data8kbToarray = tcpForwardInfo.ToBytes(out data8kbToarrayLength);

            tcpForwardInfo.Buffer = data16kb;
            data16kbMessagePack = MessagePackSerializer.Serialize(tcpForwardInfo);
            data16kbToarray = tcpForwardInfo.ToBytes(out data16kbToarrayLength);

            tcpForwardInfo.Buffer = data32kb;
            data32kbMessagePack = MessagePackSerializer.Serialize(tcpForwardInfo);
            data32kbToarray = tcpForwardInfo.ToBytes(out data32kbToarrayLength);
        }

        [Benchmark]
        public void MessagePack_1KB_Serialize()
        {
            tcpForwardInfo.Buffer = data1kb;
            byte[] bytes = MessagePackSerializer.Serialize(tcpForwardInfo);
        }
        [Benchmark]
        public void MessagePack_1KB_Deserialize()
        {
            TcpForwardInfo model = MessagePackSerializer.Deserialize<TcpForwardInfo>(data1kbMessagePack);
        }

        [Benchmark]
        public void Toarray_1KB_ToBytes()
        {
            tcpForwardInfo.Buffer = data1kb;
            byte[] bytes = tcpForwardInfo.ToBytes(out int length);
            tcpForwardInfo.Return(bytes);
        }
        [Benchmark]
        public void Toarray_1KB_DeBytes()
        {
            TcpForwardInfo model = new TcpForwardInfo();
            model.DeBytes(data1kbToarray.Slice(0, data1kbToarrayLength));
        }



        [Benchmark]
        public void MessagePack_2KB_Serialize()
        {
            tcpForwardInfo.Buffer = data2kb;
            byte[] bytes = MessagePackSerializer.Serialize(tcpForwardInfo);
        }
        [Benchmark]
        public void MessagePack_2KB_Deserialize()
        {
            TcpForwardInfo model = MessagePackSerializer.Deserialize<TcpForwardInfo>(data2kbMessagePack);
        }

        [Benchmark]
        public void Toarray_2KB_ToBytes()
        {
            tcpForwardInfo.Buffer = data2kb;
            byte[] bytes = tcpForwardInfo.ToBytes(out int length);
            tcpForwardInfo.Return(bytes);
        }
        [Benchmark]
        public void Toarray_2KB_DeBytes()
        {
            TcpForwardInfo model = new TcpForwardInfo();
            model.DeBytes(data2kbToarray.Slice(0, data2kbToarrayLength));
        }




        [Benchmark]
        public void MessagePack_8KB_Serialize()
        {
            tcpForwardInfo.Buffer = data8kb;
            byte[] bytes = MessagePackSerializer.Serialize(tcpForwardInfo);
        }
        [Benchmark]
        public void MessagePack_8KB_Deserialize()
        {
            TcpForwardInfo model = MessagePackSerializer.Deserialize<TcpForwardInfo>(data8kbMessagePack);
        }

        [Benchmark]
        public void Toarray_8KB_ToBytes()
        {
            tcpForwardInfo.Buffer = data8kb;
            byte[] bytes = tcpForwardInfo.ToBytes(out int length);
            tcpForwardInfo.Return(bytes);
        }
        [Benchmark]
        public void Toarray_8KB_DeBytes()
        {
            TcpForwardInfo model = new TcpForwardInfo();
            model.DeBytes(data8kbToarray.Slice(0, data8kbToarrayLength));
        }




        [Benchmark]
        public void MessagePack_16KB_Serialize()
        {
            tcpForwardInfo.Buffer = data1kb;
            byte[] bytes = MessagePackSerializer.Serialize(tcpForwardInfo);
        }
        [Benchmark]
        public void MessagePack_16KB_Deserialize()
        {
            TcpForwardInfo model = MessagePackSerializer.Deserialize<TcpForwardInfo>(data16kbMessagePack);
        }

        [Benchmark]
        public void Toarray_16KB_ToBytes()
        {
            tcpForwardInfo.Buffer = data16kb;
            byte[] bytes = tcpForwardInfo.ToBytes(out int length);
            tcpForwardInfo.Return(bytes);
        }
        [Benchmark]
        public void Toarray_16KB_DeBytes()
        {
            TcpForwardInfo model = new TcpForwardInfo();
            model.DeBytes(data16kbToarray.Slice(0, data16kbToarrayLength));
        }




        [Benchmark]
        public void MessagePack_32KB_Serialize()
        {
            tcpForwardInfo.Buffer = data32kb;
            byte[] bytes = MessagePackSerializer.Serialize(tcpForwardInfo);
        }
        [Benchmark]
        public void MessagePack_32KB_Deserialize()
        {
            TcpForwardInfo model = MessagePackSerializer.Deserialize<TcpForwardInfo>(data32kbMessagePack);
        }

        [Benchmark]
        public void Toarray_32KB_ToBytes()
        {
            tcpForwardInfo.Buffer = data32kb;
            byte[] bytes = tcpForwardInfo.ToBytes(out int length);
            tcpForwardInfo.Return(bytes);
        }
        [Benchmark]
        public void Toarray_32KB_DeBytes()
        {
            TcpForwardInfo model = new TcpForwardInfo();
            model.DeBytes(data32kbToarray.Slice(0, data32kbToarrayLength));
        }
    }

    [MessagePackObject]
    public sealed class TcpForwardInfo
    {
        public TcpForwardInfo() { }

        [Key(1)]
        public ushort SourcePort { get; set; }
        [Key(2)]
        public TcpForwardAliveTypes AliveType { get; set; }
        [Key(3)]
        public TcpForwardTypes ForwardType { get; set; }
        [Key(4)]
        public TcpForwardDataTypes DataType { get; set; }
        [Key(5)]
        public TcpForwardStateTypes StateType { get; set; }

        [Key(6)]
        public uint RequestId { get; set; }

        [Key(7)]
        [System.Text.Json.Serialization.JsonIgnore]
        public Memory<byte> TargetEndpoint { get; set; }

        [Key(8)]
        [System.Text.Json.Serialization.JsonIgnore]
        public Memory<byte> Buffer { get; set; }


        [IgnoreMemberAttribute]
        public Memory<byte> Cache { get; set; }

        [IgnoreMemberAttribute]
        [System.Text.Json.Serialization.JsonIgnore]
        public IConnection Connection { get; set; }
        public byte[] ToBytes(out int length)
        {
            length =
                1 +  //AliveType + ForwardType
                1 + // DataType + StateType
                4 +
                1 + TargetEndpoint.Length +
                Buffer.Length;

            byte[] bytes = ArrayPool<byte>.Shared.Rent(length);
            var memory = bytes.AsMemory();
            int index = 0;


            bytes[index] = (byte)(((byte)AliveType - 1) << 1 | ((byte)ForwardType - 1));
            index += 1;

            bytes[index] = (byte)((byte)DataType << 4 | (byte)StateType);
            index += 1;

            RequestId.ToBytes(memory.Slice(index));
            index += 4;

            bytes[index] = (byte)TargetEndpoint.Length;
            index += 1;
            TargetEndpoint.CopyTo(memory.Slice(index));
            index += TargetEndpoint.Length;

            Buffer.CopyTo(memory.Slice(index));
            index += Buffer.Length;
            return bytes;
        }
        public void DeBytes(in Memory<byte> memory)
        {
            var span = memory.Span;
            int index = 0;

            AliveType = (TcpForwardAliveTypes)(byte)(((span[index] >> 1) & 0b1) + 1);
            ForwardType = (TcpForwardTypes)(byte)((span[index] & 0b1) + 1);
            index += 1;

            DataType = (TcpForwardDataTypes)(byte)((span[index] >> 4) & 0b1111);
            StateType = (TcpForwardStateTypes)(byte)(span[index] & 0b1111);
            index += 1;

            RequestId = span.Slice(index).ToUInt32();
            index += 4;

            byte epLength = span[index];
            index += 1;
            TargetEndpoint = memory.Slice(index, epLength);
            index += epLength;



            Buffer = memory.Slice(index);
        }

        public void Return(byte[] data)
        {
            ArrayPool<byte>.Shared.Return(data);
        }
    }
}
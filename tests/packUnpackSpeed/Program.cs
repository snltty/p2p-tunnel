using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using common.libs.extends;
using common.server;
using common.tcpforward;
using MessagePack;
using System.Buffers;

namespace packUnpackSpeed
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<TcpForwardTest>();
            Console.ReadLine();
        }
    }

    [MemoryDiagnoser]
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
        Memory<byte> data1kb = new byte[1*1024];
        Memory<byte> data2kb = new byte[2*1024];
        Memory<byte> data8kb = new byte[8*1024];
        Memory<byte> data16kb = new byte[16*1024];
        Memory<byte> data32kb = new byte[32*1024];


        [GlobalSetup]
        public void Setup()
        {

        }

        [Benchmark]
        public void MessagePack_1KB()
        {
            tcpForwardInfo.Buffer = data1kb;
            byte[] bytes = MessagePackSerializer.Serialize(tcpForwardInfo);
            TcpForwardInfo model = MessagePackSerializer.Deserialize<TcpForwardInfo>(bytes);
        }
        [Benchmark]
        public void Toarray_1KB()
        {
            tcpForwardInfo.Buffer = data1kb;
            byte[] bytes = tcpForwardInfo.ToBytes(out int length);
            TcpForwardInfo model = new TcpForwardInfo();
            model.DeBytes(bytes.AsMemory(0,length));
            model.Return(bytes);
        }


        [Benchmark]
        public void MessagePack_2KB()
        {
            tcpForwardInfo.Buffer = data2kb;
            byte[] bytes = MessagePackSerializer.Serialize(tcpForwardInfo);
            TcpForwardInfo model = MessagePackSerializer.Deserialize<TcpForwardInfo>(bytes);
        }
        [Benchmark]
        public void Toarray_2KB()
        {
            tcpForwardInfo.Buffer = data2kb;
            byte[] bytes = tcpForwardInfo.ToBytes(out int length);
            TcpForwardInfo model = new TcpForwardInfo();
            model.DeBytes(bytes.AsMemory(0, length));
            model.Return(bytes);
        }


        [Benchmark]
        public void MessagePack_8KB()
        {
            tcpForwardInfo.Buffer = data8kb;
            byte[] bytes = MessagePackSerializer.Serialize(tcpForwardInfo);
            TcpForwardInfo model = MessagePackSerializer.Deserialize<TcpForwardInfo>(bytes);
        }
        [Benchmark]
        public void Toarray_8KB()
        {
            tcpForwardInfo.Buffer = data8kb;
            byte[] bytes = tcpForwardInfo.ToBytes(out int length);
            TcpForwardInfo model = new TcpForwardInfo();
            model.DeBytes(bytes.AsMemory(0, length));
            model.Return(bytes);
        }


        [Benchmark]
        public void MessagePack_16KB()
        {
            tcpForwardInfo.Buffer = data16kb;
            byte[] bytes = MessagePackSerializer.Serialize(tcpForwardInfo);
            TcpForwardInfo model = MessagePackSerializer.Deserialize<TcpForwardInfo>(bytes);
        }
        [Benchmark]
        public void Toarray_16KB()
        {
            tcpForwardInfo.Buffer = data16kb;
            byte[] bytes = tcpForwardInfo.ToBytes(out int length);
            TcpForwardInfo model = new TcpForwardInfo();
            model.DeBytes(bytes.AsMemory(0, length));
            model.Return(bytes);
        }

        [Benchmark]
        public void MessagePack_32KB()
        {
            tcpForwardInfo.Buffer = data32kb;
            byte[] bytes = MessagePackSerializer.Serialize(tcpForwardInfo);
            TcpForwardInfo model = MessagePackSerializer.Deserialize<TcpForwardInfo>(bytes);
        }
        [Benchmark]
        public void Toarray_32KB()
        {
            tcpForwardInfo.Buffer = data32kb;
            byte[] bytes = tcpForwardInfo.ToBytes(out int length);
            TcpForwardInfo model = new TcpForwardInfo();
            model.DeBytes(bytes.AsMemory(0, length));
            model.Return(bytes);
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
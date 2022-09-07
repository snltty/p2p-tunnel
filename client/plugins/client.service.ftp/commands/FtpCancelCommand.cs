using common.libs.extends;
using System;

namespace client.service.ftp.commands
{
    public class FtpCancelCommand : IFtpCommandBase
    {
        public FtpCommand Cmd { get; set; } = FtpCommand.FILE_CANCEL;

        public ulong Md5 { get; set; }

        public byte[] ToBytes()
        {
            byte cmdByte = (byte)Cmd;
            byte[] md5Byte = Md5.ToBytes();

            var bytes = new byte[
                1 +
                md5Byte.Length
            ];
            bytes[0] = cmdByte;

            Array.Copy(md5Byte, 0, bytes, 1, md5Byte.Length);

            return bytes;
        }

        public void DeBytes(in ReadOnlyMemory<byte> bytes)
        {
            Cmd = (FtpCommand)bytes.Span[0];
            Md5 = bytes.Span.Slice(1).ToUInt64();
        }
    }

    public class FtpCanceledCommand : IFtpCommandBase
    {
        public FtpCommand Cmd { get; set; } = FtpCommand.FILE_CANCELED;

        public ulong Md5 { get; set; }

        public byte[] ToBytes()
        {
            byte cmdByte = (byte)Cmd;
            byte[] md5Byte = Md5.ToBytes();

            var bytes = new byte[
                1 +
                md5Byte.Length
            ];
            bytes[0] = cmdByte;

            Array.Copy(md5Byte, 0, bytes, 1, md5Byte.Length);

            return bytes;
        }

        public void DeBytes(in ReadOnlyMemory<byte> bytes)
        {
            Cmd = (FtpCommand)bytes.Span[0];
            Md5 = bytes.Span.Slice(1).ToUInt64();
        }
    }
}

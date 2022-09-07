using System;

namespace client.service.ftp.commands
{
    public class FtpCurrentPathCommand : IFtpCommandBase
    {
        public FtpCommand Cmd { get; set; } = FtpCommand.CURRENT_PATH;

        public byte[] ToBytes()
        {
            byte cmdByte = (byte)Cmd;

            var bytes = new byte[1] { cmdByte };
            return bytes;
        }

        public void DeBytes(in ReadOnlyMemory<byte> bytes)
        {
            Cmd = (FtpCommand)bytes.Span[0];
        }
    }
}

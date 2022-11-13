namespace common.server.model
{
    public enum ServerType : byte
    {
        TCP = 1, UDP = 2, TCPUDP = TCP | UDP
    }
}

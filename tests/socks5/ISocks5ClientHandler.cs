namespace socks5
{
    public interface ISocks5ClientHandler
    {
        Func<Socks5Info, bool> OnSendRequest { get; set; }
        Action<Socks5Info> OnSendClose { get; set; }
        
        void InputData(Memory<byte> data);
        void Flush();
    }
}

namespace socks5
{
    public interface ISocks5ServerHandler
    {
        Action<Socks5Info> OnSendResponse { get; set; }
        Action<Socks5Info> OnSendClose { get; set; }
        
        void InputData(Socks5Info data);
    }
}

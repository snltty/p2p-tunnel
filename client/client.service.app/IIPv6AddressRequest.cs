using System.Net;

namespace client.service.app
{
    internal class IPv6AddressRequest : IIPv6AddressRequest
    {
        public IPAddress[] GetIPV6()
        {
            try
            {
                using HttpClient client = new HttpClient();
                string ip = client.GetStringAsync("https://api64.ipify.org").Result;

                return new IPAddress[] { IPAddress.Parse(ip) };

            }
            catch (Exception)
            {
            }
            return Array.Empty<IPAddress>();
        }
    }
}

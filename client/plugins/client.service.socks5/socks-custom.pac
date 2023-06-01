
var proxy = "SOCKS5 {socks5-address}";

function FindProxyForURL(url, host) 
{
    if (shExpMatch(host, "*.mydomain.com"))
    {
        return proxy;
    }
    return "DIRECT";
}


var proxy = "PROXY {proxy-address}";

function FindProxyForURL(url, host) 
{
    if (shExpMatch(host, "*.mydomain.com"))
    {
        return proxy;
    }
    return "DIRECT";
}

namespace DNS_proxy.Service;

public static class DnsServerInstance
{
    public static CustomDnsServer Server { get; } = new CustomDnsServer();
}

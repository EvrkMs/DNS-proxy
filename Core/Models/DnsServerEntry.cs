namespace DNS_proxy.Core.Models;

public class DnsServerEntry
{
    public int Id { get; set; }
    public string Address { get; set; } = null!;
    public bool IsDoh { get; set; }
    public bool UseWireFormat { get; set; } = false;
    public int Priority { get; set; }
}


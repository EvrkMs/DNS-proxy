namespace DNS_proxy.Core.Models;

public class DnsRule
{
    public int Id { get; set; }
    public string SourceIp { get; set; } = string.Empty;
    public string DomainPattern { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? RewriteIp { get; set; }
}

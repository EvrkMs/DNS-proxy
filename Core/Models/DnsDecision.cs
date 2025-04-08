namespace DNS_proxy.Core.Models;

public class DnsDecision
{
    public static DnsDecision Allow => new() { IsBlocked = false };
    public static DnsDecision Block => new() { IsBlocked = true };

    public bool IsBlocked { get; set; }
    public bool IsRewrite { get; set; }
    public string? RewriteIp { get; set; }
}

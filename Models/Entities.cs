using System.Net;

namespace DnsProxy.Models;

public enum RuleAction { Allow, Block, Rewrite }

public class DnsRule
{
    public int Id { get; set; }
    public string SourceIp { get; set; } = "";
    public string DomainPattern { get; set; } = "";
    public RuleAction Action { get; set; }
    public string? RewriteIp { get; set; }
    public string? IncludeServers { get; set; }    // csv серверов, через которые МОЖНО пустить
    public string? ExcludeServers { get; set; }    // csv серверов, через которые НЕЛЬЗЯ
}
public enum DnsProtocol
{
    Udp,
    DoH_Wire,     // wire-формат (RFC 8484)
    DoH_Json,     // JSON-формат (Google/Cloudflare style)
}
public class DnsServerEntry
{
    public int Id { get; set; }
    public string Address { get; set; } = "";
    public DnsProtocol Protocol { get; set; }
    public int Priority { get; set; }
    public IPAddress? StaticAddress { get; set; }
}

public class VisitStatistic
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string ClientIp { get; set; } = "";
    public string Domain { get; set; } = "";
    public string Upstream { get; set; } = "";
    public RuleAction Action { get; set; }
}
